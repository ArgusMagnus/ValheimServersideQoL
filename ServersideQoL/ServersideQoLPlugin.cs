using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ServersideQoL;

partial class ServersideQoLPlugin : ServersideQoLPluginBase<ServersideQoLPlugin, Config>
{
  internal static readonly int PluginGuidHash = "argusmagnus.ServersideQoL".GetStableHashCode(); // use old GUID here to not break existing worlds

  static readonly HashSet<IServersideQoLPlugin> __plugins = [];
  readonly Dictionary<Guid, Processor> _processorsById = [];
  readonly List<Processor> _enabledProcessors = [];

  internal static Harmony HarmonyInstance { get; } = new(PluginGuid);
  internal IReadOnlyDictionary<Guid, Processor> Processors => _processorsById;
  public event Action? GlobalKeysChanged;
  public event Action? GlobalKeyValuesChanged;

  internal static void RegisterPlugin(IServersideQoLPlugin plugin)
      => __plugins.Add(plugin);

  Func<PrefabInfo> _prefabInfoFactory = default!;
  readonly ConcurrentDictionary<int, PrefabInfo?> _prefabInfos = [];

  readonly GameVersion ExpectedGameVersion = GameVersion.ParseGameVersion("0.221");
  const uint ExpectedNetworkVersion = 35;
  const uint ExpectedItemDataVersion = 106;
  const uint ExpectedWorldVersion = 36;

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
  List<(Processor, double)>? _processingTimes;

  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  partial void OnAwake()
  {
    HarmonyInstance.PatchAll(typeof(ServersideQoLPlugin).Assembly);
  }

  void Start()
  {
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
    foreach (var plugin in __plugins)
    {
      var config = plugin.Config; // Initialize
      config.ConfigChanged -= OnConfigChanged;
      config.ConfigChanged += OnConfigChanged;
    }

    //if (_mainConfig is not null)
    //    _mainConfig.ConfigFile.SettingChanged -= OnConfigChanged;
    //if (_worldConfig is not null)
    //    _worldConfig.ConfigFile.SettingChanged -= OnConfigChanged;
    //_worldConfig = null;

    //if (Config.General.ConfigPerWorld.Value)
    //{
    //    var path = ZNet.World.GetRootPath(FileHelpers.FileSource.Local);
    //    path = $"{path}.{PluginName}.cfg";
    //    if (!File.Exists(path) && File.Exists(base.Config.ConfigFilePath))
    //        File.Copy(base.Config.ConfigFilePath, path);

    //    var srcDir = Path.Combine(Path.GetDirectoryName(base.Config.ConfigFilePath), Path.GetFileNameWithoutExtension(base.Config.ConfigFilePath));
    //    if (Directory.Exists(srcDir))
    //    {
    //        var dstDir = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
    //        Directory.CreateDirectory(dstDir);
    //        foreach (var file in Directory.EnumerateFiles(srcDir))
    //        {
    //            var dstFile = Path.Combine(dstDir, Path.GetFileName(file));
    //            if (!File.Exists(dstFile))
    //                File.Copy(file, dstFile);
    //        }
    //    }

    //    Logger.LogInfo("Using world config file");
    //    _worldConfig = new(new(path, saveOnInit: false, new(PluginGuid, PluginName, PluginVersion)));
    //}

    var cfg = Config;
    Logger.LogInfo(Invariant($"Enabled: {cfg.Enabled.Value}, DiagnosticLogs: {cfg.DiagnosticLogs.Value}"));

    if (!cfg.Enabled.Value)
      return false;

    if (Chainloader.PluginInfos.TryGetValue("org.bepinex.plugins.dedicatedserver", out var pluginInfo))
      Logger.LogWarning($"Many features are incompatible with {pluginInfo.Metadata.Name}");

    if (cfg.DiagnosticLogs.Value)
      Logger.LogInfo(string.Join($"{Environment.NewLine}  ", ["Config:", .. Config.ConfigFile.Select(static x => Invariant($"[{x.Key.Section}].[{x.Key.Key}] = {x.Value.BoxedValue}"))]));

    var failed = false;
    var abort = false;
    //if (RuntimeInformation.Instance.GameVersion != ExpectedGameVersion)
    //{
    //    Logger.LogWarning(Invariant($"Unsupported game version: {RuntimeInformation.Instance.GameVersion}, expected: {ExpectedGameVersion}"));
    //    failed = true;
    //    abort |= !cfg.IgnoreGameVersionCheck.Value;
    //}
    //if (RuntimeInformation.Instance.NetworkVersion != ExpectedNetworkVersion)
    //{
    //    Logger.LogWarning(Invariant($"Unsupported network version: {RuntimeInformation.Instance.NetworkVersion}, expected: {ExpectedNetworkVersion}"));
    //    failed = true;
    //    abort |= !cfg.IgnoreNetworkVersionCheck.Value;
    //}
    //if (RuntimeInformation.Instance.ItemDataVersion != ExpectedItemDataVersion)
    //{
    //    Logger.LogWarning(Invariant($"Unsupported item data version: {RuntimeInformation.Instance.ItemDataVersion}, expected: {ExpectedItemDataVersion}"));
    //    failed = true;
    //    abort |= !cfg.IgnoreItemDataVersionCheck.Value;
    //}
    //if (RuntimeInformation.Instance.WorldVersion != ExpectedWorldVersion)
    //{
    //    Logger.LogWarning(Invariant($"Unsupported world version: {RuntimeInformation.Instance.WorldVersion}, expected: {ExpectedWorldVersion}"));
    //    failed = true;
    //    abort |= !cfg.IgnoreWorldVersionCheck.Value;
    //}

    if (failed)
    {
      if (!abort)
        Logger.LogError("Version checks failed, but you chose to ignore the checks (config). Continuing...");
      else
      {
        Logger.LogError("Version checks failed. Mod execution is stopped");
        return false;
      }
    }

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
    var zonesAroundPlayers = ZoneSystem.instance.m_activeArea - 1; // Config.General.ZonesAroundPlayers.Value;
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
    foreach (var processor in _enabledProcessors)
      processor.PreProcessInternal(peers);

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

          if (zdo.PrefabInfo is null)
          {
            zdo.PrefabInfo = GetPrefabInfo(zdo.ZDO.GetPrefab());
            if (zdo.PrefabInfo is null)
              continue;
          }

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
      var allProcessors = zdo.Processors!;
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
            Logger.LogError(Invariant($"ZDO {zdo.ZDO.m_uid} claimed exclusive by {processor.GetType().Name} while already claimed by {claimedExclusiveBy.GetType().Name}"));
        }

        if (claimedExclusiveBy is not null)
          zdo.UnregisterAllExcept(claimedExclusiveBy);
      }
    }

    var destroy = false;
    var recreate = false;
    _unregister.Clear();
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
        _unregister.Add(processor);
      
      if (!recreate && !unregister && (result & Processor.ProcessResult.ScheduleReprocessing) is not 0)
        ScheduleReprocessing(zdo);

      if ((result & Processor.ProcessResult.SkipOtherProcessors) is not 0)
        break;
    }
    if (!destroy)
    {
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
        else if(attr.RunBefore is false)
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

  internal void ScheduleReprocessing(ServersideQoLZDO zdo)
  {
    var sector = zdo.ZDO.GetSector();
    if (!_sectors.TryGetValue(sector, out var state))
      _sectors.Add(sector, state = new());
    state.Repeat.Add(zdo);
  }

  internal void ScheduleReprocessing(ServersideQoLZDO zdo, float delayInSeconds)
  {
    zdo.DelaySchedulingFor(delayInSeconds);
    ScheduleReprocessing(zdo);
  }

  internal PrefabInfo? GetPrefabInfo(int prefab) => _prefabInfos.GetOrAdd(prefab, prefabHash =>
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
        (components ??= []).Add(group.Key, list);
        for (var type = group.Key.BaseType; type != typeof(MonoBehaviour); type = type.BaseType)
          components.Add(type, list);
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
    return prefabInfo;
  });

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
      if (zdo.UpdateOwnerAndDataRevisions())
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

        foreach (var (key, add) in state.GlobalKeyModifications)
        {
          if (!add)
            globalKeys.Remove(key);
          else if (!globalKeys.Contains(key))
            globalKeys.Add(key);
        }
        return globalKeys;
      }
    }
  }
}
