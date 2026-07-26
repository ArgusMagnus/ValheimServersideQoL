using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Concurrent;
using System.Reflection;
using UnityEngine;

namespace ServersideQoL;

partial class ServersideQoLPlugin : ServersideQoLPluginBase<ServersideQoLPlugin, Config>
{
  public static readonly int PluginGuidHash = "argusmagnus.ServersideQoL".GetStableHashCode(); // use old GUID here to not break existing worlds

  static readonly HashSet<IServersideQoLPlugin> __plugins = [];
  readonly Dictionary<Guid, Processor> _processorsById = [];
  readonly List<Processor> _enabledProcessors = [];
  bool _hasCyclicProcessors;

  internal static ServersideQoLPlugin Instance { get; private set; } = default!;
  internal static Harmony HarmonyInstance { get; } = new(PluginGuid);
  internal IReadOnlyDictionary<Guid, Processor> Processors => _processorsById;

  internal static void RegisterPlugin(IServersideQoLPlugin plugin)
      => __plugins.Add(plugin);

  Func<PrefabInfo> _prefabInfoFactory = default!;
  readonly ConcurrentDictionary<int, PrefabInfo?> _prefabInfos = [];

  readonly GameVersion ExpectedGameVersion = GameVersion.ParseGameVersion("0.221");
  const uint ExpectedNetworkVersion = 35;
  const uint ExpectedItemDataVersion = 106;
  const uint ExpectedWorldVersion = 36;


  readonly HashSet<IConfig> _configChanged = [];
  uint _unfinishedProcessingInRow;
  record SectorInfo(List<Peer> Peers, List<ZDO> ZDOs)
  {
    public int ZdoIndex { get; set; }
    public int InverseWeight { get; set; }
  }
  readonly Stack<SectorInfo> _sectorInfoPool = [];
  Dictionary<Vector2s, SectorInfo> _playerSectors = [];
  Dictionary<Vector2s, SectorInfo> _playerSectorsOld = [];
  List<(Processor, double)>? _processingTimes;

  readonly List<Processor> _unregister = [];
  ConcurrentDictionary<ServersideQoLZDO, object?>? _changed = [];
  readonly HashSet<ServersideQoLZDO> _repeat = [];

  public ServersideQoLPlugin() => Instance = this;

  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  void Start()
  {
    StartCoroutine(CallExecute());

    IEnumerator<YieldInstruction?> CallExecute()
    {
      bool processorsInitialized = false;

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

        if (!processorsInitialized)
        {
          processorsInitialized = true;
          if (!InitializeProcessors())
            yield break;
        }

        while (ZDOMan.instance is null || ZNetScene.instance is null || ZNet.World is null)
          yield return new WaitForSeconds(0.2f);

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

  bool InitializeProcessors()
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
    _hasCyclicProcessors = _enabledProcessors.Any(static x => x.Attribute.Cyclic);

    _prefabInfoFactory = prefabInfoBuilder.GetFactory();

    foreach (var plugin in __plugins)
      plugin.Config.ConfigChanged += OnConfigChanged;

    HarmonyInstance.PatchAll(typeof(ServersideQoLPlugin).Assembly);

    return true;
  }

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<ContainerRegistryProcessor>()
    .Add<TameableRegistryProcessor>()
    .Add<PlayerRegistryProcessor>();

  IReadOnlyDictionary<string, PieceTable> PieceTablesByPieceName => field ?? new Func<IReadOnlyDictionary<string, PieceTable>>(static () =>
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

  void OnPrefabChanged(ServersideQoLZDO zdo)
  {
    // may be called from field initializers which may be called from other threads

    PrefabInfo? prefabInfo;
    if (!zdo.ZDO.IsValid())
      prefabInfo = null;
    else if (!_prefabInfos.TryGetValue(zdo.ZDO.GetPrefab(), out prefabInfo))
      _changed?.TryAdd(zdo, null);

    zdo.PrefabInfo = prefabInfo;

    if (_changed is not null && prefabInfo is { EnabledProcessors.Count: > 0 })
      _changed.TryAdd(zdo, null);
  }

  void OnDataOrOwnerRevisionChanged(ServersideQoLZDO zdo)
  {
    // may be called from field initializers which may be called from other threads
    if (_changed is not null && zdo.HasProcessors)
      _changed.TryAdd(zdo, null);
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

    Processor.StaticInitialize();
    foreach (var processor in _enabledProcessors)
      processor.Initialize();

    return true;
  }

  void OnConfigChanged(object sender, SettingChangedEventArgs e)
  {
    var cfg = (IConfig)sender;
    _configChanged.Add(cfg);
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
      _hasCyclicProcessors = _enabledProcessors.Any(static x => x.Attribute.Cyclic);

      foreach (var prefabInfo in _prefabInfos.Values)
      {
        if (prefabInfo is null)
          continue;

        if (cfg.Enabled.Value)
        {
          foreach (var processor in cfg.Plugin.Processors)
            prefabInfo.EnabledProcessors.Add(processor);
          SortProcessors(prefabInfo.EnabledProcessors, isPrefabList: true);
          foreach (var processor in prefabInfo.EnabledProcessors)
          {
            if (processor.Attribute.Cyclic)
              prefabInfo.EnabledCyclicProcessors.Add(processor);
          }
        }
        else
        {
          foreach (var processor in cfg.Plugin.Processors)
          {
            prefabInfo.EnabledProcessors.Remove(processor);
            if (processor.Attribute.Cyclic)
              prefabInfo.EnabledCyclicProcessors.Remove(processor);
          }
        }
      }

      if (cfg.Enabled.Value)
      {
        foreach (var processor in cfg.Plugin.Processors)
          processor.Initialize();

        foreach (var zdo in ZDOMan.instance.GetObjects().Select(static x => x.ServersideQoLZDO))
        {
          zdo.ReregisterAll();
          zdo.ProcessorDataRevisions?.Clear();
          OnDataOrOwnerRevisionChanged(zdo);
        }
      }
    }
  }

  void Execute(PeersEnumerable peers, double timeBudgetSeconds)
  {
    var timeStartSeconds = Time.realtimeSinceStartupAsDouble;
    var executeUntil = timeStartSeconds + timeBudgetSeconds;

    peers.Update();

    if (_repeat.Count is not 0)
    {
      foreach (var zdo in _repeat)
        _changed!.TryAdd(zdo, null);
      _repeat.Clear();
    }
    else if (peers.Count is 0 && _changed!.Count is 0)
      return;

    //SharedProcessorState.CleanUp(peers);

    (_playerSectors, _playerSectorsOld) = (_playerSectorsOld, _playerSectors);
    var zonesAroundPlayers = ZoneSystem.instance.m_activeArea - 1; // Config.General.ZonesAroundPlayers.Value;
    foreach (var peer in peers)
    {
      var playerSector = ZoneSystem.GetZone(peer.RefPos);
      for (int x = playerSector.x - zonesAroundPlayers; x <= playerSector.x + zonesAroundPlayers; x++)
      {
        for (int y = playerSector.y - zonesAroundPlayers; y <= playerSector.y + zonesAroundPlayers; y++)
        {
          var sector = new Vector2s(x, y);
          if (_playerSectorsOld.Remove(sector, out var sectorInfo))
          {
            _playerSectors.Add(sector, sectorInfo);
            sectorInfo.InverseWeight = 0;
            sectorInfo.Peers.Clear();
            sectorInfo.Peers.Add(peer);
          }
          else if (_playerSectors.TryGetValue(sector, out sectorInfo))
          {
            sectorInfo.Peers.Add(peer);
          }
          else
          {
            if (_sectorInfoPool.TryPop(out sectorInfo))
              sectorInfo.Peers.Add(peer);
            else
              sectorInfo = new([peer], []);
            _playerSectors.Add(sector, sectorInfo);
          }
        }
      }
    }

    foreach (var sectorInfo in _playerSectorsOld.Values)
    {
      sectorInfo.ZdoIndex = 0;
      sectorInfo.InverseWeight = 0;
      sectorInfo.Peers.Clear();
      sectorInfo.ZDOs.Clear();
      _sectorInfoPool.Push(sectorInfo);
    }
    _playerSectorsOld.Clear();

    var playerSectors = _playerSectors;

    foreach (var processor in _enabledProcessors)
      processor.PreProcessInternal(peers);

    int processedSectors = 0;
    int processedZdos = 0;
    int totalZdos = 0;

    (var changed, _changed) = (_changed!, null);
    foreach (var zdo in changed.Keys)
    {
      processedZdos++;

      if (zdo.PrefabInfo is null)
      {
        zdo.PrefabInfo = GetPrefabInfo(zdo.ZDO.GetPrefab());
        if (zdo.PrefabInfo is null)
          continue;
      }

      playerSectors.TryGetValue(zdo.ZDO.GetSector(), out var sectorInfo);
      if (!zdo.ZDO.IsValid() || !zdo.HasProcessors)
        continue;

      ProcessZdo(sectorInfo?.Peers ?? [], zdo, false);
    }

    if (_hasCyclicProcessors)
    {
      foreach (var (sector, sectorInfo) in playerSectors)
      {
        if (Time.realtimeSinceStartupAsDouble > executeUntil)
          break;

        processedSectors++;

        if (sectorInfo is { ZDOs.Count: 0 })
          ZDOMan.instance.FindSectorObjects(sector, 0, 0, sectorInfo.ZDOs);

        totalZdos += sectorInfo.ZDOs.Count;

        for (; sectorInfo.ZdoIndex < sectorInfo.ZDOs.Count; sectorInfo.ZdoIndex++)
        {
          if (processedZdos % 10 is 0 && Time.realtimeSinceStartupAsDouble >= executeUntil)
            break;

          processedZdos++;
          var zdo = sectorInfo.ZDOs[sectorInfo.ZdoIndex].ServersideQoLZDO;
          if (!zdo.ZDO.IsValid() || !zdo.HasCyclicProcessors || changed.ContainsKey(zdo))
            continue;

          ProcessZdo(sectorInfo.Peers, zdo, true);
        }

        if (sectorInfo.ZdoIndex >= sectorInfo.ZDOs.Count)
        {
          sectorInfo.ZDOs.Clear();
          sectorInfo.ZdoIndex = 0;
        }
      }
    }

    //foreach (var processor in Processor.DefaultProcessors.AsEnumerable())
    //    processor.PostProcess();

    changed.Clear();
    _changed = changed;

    if (processedSectors < _playerSectors.Count || processedZdos < totalZdos)
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

  void ProcessZdo(IReadOnlyList<Peer> peers, ServersideQoLZDO zdo, bool cyclic)
  {
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
    foreach (var processor in (cyclic ? zdo.CyclicProcessors : zdo.Processors).Enumerate())
    {
      if (!zdo.CheckProcessorDataRevisionChanged(processor))
        continue;

      var result = processor.ProcessInternal(peers, zdo);
      if (destroy = (result & Processor.ProcessResult.DestroyZDO) is not 0)
      {
        zdo.Destroy();
        break;
      }

      if ((result & Processor.ProcessResult.RecreateZDO) is not 0)
        recreate = true;
      else if ((result & Processor.ProcessResult.UnregisterProcessor) is not 0)
        _unregister.Add(processor);
      else if ((result & Processor.ProcessResult.ScheduleReprocessing) is not 0)
        ScheduleReprocessing(zdo);
      else if ((result & Processor.ProcessResult.WaitForZDORevisionChange) is not 0)
        zdo.UpdateProcessorDataRevision(processor, onlyExisting: !processor.Attribute.Cyclic);
      else if (!cyclic)
        zdo.UpdateProcessorDataRevision(processor, onlyExisting: true);

      if ((result & Processor.ProcessResult.SkipOtherProcessors) is not 0)
        break;
    }
    if (!destroy)
    {
      if (recreate)
        zdo.Recreate();
      else if (_unregister.Count > 0)
        zdo.Unregister(_unregister);
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

  internal void ScheduleReprocessing(ServersideQoLZDO zdo) => _repeat.Add(zdo);

  internal PrefabInfo? GetPrefabInfo(int prefab) => _prefabInfos.GetOrAdd(prefab, prefabHash =>
  {
    PrefabInfo? prefabInfo = null;
    if (ZNetScene.instance.GetPrefab(prefabHash) is { } prefab &&
      prefab.GetComponent<ZNetView>()?.gameObject.GetComponentsInChildren<MonoBehaviour>() is { } availableComponents)
    {
      prefabInfo = _prefabInfoFactory();
      var components = availableComponents.GroupBy(static x => x.GetType()).ToDictionary(static x => x.Key, static x => (IReadOnlyList<MonoBehaviour>)[.. x]);
      if (prefab.GetComponent<Piece>() is not null && PieceTablesByPieceName.TryGetValue(prefab.name, out var pieceTable))
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
      foreach (var processor in prefabInfo.EnabledProcessors)
      {
        if (processor.Attribute.Cyclic)
          prefabInfo.EnabledCyclicProcessors.Add(processor);
      }
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
}
