using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ServersideQoL;

partial class ServersideQoLPlugin : ServersideQoLPluginBase<ServersideQoLPlugin, Config>
{
  static readonly HashSet<IServersideQoLPlugin> __plugins = [];
  readonly Dictionary<Guid, Processor> _processorsById = [];
  readonly List<Processor> _enabledProcessors = [];
  List<Processor>? _preprocessors;
  bool _patcherSucceeded;

  internal static Harmony HarmonyInstance { get; } = new(PluginGuid);
  internal IReadOnlyDictionary<Guid, Processor> Processors => _processorsById;
  public event Action? GlobalKeysChanged;
  public event Action? GlobalKeyValuesChanged;

  internal static void RegisterPlugin(IServersideQoLPlugin plugin)
      => __plugins.Add(plugin);

  Func<PrefabInfo> _prefabInfoFactory = default!;
  readonly ConcurrentDictionary<int, PrefabInfo> _prefabInfos = [];

  uint _unfinishedProcessingInRow;

  sealed class SectorState
  {
    public List<Peer> Peers { get; } = [];
    public HashSet<ServersideQoLZDO> Changed { get; } = [];
    public HashSet<ServersideQoLZDO> Repeat { get; } = [];
  }

  readonly Dictionary<Vector2s, SectorState> _sectors = [];
  readonly HashSet<SectorState> _sectorsToProcess = [];
  List<ServersideQoLZDO> _repeat = [];
  SectorState? _currentlyProcessing;

  readonly List<Processor> _unregister = [];
  readonly List<Processor> _reregisterOnRecreate = [];
  List<(Processor, double)>? _processingTimes;

  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  partial void OnAwake()
  {
    _patcherSucceeded = true;
    try { AssertPatcher(); }
    catch (MissingMemberException) { _patcherSucceeded = false; }

    if (_patcherSucceeded)
      HarmonyInstance.PatchAll(typeof(ServersideQoLPlugin).Assembly);
    else
      Logger.LogError($"{Patchers.PatchersPlugin.PluginName}.dll was not installed correctly. Put it in {Paths.PatcherPluginPath}");

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AssertPatcher()
    {
      if (new ZDO().ServersideQoLZDO is null)
        throw new Exception();
    }
  }

  void Start()
  {
    if (!_patcherSucceeded)
      return;

    StartCoroutine(CallExecute());

    IEnumerator<YieldInstruction?> CallExecute()
    {
      bool pluginsInitialized = false;

      while (true)
      {
        while (ZNet.instance is null)
          yield return new WaitForSeconds(0.2f);

        if (ZNet.instance.IsServer() is false)
        {
          Logger.LogWarning("Mod should only be installed on the host");
          yield return new WaitForSeconds(5);
          continue;
        }

        while (ZDOMan.instance is null || ZNetScene.instance is null || ZNet.World is null)
          yield return new WaitForSeconds(0.2f);

        if (!pluginsInitialized)
        {
          pluginsInitialized = true;
          if (!InitializePlugins())
          {
            HarmonyInstance.UnpatchSelf();
            yield break;
          }
        }

        if (!Initialize())
        {
          yield return new WaitForSeconds(5);
          continue;
        }

        ZNetPeer? localPeer = null;
        if (!ZNet.instance.IsDedicated())
        {
          while (Player.m_localPlayer is null)
            yield return new WaitForSeconds(0.2f);

          localPeer = new(new DummySocket(), true)
          {
            m_uid = ZDOMan.GetSessionID(),
            m_characterID = Player.m_localPlayer.GetZDOID(),
            m_server = true
          };
        }
        var peers = new PeersEnumerable(localPeer);

        while (true)
        {
          yield return null;

          if (ZNet.instance is null)
            break;

          var minFps = ZNet.instance.IsDedicated() ? 10 : 30;// Game.m_minimumFPSLimit;
          var targetFps = Application.targetFrameRate < 0 ? 2 * minFps : Application.targetFrameRate;
          var maxDelta = 1.0 / minFps;
          var actualFps = 1.0 / Time.unscaledDeltaTime;
          if (Time.unscaledDeltaTime > maxDelta)
          {
            if (Config.DiagnosticLogs.Value)
              Logger.LogInfo($"No time budget available, actual FPS: {actualFps}, min FPS: {minFps}, target FPS: {targetFps}");
            continue;
          }
          var fraction = Math.Min(1, (actualFps - minFps) / (targetFps - minFps));
          var budget = (maxDelta - Time.unscaledDeltaTime) * fraction;

          try { Execute(peers, budget); }
          catch (OperationCanceledException) { yield break; }
          catch (Exception ex)
          {
            Logger.LogError(ex);
            yield break;
          }
        }
      }
    }
  }

  bool InitializePlugins()
  {
    List<IServersideQoLPlugin>? remove = null;
    TypeExtensionBuilder<IPrefabInfo, PrefabInfo> prefabInfoBuilder = new();
    int processorCount = 0;
    foreach (var plugin in __plugins)
    {
      try { plugin.RegisterProcessors(); }
      catch (Exception ex)
      {
        Logger.LogError(Invariant($"Failed to register processors for plugin {plugin.GetType().FullName}: {ex}"));
        (remove ??= []).Add(plugin);
        continue;
      }
      if (plugin.Processors.Count is 0 && plugin is not ServersideQoLPlugin)
      {
        Logger.LogWarning(Invariant($"No processors registered for plugin {plugin.GetType().FullName}"));
        (remove ??= []).Add(plugin);
        continue;
      }

      foreach (var processor in plugin.Processors)
      {
        if (!_processorsById.TryAdd(processor.Attribute.Id, processor))
        {
          var existing = _processorsById[processor.Attribute.Id];
          Logger.LogError($"Processor {processor.GetType().FullName} is using the same ID as {existing.GetType().FullName} and will be ignored");
          continue;
        }
        processor.AddPrefabInfoInterfaceInternal(prefabInfoBuilder);
        if (plugin is not ServersideQoLPlugin)
          ++processorCount;
        if (plugin.Config.Enabled.Value)
          _enabledProcessors.Add(processor);
      }
    }
    if (remove is not null)
    {
      foreach (var plugin in remove)
        __plugins.Remove(plugin);
    }

    if (!prefabInfoBuilder.HasInterfaces)
    {
      Logger.LogWarning("No plugins registered");
      return false;
    }

    if (processorCount is 0)
    {
      Logger.LogWarning("No processors registered");
      return false;
    }

    SortProcessors(_enabledProcessors, isPrefabList: false);

    _prefabInfoFactory = prefabInfoBuilder.GetFactory();

    foreach (var plugin in __plugins)
      plugin.Config.ConfigChanged += OnConfigChanged;

    return true;
  }

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
#if DEBUG
    .Add<TestProcessor>()
#endif
    .Add<ContainerRegistryProcessor>()
    .Add<TameableRegistryProcessor>()
    .Add<PlayerRegistryProcessor>();

  internal IReadOnlyDictionary<string, PieceTable> PieceTablesByPieceName => field ?? new Func<IReadOnlyDictionary<string, PieceTable>>(static () =>
  {
    var tables = new HashSet<PieceTable>();
    var dict = new Dictionary<string, PieceTable>();
    foreach (var prefab in ZNetScene.instance.m_prefabs)
    {
      var table = prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;
      if (table is null || !tables.Add(table))
        continue;

      foreach (var piece in table.m_pieces)
        dict.TryAdd(piece.name, table);
    }
    return dict;
  }).Invoke();

  void AddChanged(ServersideQoLZDO zdo)
  {
    var sector = zdo.ZDO.GetSector();
    if (!_sectors.TryGetValue(sector, out var sectorState))
    {
      _sectors.Add(sector, sectorState = new());
      sectorState.Changed.Add(zdo);
    }
    else if (_currentlyProcessing != sectorState)
      sectorState.Changed.Add(zdo);
    else if (!sectorState.Changed.Contains(zdo))
      sectorState.Repeat.Add(zdo);
  }

  void OnPrefabChanged(ServersideQoLZDO zdo)
  {
    // may be called from field initializers which may be called from other threads

    PrefabInfo? prefabInfo;
    if (!zdo.ZDO.IsValid())
      prefabInfo = null;
    else if (!_prefabInfos.TryGetValue(zdo.ZDO.GetPrefab(), out prefabInfo))
      AddChanged(zdo);

    zdo.PrefabInfo = prefabInfo;

    if (prefabInfo is { EnabledProcessors.Count: > 0 })
      AddChanged(zdo);
  }

  void OnDataOrOwnerRevisionChanged(ServersideQoLZDO zdo)
  {
    // may be called from field initializers which may be called from other threads
    if (zdo.HasProcessors)
      AddChanged(zdo);
  }

  bool Initialize()
  {
    GenerateDocs();

    foreach (var plugin in __plugins)
    {
      var config = plugin.Config; // Initialize
      config.ConfigChanged -= OnConfigChanged;
      config.ConfigChanged += OnConfigChanged;
    }

    var cfg = Config;
    Logger.LogInfo(Invariant($"Enabled: {cfg.Enabled.Value}, DiagnosticLogs: {cfg.DiagnosticLogs.Value}"));

    if (!cfg.Enabled.Value)
      return false;

    if (Chainloader.PluginInfos.TryGetValue("org.bepinex.plugins.dedicatedserver", out var pluginInfo))
      Logger.LogWarning($"Many features are incompatible with {pluginInfo.Metadata.Name}");

    if (cfg.DiagnosticLogs.Value)
    {
      Logger.LogInfo(string.Join($"{Environment.NewLine}  ", ["Config:", .. __plugins
        .Where(static x => x.Config.Enabled.Value)
        .SelectMany(static x => x.Config.ConfigFile
          //.Where(static x => !x.Value.BoxedValue.Equals(x.Value.DefaultValue))
          .Select(static x => Invariant($"{(!x.Value.BoxedValue.Equals(x.Value.DefaultValue) ? "*" : "")}[{x.Key.Section}].[{x.Key.Key}] = {x.Value.BoxedValue}")))]));
    }

    //var failed = false;
    //var abort = false;

    //var networkVersion = (uint)typeof(Version).GetField(nameof(Version.c_networkVersion)).GetRawConstantValue();
    //var itemDataVersion = (Version.Item)typeof(Version).GetField(nameof(Version.c_ItemDataVersion)).GetRawConstantValue();
    //var worldVersion = (Version.World)typeof(Version).GetField(nameof(Version.c_WorldVersion)).GetRawConstantValue();

    //if (gameVersion != ExpectedGameVersion)
    //{
    //    Logger.LogWarning(Invariant($"Unsupported game version: {gameVersion}.x, expected: {ExpectedGameVersion}.x"));
    //    failed = true;
    //    abort |= !Config.Instance.IgnoreGameVersionCheck.Value;
    //}
    //if (networkVersion != Version.c_networkVersion)
    //{
    //  Logger.LogWarning(Invariant($"Unsupported network version: {networkVersion}, expected: {Version.c_networkVersion}"));
    //  failed = true;
    //  abort |= !Config.Instance.IgnoreNetworkVersionCheck.Value;
    //}
    //if (itemDataVersion != Version.c_ItemDataVersion)
    //{
    //  Logger.LogWarning(Invariant($"Unsupported item data version: {itemDataVersion:D} [{itemDataVersion}], expected: {Version.c_ItemDataVersion:D} [{Version.c_ItemDataVersion}]"));
    //  failed = true;
    //  abort |= !Config.Instance.IgnoreItemDataVersionCheck.Value;
    //}
    //if (worldVersion != Version.c_WorldVersion)
    //{
    //  Logger.LogWarning(Invariant($"Unsupported world version: {worldVersion:D} [{worldVersion}], expected: {Version.c_WorldVersion:D} [{Version.c_WorldVersion}]"));
    //  failed = true;
    //  abort |= !Config.Instance.IgnoreWorldVersionCheck.Value;
    //}

    //if (failed)
    //{
    //  if (!abort)
    //    Logger.LogError("Version checks failed, but you chose to ignore the checks (config). Continuing...");
    //  else
    //  {
    //    Logger.LogError("Version checks failed. Mod execution is stopped. This check can be disabled in the config.");
    //    return false;
    //  }
    //}

    foreach (var zdo in _sectors.Values.SelectMany(static x => x.Changed))
    {
      if (zdo.ZDO.IsValid())
        zdo.PrefabInfo ??= GetPrefabInfo(zdo.ZDO.GetPrefab());
    }

    Processor.StaticInitialize();
    foreach (var processor in _enabledProcessors)
      processor.Initialize();

    return true;
  }

  void OnConfigChanged(object sender, SettingChangedEventArgs e)
  {
    var cfg = (IConfig)sender;
    if (Config.DiagnosticLogs.Value || ReferenceEquals(e.ChangedSetting, Config.DiagnosticLogs))
      Logger.LogInfo($"Config changed: [{e.ChangedSetting.Definition.Section}].[{e.ChangedSetting.Definition.Key}] = {e.ChangedSetting.BoxedValue}");
    if (ReferenceEquals(e.ChangedSetting, Config.DiagnosticLogs) && Config.DiagnosticLogs.Value)
      Logger.LogInfo(string.Join($"{Environment.NewLine}  ", ["Config:", .. Config.ConfigFile.Select(static x => Invariant($"[{x.Key.Section}].[{x.Key.Key}] = {x.Value.BoxedValue}"))]));
    if (ReferenceEquals(cfg.Enabled, e.ChangedSetting))
    {
      _preprocessors = null;
      if (cfg.Enabled.Value)
      {
        foreach (var processor in cfg.Plugin.Processors)
          _enabledProcessors.Add(processor);
        SortProcessors(_enabledProcessors, isPrefabList: false);
      }
      else
      {
        foreach (var processor in cfg.Plugin.Processors)
          _enabledProcessors.Remove(processor);
      }

      foreach (var prefabInfo in _prefabInfos.Values)
      {
        if (prefabInfo is null)
          continue;

        if (cfg.Enabled.Value)
        {
          foreach (var processor in cfg.Plugin.Processors)
            prefabInfo.EnabledProcessors.Add(processor);
          SortProcessors(prefabInfo.EnabledProcessors, isPrefabList: true);
        }
        else
        {
          foreach (var processor in cfg.Plugin.Processors)
            prefabInfo.EnabledProcessors.Remove(processor);
        }
      }

      if (cfg.Enabled.Value)
      {
        foreach (var processor in cfg.Plugin.Processors)
          processor.Initialize();

        foreach (var zdo in ZDOMan.instance.GetObjects().Select(static x => x.ServersideQoLZDO))
        {
          zdo.ReregisterAll();
          OnDataOrOwnerRevisionChanged(zdo);
        }
      }
    }
  }

  void Execute(PeersEnumerable peers, double timeBudgetSeconds)
  {
    var timeStartSeconds = Time.realtimeSinceStartupAsDouble;

    peers.Update();
    if (peers.Count is 0)
      return;

    var executeUntil = timeStartSeconds + timeBudgetSeconds;

    _sectorsToProcess.Clear();
    var zonesAroundPlayers = ZNet.instance.GetSyncedSimulationDistance().NearSimulationDistance - 1; // Config.General.ZonesAroundPlayers.Value;
    foreach (var peer in peers)
    {
      var playerSector = peer.GetSector();
      for (int x = playerSector.x - zonesAroundPlayers; x <= playerSector.x + zonesAroundPlayers; x++)
      {
        for (int y = playerSector.y - zonesAroundPlayers; y <= playerSector.y + zonesAroundPlayers; y++)
        {
          var sector = new Vector2s(x, y);
          if (!_sectors.TryGetValue(sector, out var state) || state is { Changed.Count: 0, Repeat.Count: 0 })
            continue;
          state.Peers.Add(peer);
          _sectorsToProcess.Add(state);
        }
      }
    }

    if (_sectorsToProcess.Count is 0)
      return;

    Processor.StaticPreProcess(peers);
    if (_preprocessors is null)
    {
      _preprocessors = [];
      foreach (var processor in _enabledProcessors)
      {
        processor.PreProcessInternal(peers);
        if (processor.HasPreProcessor)
          _preprocessors.Add(processor);
      }
    }
    else
    {
      foreach (var processor in _preprocessors)
        processor.PreProcessInternal(peers);
    }

    int processedZdos = 0;
    int totalZdos = 0;

    foreach (var sectorState in _sectorsToProcess)
    {
      if (sectorState.Repeat.Count is not 0)
      {
        foreach (var zdo in sectorState.Repeat)
        {
          if (!zdo.ZDO.IsValid())
            continue;

          if (zdo.ScheduleBefore > executeUntil)
            _repeat.Add(zdo);
          else
            sectorState.Changed.Add(zdo);
        }
        sectorState.Repeat.Clear();

        foreach (var zdo in _repeat)
        {
          var sector = zdo.ZDO.GetSector();
          if (!_sectors.TryGetValue(sector, out var state))
            _sectors.Add(sector, state = new());
          state.Repeat.Add(zdo);
        }
        _repeat.Clear();
      }

      if (sectorState.Changed.Count is not 0)
      {
        totalZdos += sectorState.Changed.Count;
        _currentlyProcessing = sectorState;
        foreach (var zdo in sectorState.Changed)
        {
          processedZdos++;
          if (!zdo.ZDO.IsValid())
            continue;

          zdo.PrefabInfo ??= GetPrefabInfo(zdo.ZDO.GetPrefab());

          if (!zdo.HasProcessors)
            continue;

          ProcessZdo(sectorState.Peers, zdo);
        }
        sectorState.Changed.Clear();
        _currentlyProcessing = null;
      }
      sectorState.Peers.Clear();
    }

    if (processedZdos < totalZdos)
      _unfinishedProcessingInRow++;
    else
      _unfinishedProcessingInRow = 0;

    //#if DEBUG
    //    var logLevel = _unfinishedProcessingInRow is 0 ? LogLevel.Debug : LogLevel.Info;
    //#else
    //        if (!Config.DiagnosticLogs.Value)
    //            return;
    //        var logLevel = _unfinishedProcessingInRow is 0 ? LogLevel.Debug : LogLevel.Info;
    //#endif

    //    var elapsedMs = (Time.realtimeSinceStartupAsDouble - timeStartSeconds) * 1000;
    //    Logger.Log(logLevel,
    //        Invariant($"{nameof(Execute)} took {elapsedMs:F2} ms (budget: {timeBudgetSeconds * 1000:F2} ms) to process {processedZdos} of {totalZdos} ZDOs in {processedSectors} of {_playerSectors.Count} zones. Incomplete runs in row: {_unfinishedProcessingInRow}"));

    //    if (logLevel is > LogLevel.Info or LogLevel.None)
    //      return;

    //(_processingTimes ??= new(Processor.DefaultProcessors.Count)).Clear();
    //foreach (var processor in Processor.DefaultProcessors.AsEnumerable())
    //{
    //  var time = Math.Round(processor.ProcessingTimeSeconds * 1000, 2);
    //  if (time <= 0)
    //    continue;
    //  _processingTimes.Add((processor, time));
    //}
    //if (_processingTimes.Count is 0)
    //  return;
    //_processingTimes.Sort(static (a, b) => Math.Sign(b.Item2 - a.Item2));
    //Logger.Log(logLevel, Invariant($"Processing Time: {string.Join($", ", _processingTimes.Select(static x => Invariant($"{x.Item1.GetType().Name}: {x.Item2}ms")))}"));
  }

  void ProcessZdo(IReadOnlyList<Peer> peers, ServersideQoLZDO zdo)
  {
    zdo.ScheduleBefore = float.NaN;

    if (!zdo.ExclusivityCheckDone)
    {
      zdo.ExclusivityCheckDone = true;
      var allProcessors = zdo.Processors;
      if (allProcessors.Count > 1)
      {
        Processor? claimedExclusiveBy = null;
        foreach (var processor in allProcessors.Enumerate())
        {
          if (!processor.ClaimExclusive(zdo))
            continue;
          if (claimedExclusiveBy is null)
            claimedExclusiveBy = processor;
          else if (Config.DiagnosticLogs.Value)
            Logger.LogError(Invariant($"ZDO {zdo.PrefabInfo?.PrefabName} claimed exclusively by {processor.GetType().Name} while already claimed by {claimedExclusiveBy.GetType().Name}"));
        }

        if (claimedExclusiveBy is not null)
          zdo.UnregisterAllExcept(claimedExclusiveBy);
      }
    }

    var destroy = false;
    var recreate = false;
    _unregister.Clear();
    _reregisterOnRecreate.Clear();
    foreach (var processor in zdo.Processors.Enumerate())
    {
      var result = processor.ProcessInternal(peers, zdo);
      if (destroy = (result & Processor.ProcessResult.DestroyZDO) is not 0)
      {
        zdo.Destroy();
        break;
      }

      if ((result & Processor.ProcessResult.RecreateZDO) is not 0)
        recreate = true;

      var unregister = (result & Processor.ProcessResult.UnregisterProcessor) is not 0;
      if (unregister)
      {
        var reregisterOnRecreate = (result & Processor.ProcessResult.ReregisterOnRecreated) is not 0;
        if (!(recreate && reregisterOnRecreate))
          _unregister.Add(processor);
        else if (reregisterOnRecreate)
          _reregisterOnRecreate.Add(processor);
      }

      if (!recreate && !unregister && (result & Processor.ScheduleReprocessingConst) is not 0)
        ScheduleReprocessing(zdo, processor.ScheduleReprocessingDelay);

      if ((result & Processor.ProcessResult.SkipOtherProcessors) is not 0)
        break;
    }
    if (!destroy)
    {
      if (recreate && _reregisterOnRecreate.Count > 0)
      {
        foreach (var processor in _reregisterOnRecreate)
          _unregister.Remove(processor);
      }

      if (_unregister.Count > 0)
        zdo.Unregister(_unregister);
      if (recreate)
        zdo.Recreate();
    }
  }

  // Priority‑aware topological sort. Implementation could probably be more efficient, but this method is called seldomly and nowhere near a hot path.
  void SortProcessors(List<Processor> processors, bool isPrefabList)
  {
    var graph = new Dictionary<Processor, List<Processor>>(processors.Count);
    var inDegree = new Dictionary<Processor, int>(processors.Count);
    var dependencyAttributes = processors.ToDictionary(static x => x, static x => x.GetType().GetCustomAttributes<ProcessorDependencyAttribute>().ToList());

    HashSet<Guid>? dependents = null;

    for (int i = processors.Count - 1; i >= 0; i--)
    {
      var processor = processors[i];
      if (!isPrefabList && processor.Attribute.OnlyWhenDependedOn)
      {
        dependents ??= [.. dependencyAttributes.Values.SelectMany(static x => x.Select(static x => x.ProcessorId))];
        if (!dependents.Contains(processor.Attribute.Id))
        {
          processors.RemoveAt(i);
          Logger.DevLog($"Dropping processor {processor.GetType().FullName} because no dependents where found");
          continue;
        }
      }
      graph.Add(processor, []);
      inDegree.Add(processor, 0);
    }

    for (int i = processors.Count - 1; i >= 0; i--)
    {
      var processor = processors[i];

      if (!dependencyAttributes.TryGetValue(processor, out var list))
        continue;

      foreach (var attr in list)
      {
        if (!_processorsById.TryGetValue(attr.ProcessorId, out var dependency) || !graph.ContainsKey(dependency))
        {
          if (!isPrefabList && attr.Required)
          {
            if (!isPrefabList)
              Logger.DevLog($"Dropping processor {processor.GetType().FullName} because required dependency ({nameof(RunBeforeAttribute)}) {attr.ProcessorId} is missing");
            processors.RemoveAt(i);
            break;
          }
          continue;
        }

        if (attr.RunBefore is true)
        {
          graph[processor].Add(dependency);
          inDegree[dependency]++;
        }
        else if (attr.RunBefore is false)
        {
          graph[dependency].Add(processor);
          inDegree[processor]++;
        }
      }
    }

    var ready = new List<Processor>();

    foreach (var (processor, degree) in inDegree)
    {
      if (degree is 0)
        ready.Add(processor);
    }

    var expectedCount = processors.Count;
    processors.Clear();

    while (ready.Count > 0)
    {
      ready.Sort(static (a, b) => b.Attribute.Priority.CompareTo(a.Attribute.Priority));

      var node = ready[^1];
      ready.RemoveAt(ready.Count - 1);
      if (!isPrefabList && node.Attribute.Priority is not 0 && ready.Count > 0 && ready[^1] is { } next && next.Attribute.Priority == node.Attribute.Priority)
        Logger.LogWarning($"Processors {node.GetType().FullName} and {next.GetType().FullName} share the same non-default priority ({node.Attribute.Priority})");

      processors.Add(node);

      foreach (var neighbor in graph[node])
      {
        if (--inDegree[neighbor] is 0)
          ready.Add(neighbor);
      }
    }

    if (isPrefabList)
      return;

    if (processors.Count != expectedCount)
    {
      var notAdded = inDegree.Where(static x => x.Value > 0).Select(static x => $"{x.Key.Attribute.Id} ({x.Key.GetType().FullName})");
      Logger.LogError($"The following processors are not used due to cyclic dependencies: {string.Join(", ", notAdded)}");
    }

    Logger.DevLog(string.Join($"{Environment.NewLine}  - ", processors.Select(static x => $"{x.Attribute.Id} ({x.GetType().FullName})").Prepend("Processor order:")));
  }

  internal void ScheduleReprocessing(ServersideQoLZDO zdo, float delayInSeconds)
  {
    zdo.DelaySchedulingFor(delayInSeconds);
    var sector = zdo.ZDO.GetSector();
    if (!_sectors.TryGetValue(sector, out var state))
      _sectors.Add(sector, state = new());
    state.Repeat.Add(zdo);
  }

  internal PrefabInfo GetPrefabInfo(int prefab) => _prefabInfos.GetOrAdd(prefab, prefabHash =>
  {
    PrefabInfo? prefabInfo = null;
    if (ZNetScene.instance.GetPrefab(prefabHash) is { } prefab &&
      prefab.GetComponent<ZNetView>()?.gameObject.GetComponentsInChildren<MonoBehaviour>() is { } availableComponents)
    {
      prefabInfo = _prefabInfoFactory();
      Dictionary<Type, IReadOnlyList<MonoBehaviour>>? components = null;
      foreach (var group in availableComponents
        .Where(static x => x.GetType().Assembly == typeof(ZNetView).Assembly)
        .GroupBy(static x => x.GetType()))
      {
        IReadOnlyList<MonoBehaviour> list = [.. group];
        (components ??= [])[group.Key] = list;
        for (var type = group.Key.BaseType; type != typeof(MonoBehaviour); type = type.BaseType)
          components.TryAdd(type, list);
      }

      if (components?.ContainsKey(typeof(Piece)) is true && PieceTablesByPieceName.TryGetValue(prefab.name, out var pieceTable))
        components.Add(typeof(PieceTable), [pieceTable]);
      prefabInfo.Init(prefab, prefabHash, prefab.name, components);

      foreach (var plugin in __plugins)
      {
        foreach (var processor in plugin.Processors)
        {
          if (!processor.InitializePrefabInfoInternal(prefabInfo))
            continue;

          prefabInfo.AvailableProcessors.Add(processor);
          if (plugin.Config.Enabled.Value)
            prefabInfo.EnabledProcessors.Add(processor);
        }
      }
      SortProcessors(prefabInfo.EnabledProcessors, isPrefabList: true);
    }
    return prefabInfo ?? DummyPrefabInfo.Instance;
  });

  sealed class DummyPrefabInfo : PrefabInfo
  {
    public static DummyPrefabInfo Instance { get; } = new();

    DummyPrefabInfo() { Init(default!, 0, "Dummy", null); }
  }

  [HarmonyPatch]
  static class PrefabChangedPatches
  {
    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> GetTargetMethods()
    {
      var zdo = new ZDO();
      yield return ((Delegate)zdo.SetPrefab).Method;
      yield return ((Delegate)zdo.Deserialize).Method;
      yield return ((Delegate)zdo.Load).Method;
      yield return ((Delegate)zdo.LoadOldFormat).Method;
      yield return ((Delegate)zdo.Reset).Method;
    }

    [HarmonyPostfix]
    public static void OnPrefabChanged(ZDO __instance)
    {
      var zdo = __instance.ServersideQoLZDO;
      if (zdo.UpdatePrefab())
        Instance.OnPrefabChanged(zdo);
    }
  }

  [HarmonyPatch]
  static class DataOrOwnerRevisionChangedPatches
  {
    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> GetTargetMethods() => [
      typeof(ZDO).GetProperty(nameof(ZDO.DataRevision), BindingFlags.Instance | BindingFlags.Public)!.SetMethod,
      typeof(ZDO).GetProperty(nameof(ZDO.OwnerRevision), BindingFlags.Instance | BindingFlags.Public)!.SetMethod];

    [HarmonyPostfix]
    public static void OnDataOrOwnerRevisionChanged(ZDO __instance)
    {
      var zdo = __instance.ServersideQoLZDO;
      if (zdo.UpdateOwnerAndDataRevisions() is { DataRevChanged: true } /*or { OwnerRevChanged: true }*/)
        Instance.OnDataOrOwnerRevisionChanged(zdo);
    }
  }

  [HarmonyPatch(typeof(ZoneSystem), "SendGlobalKeys")]
  static class ZoneSystemSendGlobalKeys
  {
    static readonly Dictionary<string, string> __prevKeys = [];

    public static void Prefix(ZoneSystem __instance, long peer)
    {
      if (peer != ZRoutedRpc.Everybody)
        return;

      var changed = false;

      if (Instance.GlobalKeysChanged is not null && !__prevKeys.Keys.SequenceEqual(__instance.m_globalKeysValues.Keys))
      {
        changed = true;
        Logger.DevLog($"Invoking {nameof(GlobalKeysChanged)} event");
        Instance.GlobalKeysChanged();
      }

      if (Instance.GlobalKeyValuesChanged is not null && !__prevKeys.SequenceEqual(__instance.m_globalKeysValues))
      {
        changed = true;
        Logger.DevLog($"Invoking {nameof(GlobalKeyValuesChanged)} event");
        Instance.GlobalKeyValuesChanged();
      }

      if (!changed)
        return;

      __prevKeys.Clear();
      foreach (var (key, value) in __instance.m_globalKeysValues)
        __prevKeys.Add(key, value);
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
      //foreach (var instruction in instructions)
      //{
      //    Main.Instance.Logger.DevLog($"{instruction.opcode}: {instruction.operand}");
      //    yield return instruction;
      //}

      var listCtor = typeof(List<string>).GetConstructor([typeof(IEnumerable<string>)]);
      var method = ((Delegate)ModfiyGlobalKeys).Method;

      return new CodeMatcher().Start().Insert(instructions).Start()
          .MatchForward(false, new CodeMatch(new CodeInstruction(OpCodes.Newobj, listCtor)))
          .Advance(1)
          .Insert(
            // Load "peer" argument
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, method)
          )
          .ThrowIfInvalid($"Failed to apply patch {nameof(ZoneSystemSendGlobalKeys)}.{nameof(Transpiler)}")
          .InstructionEnumeration();

      static List<string> ModfiyGlobalKeys(List<string> globalKeys, long peer)
      {
        if (Processor.Instance<PlayerRegistryProcessor>().GetStateForPeerID(peer) is not { } state)
          return globalKeys;

        foreach (var (key, (add, value)) in state.GlobalKeyModifications)
        {
          if (value is not null)
          {
            var newKey = Invariant($"{key.Key} {value}");
            var idx = globalKeys.FindIndex(x => x.Length > key.Key.Length && x[key.Key.Length] is ' ' && x.StartsWith(key.Key));
            if (idx > -1)
              globalKeys[idx] = newKey;
            else
              globalKeys.Add(newKey);
          }
          else if (!add!.Value)
            globalKeys.Remove(key);
          else if (!globalKeys.Contains(key))
            globalKeys.Add(key);
        }
        return globalKeys;
      }
    }
  }


  [Conditional("DEBUG")]
  static void GenerateDocs()
  {
#if DEBUG
    var docsPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(DependencyDirectory)), "Docs");
    Logger.DevLog($"Generating docs in {docsPath} ...");
    var docsComponentsPath = Path.Combine(docsPath, "Components");

    try { Directory.Delete(docsComponentsPath, true); } catch (DirectoryNotFoundException) { }

    Directory.CreateDirectory(docsComponentsPath);

    HashSet<Type> validFieldTypes = [typeof(int), typeof(float), typeof(bool), typeof(Vector3), typeof(string), typeof(GameObject), typeof(ItemDrop)];
    var componentFields = new ConcurrentDictionary<Type, IReadOnlyList<FieldInfo>>();
    var prefabs = new ConcurrentBag<(string Prefab, string? Name, string Components)>();
    var prefabsFx = new ConcurrentBag<(string Prefab, string? Name, string Components)>();
    var prefabsSfx = new ConcurrentBag<(string Prefab, string? Name, string Components)>();
    var prefabsVfx = new ConcurrentBag<(string Prefab, string? Name, string Components)>();
    var componentsBag = new ConcurrentDictionary<MonoBehaviour, string>();
    Parallel.ForEach(ZNetScene.instance.m_prefabs, prefab =>
    {
      var components = prefab.GetComponent<ZNetView>()?.gameObject.GetComponentsInChildren<MonoBehaviour>()
              .Where(static x => x is not ZNetView)
              .ToList();

      if (components is not { Count: > 0 })
        return;

      string? name = null;

      for (int i = components.Count - 1; i >= 0; i--)
      {
        var component = components[i];
        var fields = componentFields.GetOrAdd(component.GetType(), type => type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => validFieldTypes.Contains(x.FieldType))
                .ToList());

        if (fields.Count is 0)
        {
          components.RemoveAt(i);
          continue;
        }

        componentsBag.TryAdd(component, prefab.name);
        name ??= component switch
        {
          ItemDrop itemDrop => itemDrop.m_itemData.m_shared.m_name,
          _ => component.GetType().GetField("m_name")?.GetValue(component) as string
        };
      }

      var bag = prefabs;
      if (prefab.name.StartsWith("fx_"))
        bag = prefabsFx;
      else if (prefab.name.StartsWith("sfx_"))
        bag = prefabsSfx;
      else if (prefab.name.StartsWith("vfx_"))
        bag = prefabsVfx;

      // markdown link: ' ' -> '-', remove non-alphanumeric characters
      bag.Add((prefab.name, name, string.Join(", ", components
                  .Select(static x => (Type: x.GetType().Name, Name: x.name))
                  .OrderBy(static x => x.Type).ThenBy(static x => x.Name)
                  .Select(x => Invariant($"[{x.Type} ({x.Name})](Components/{x.Type}.md#{prefab.name.ToLowerInvariant().Replace(' ', '-')}-{x.Name.ToLowerInvariant().Replace(' ', '-')})")))));
    });

    Parallel.ForEach(componentsBag.GroupBy(static x => x.Key.GetType()), group =>
    {
      var componentType = group.Key;
      var fields = componentFields[componentType];

      using var writer = new StreamWriter(Path.Combine(docsComponentsPath, Invariant($"{componentType.Name}.md")), false, new UTF8Encoding(false));
      writer.WriteLine(Invariant($"# {componentType.Name}"));
      writer.WriteLine();
      writer.WriteLine("The following section headers are in the format `Prefab.name: Component.name`.");
      writer.WriteLine();
      foreach (var (component, header) in group.Select(static x => (x.Key, Invariant($"## {x.Value}: {x.Key.name}"))).OrderBy(static x => x.Item2))
      {
        writer.WriteLine(header);
        writer.WriteLine();
        writer.WriteLine("|Field|Type|Default Value|");
        writer.WriteLine("|-----|----|-------------|");
        foreach (var field in fields)
        {
          var value = field.GetValue(component);
          if (value is UnityEngine.Object obj)
            value = obj.name;
          writer.WriteLine(Invariant($"|{field.Name}|{field.FieldType}|{value ?? "*null*"}|"));
        }
        writer.WriteLine();
      }
    });

    WritePrefabsFile(docsPath, "Prefabs.md", prefabs);
    WritePrefabsFile(docsPath, "PrefabsFX.md", prefabsFx);
    WritePrefabsFile(docsPath, "PrefabsSFX.md", prefabsSfx);
    WritePrefabsFile(docsPath, "PrefabsVFX.md", prefabsVfx);

    WriteLocalizationsFile(docsPath, "Localization.md");
    WriteEventsFile(docsPath, "RandomEvents.md");
    WriteRpcFile(docsPath, "RPC.md");

    Logger.DevLog("Generating docs done");
    return;

    static void WritePrefabsFile(string path, string filename, IEnumerable<(string Prefab, string? Name, string Components)> prefabs)
    {
      using var writer = new StreamWriter(Path.Combine(path, filename), false, new UTF8Encoding(false));
      writer.WriteLine("# Prefabs");
      writer.WriteLine();
      writer.WriteLine("|Prefab|Components|");
      writer.WriteLine("|------|----------|");
      foreach (var (prefab, name, components) in prefabs.OrderBy(static x => x.Prefab))
      {
        var str = $"{prefab}<small><br>- Hash: {prefab.GetStableHashCode()}";
        if (name is not null)
        {
          str += $"<br>- Name: {name}";
          if (Localization.instance.Localize(name) is { } localized && localized != name)
            str += $"<br>- English Name: {localized}";
        }
        str += "</small>";
        writer.WriteLine(Invariant($"|{str}|{components}|"));
      }
    }

    static void WriteLocalizationsFile(string path, string filename)
    {
      using var writer = new StreamWriter(Path.Combine(path, filename), false, new UTF8Encoding(false));
      writer.WriteLine("# Localization");
      writer.WriteLine();
      writer.WriteLine("|Key|English|");
      writer.WriteLine("|---|-------|");
      foreach (var (key, value) in Localization.instance.GetStrings().Select(static x => (x.Key, x.Value)).OrderBy(static x => x.Key))
        writer.WriteLine(Invariant($"|{key}|{value?.Replace("\n", "<br>") ?? "*null*"}|"));
    }

    static void WriteEventsFile(string path, string filename)
    {
      using var writer = new StreamWriter(Path.Combine(path, filename), false, new UTF8Encoding(false));
      writer.WriteLine("# Random Events");
      writer.WriteLine();
      writer.WriteLine("|Name|Player: required **not** known items (all)|Player: required **not** set keys (all)|Player: required known items (any)|Player: required keys (any)|Player: required keys (all)|");
      writer.WriteLine("|----|------------------------------------------|---------------------------------------|----------------------------------|---------------------------|---------------------------|");
      foreach (var ev in RandEventSystem.instance.m_events.Where(static x => x.m_enabled && x.m_random).OrderBy(static x => x.m_name))
      {
        //Instance.Logger.DevLog($"{ev.m_name}: {string.Join(", ", ev.m_notRequiredGlobalKeys)} / {string.Join(", ", ev.m_requiredGlobalKeys)}");
        /// <see cref="RandEventSystem.PlayerIsReadyForEvent(Player, RandomEvent)"/>
        var altRequiredNotKnownItems = string.Join("<br>", ev.m_altRequiredNotKnownItems.Select(static x => $"- {x.name}"));
        var altNotRequiredPlayerKeys = string.Join("<br>", ev.m_altNotRequiredPlayerKeys.Select(static x => $"- {x}"));
        var altRequiredKnownItems = string.Join("<br>", ev.m_altRequiredKnownItems.Select(static x => $"- {x.name}"));
        var altRequiredPlayerKeysAny = string.Join("<br>", ev.m_altRequiredPlayerKeysAny.Select(static x => $"- {x}"));
        var altRequiredPlayerKeysAll = string.Join("<br>", ev.m_altRequiredPlayerKeysAll.Select(static x => $"- {x}"));
        writer.WriteLine(Invariant($"|{ev.m_name}|{altRequiredNotKnownItems}|{altNotRequiredPlayerKeys}|{altRequiredKnownItems}|{altRequiredPlayerKeysAny}|{altRequiredPlayerKeysAll}|"));
      }
    }

    static void WriteRpcFile(string path, string filename)
    {
      using var writer = new StreamWriter(Path.Combine(path, filename), false, new UTF8Encoding(false));
      writer.WriteLine("# RPC");
      writer.WriteLine();
      writer.WriteLine("|Type|Method|Parameters|");
      writer.WriteLine("|----|------|----------|");

      foreach (var type in typeof(ZNet).Assembly.ExportedTypes.OrderBy(static x => x.Name))
      {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).OrderBy(static x => x.Name))
        {
          if (!method.Name.StartsWith("RPC_"))
            continue;
          var parameters = string.Join(", ", method.GetParameters().Select(static x => $"{x.ParameterType.Name} {x.Name}"));
          writer.WriteLine($"|{type.Name}|{method.Name}|{parameters}|");
        }
      }
    }
#endif
  }
}
