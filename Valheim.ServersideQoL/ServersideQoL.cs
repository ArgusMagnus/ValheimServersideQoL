using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.Reflection;
using UnityEngine;
using Valheim.ZDOExtender;

namespace Valheim.ServersideQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(ZDOExtender.ZDOExtender.PluginGuid, ZDOExtender.ZDOExtender.PluginVersion)]
public sealed partial class ServersideQoL : BaseUnityPlugin
{
    public const string PluginName = nameof(ServersideQoL);
    public const string PluginGuid = $"argusmagnus.{PluginName}";

    readonly ExtendedZDOInterface<IZDOWithProcessors> _extendedZDOInterface = ZDOExtender.ZDOExtender.AddInterface<IZDOWithProcessors>();
    static Dictionary<Type, Processor>? _processors = [];
    internal static readonly Dictionary<Type, Func<IConfig>> _configFactories = [];
    internal static readonly Dictionary<Type, IConfig> _configs = [];

    internal static new readonly Logger Logger = new(PluginName);
    static new Config Config => Config.Instance;

    public static IReadOnlyList<Processor> Processors => field ??= new Func<IReadOnlyList<Processor>>(static () =>
    {
        var processors = _processors!;
        _processors = null;
        return [.. processors.OrderByDescending(static x => x.Key.GetCustomAttribute<ProcessorAttribute>()?.Priority ?? 0).Select(static x => x.Value)];
    }).Invoke();

    public static void AddProcessor<T>()
        where T : Processor, new()
    {
        if (_processors is null)
            throw new InvalidOperationException("Processor registration is closed.");

        var type = typeof(T);
        if (_processors.ContainsKey(type))
            return;

        var processor = Processor.Instance<T>();
        processor.ValidateProcessorInternal();
        _processors.Add(type, processor);
    }

    public static void AddConfig<T>(Func<T> factory)
        where T : ConfigBase<T>
        => _configFactories.Add(typeof(T), factory);

    readonly GameVersion ExpectedGameVersion = GameVersion.ParseGameVersion("0.221.4");
    const uint ExpectedNetworkVersion = 35;
    const uint ExpectedItemDataVersion = 106;
    const uint ExpectedWorldVersion = 36;

    ulong _executeCounter;
    readonly HashSet<Assembly> _configChanged = [];
    uint _unfinishedProcessingInRow;
    record SectorInfo(List<Peer> Peers, List<ZDO> ZDOs)
    {
        public int ZdoIndex { get; set; }
        public int InverseWeight { get; set; }
    }
    readonly Stack<SectorInfo> _sectorInfoPool = [];
    Dictionary<Vector2i, SectorInfo> _playerSectors = [];
    Dictionary<Vector2i, SectorInfo> _playerSectorsOld = [];
    List<(Processor, double)>? _processingTimes;

    readonly List<Processor> _unregister = [];

    void Awake()
    {
        AddConfig(() => new Config(base.Config, Logger));
    }

    void Start()
    {
        if (Processors.Count is 0)
        {
            Logger.LogWarning("No processors registered");
            return;
        }

        StartCoroutine(CallExecute());

        IEnumerator<YieldInstruction?> CallExecute()
        {
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

                    var minFps = ZNet.instance.IsDedicated() ? 10 : Game.m_minimumFPSLimit;
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

    bool Initialize()
    {
        foreach (var (configType, factory) in _configFactories)
        {
            var config = factory();
            _configs.Add(configType, config);
            config.RaiseInitialized();
        }
        _configFactories.Clear();

        //if (_mainConfig is not null)
        //    _mainConfig.ConfigFile.SettingChanged -= OnConfigChanged;
        //if (_worldConfig is not null)
        //    _worldConfig.ConfigFile.SettingChanged -= OnConfigChanged;
        //_worldConfig = null;
        _executeCounter = 0;

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
        if (RuntimeInformation.Instance.GameVersion != ExpectedGameVersion)
        {
            Logger.LogWarning(Invariant($"Unsupported game version: {RuntimeInformation.Instance.GameVersion}, expected: {ExpectedGameVersion}"));
            failed = true;
            abort |= !cfg.IgnoreGameVersionCheck.Value;
        }
        if (RuntimeInformation.Instance.NetworkVersion != ExpectedNetworkVersion)
        {
            Logger.LogWarning(Invariant($"Unsupported network version: {RuntimeInformation.Instance.NetworkVersion}, expected: {ExpectedNetworkVersion}"));
            failed = true;
            abort |= !cfg.IgnoreNetworkVersionCheck.Value;
        }
        if (RuntimeInformation.Instance.ItemDataVersion != ExpectedItemDataVersion)
        {
            Logger.LogWarning(Invariant($"Unsupported item data version: {RuntimeInformation.Instance.ItemDataVersion}, expected: {ExpectedItemDataVersion}"));
            failed = true;
            abort |= !cfg.IgnoreItemDataVersionCheck.Value;
        }
        if (RuntimeInformation.Instance.WorldVersion != ExpectedWorldVersion)
        {
            Logger.LogWarning(Invariant($"Unsupported world version: {RuntimeInformation.Instance.WorldVersion}, expected: {ExpectedWorldVersion}"));
            failed = true;
            abort |= !cfg.IgnoreWorldVersionCheck.Value;
        }

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

#if DEBUG
        Logger.LogInfo(Invariant($"Registered Processors: {Processors.Count}"));
#endif
        return true;
    }

    void OnConfigChanged(object sender, SettingChangedEventArgs e)
    {
        _configChanged.Add(sender.GetType().Assembly);
        if (Config.DiagnosticLogs.Value || ReferenceEquals(e.ChangedSetting, Config.DiagnosticLogs))
            Logger.LogInfo($"Config changed: [{e.ChangedSetting.Definition.Section}].[{e.ChangedSetting.Definition.Key}] = {e.ChangedSetting.BoxedValue}");
        if (ReferenceEquals(e.ChangedSetting, Config.DiagnosticLogs) && Config.DiagnosticLogs.Value)
            Logger.LogInfo(string.Join($"{Environment.NewLine}  ", ["Config:", .. Config.ConfigFile.Select(static x => Invariant($"[{x.Key.Section}].[{x.Key.Key}] = {x.Value.BoxedValue}"))]));
    }

    void Execute(PeersEnumerable peers, double timeBudgetSeconds)
    {
        var timeStartSeconds = Time.realtimeSinceStartupAsDouble;
        var executeUntil = timeStartSeconds + timeBudgetSeconds;
        _executeCounter++;
        if (_configChanged.Count is not 0)
        {
            //_configChanged = false;

            //if (Config.GlobalsKeys.SetGlobalKeysFromConfig.Value)
            //    ZoneSystem.instance.ResetWorldKeys();

            //if (Config.WorldModifiers.SetPresetFromConfig.Value)
            //{
            //    try { MyTerminal.ExecuteCommand("setworldpreset", Invariant($"{Config.WorldModifiers.Preset.Value}")); }
            //    catch (Exception ex) { Logger.LogError(ex); }
            //}

            //if (Config.WorldModifiers.SetModifiersFromConfig.Value)
            //{
            //    foreach (var (modifier, value) in Config.WorldModifiers.Modifiers.Select(static x => (x.Key, x.Value.Value)))
            //    {
            //        try { MyTerminal.ExecuteCommand("setworldmodifier", Invariant($"{modifier}"), Invariant($"{value}")); }
            //        catch (Exception ex) { Logger.LogError(ex); }
            //    }
            //}

            //if (Config.GlobalsKeys.SetGlobalKeysFromConfig.Value)
            //{
            //    /// <see cref="FejdStartup.ParseServerArguments"/>
            //    foreach (var (key, entry) in Config.GlobalsKeys.KeyConfigs.Where(static x => !Equals(x.Value.BoxedValue, x.Value.DefaultValue)))
            //    {
            //        if (entry.BoxedValue is bool boolValue)
            //        {
            //            if (boolValue)
            //                ZoneSystem.instance.SetGlobalKey(key);
            //            else
            //                ZoneSystem.instance.RemoveGlobalKey(key);
            //        }
            //        else
            //        {
            //            float value;
            //            try { value = (float)Convert.ChangeType(entry.BoxedValue, typeof(float)); }
            //            catch (Exception ex)
            //            {
            //                Logger.LogError(ex);
            //                continue;
            //            }
            //            ZoneSystem.instance.SetGlobalKey(key, value);
            //        }
            //    }
            //}

            var affectedProcessors = Processors.Where(x => _configChanged.Contains(x.GetType().Assembly)).ToList();
            _configChanged.Clear();

            foreach (var processor in affectedProcessors)
                    processor.Initialize(_executeCounter is 1);

            if (_executeCounter is 1)
            {
//#if DEBUG
//                GenerateDefaultConfigMarkdown(base.Config);
//                GenerateDocs();
//#endif
                foreach (var cfg in _configs.Values)
                {
                    cfg.ConfigChanged -= OnConfigChanged;
                    cfg.ConfigChanged += OnConfigChanged;
                }
            }
            else
            {
                foreach (var zdo in ZDOMan.instance.GetObjects())
                    zdo.Reregister(affectedProcessors);
            }
            return;
        }

        peers.Update();

        //SharedProcessorState.CleanUp(peers);

        if (peers.Count is 0)
            return;

        (_playerSectors, _playerSectorsOld) = (_playerSectorsOld, _playerSectors);
        var zonesAroundPlayers = ZoneSystem.instance.m_activeArea - 1;
        foreach (var peer in peers)
        {
            var playerSector = ZoneSystem.GetZone(peer.m_refPos);
            for (int x = playerSector.x - zonesAroundPlayers; x <= playerSector.x + zonesAroundPlayers; x++)
            {
                for (int y = playerSector.y - zonesAroundPlayers; y <= playerSector.y + zonesAroundPlayers; y++)
                {
                    var sector = new Vector2i(x, y);
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
        //var playerSectors = _playerSectors.AsEnumerable();
        //if (_unfinishedProcessingInRow > 10)
        //{
        //    // The idea here is to process zones in order of player proximity.
        //    // However, if all ZDOs are processed anyway, this ordering is a waste of time.
        //    foreach (var peer in peers)
        //    {
        //        var playerSector = ZoneSystem.GetZone(peer.m_refPos);
        //        foreach (var (sector, sectorInfo) in _playerSectors)
        //        {
        //            var dx = sector.x - playerSector.x;
        //            var dy = sector.y - playerSector.y;
        //            sectorInfo.InverseWeight += dx * dx + dy * dy;
        //        }
        //    }
        //    playerSectors = playerSectors.OrderBy(static x => x.Value.InverseWeight);
        //}

        foreach (var processor in Processors.AsEnumerable())
            processor.PreProcessInternal(peers);

        int processedSectors = 0;
        int processedZdos = 0;
        int totalZdos = 0;

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
                var zdo = sectorInfo.ZDOs[sectorInfo.ZdoIndex];
                if (!zdo.IsValid() || zdo.GetExtension<IZDOWithProcessors>() is not { HasNoProcessors: false } extZdo /*|| ReferenceEquals(zdo.PrefabInfo, PrefabInfo.Dummy)*/)
                    continue;

                var processors = extZdo.Processors;
                if (processors.Count > 1)
                {
                    Processor? claimedExclusiveBy = null;
                    foreach (var processor in processors.AsEnumerable())
                    {
                        if (!processor.ClaimExclusive(zdo))
                            continue;
                        if (claimedExclusiveBy is null)
                            claimedExclusiveBy = processor;
                        else if (Config.DiagnosticLogs.Value)
                            Logger.LogError(Invariant($"ZDO {zdo.m_uid} claimed exclusive by {processor.GetType().Name} while already claimed by {claimedExclusiveBy.GetType().Name}"));
                    }

                    if (claimedExclusiveBy is not null)
                    {
                        zdo.UnregisterAllExcept(claimedExclusiveBy);
                        processors = extZdo.Processors;
                    }
                }

                var destroy = false;
                var recreate = false;
                _unregister.Clear();
                foreach (var processor in processors.AsEnumerable())
                {
                    if (!zdo.CheckProcessorDataRevisionChanged(processor))
                        continue;
                    var result = processor.Process(sectorInfo.Peers, zdo);
                    if ((result & Processor.ProcessResult.WaitForZDORevisionChange) is not 0)
                        zdo.UpdateProcessorDataRevision(processor);
                    if ((result & Processor.ProcessResult.UnregisterProcessor) is not 0)
                        _unregister.Add(processor);
                    if (destroy = (result & Processor.ProcessResult.DestroyZDO) is not 0)
                    {
                        zdo.Destroy();
                        break;
                    }
                    recreate = recreate || (result & Processor.ProcessResult.RecreateZDO) is not 0;
                }
                if (!destroy && recreate)
                    zdo.Recreate();
                else if (!destroy && _unregister.Count > 0)
                    zdo.Ungregister(_unregister);
            }

            if (sectorInfo.ZdoIndex >= sectorInfo.ZDOs.Count)
            {
                sectorInfo.ZDOs.Clear();
                sectorInfo.ZdoIndex = 0;
            }
        }

        //foreach (var processor in Processors.AsEnumerable())
        //    processor.PostProcess();

        if (processedSectors < _playerSectors.Count || processedZdos < totalZdos)
            _unfinishedProcessingInRow++;
        else
            _unfinishedProcessingInRow = 0;

#if DEBUG
        var logLevel = _unfinishedProcessingInRow is 0 ? LogLevel.Debug : LogLevel.Info;
#else
        if (!Config.General.DiagnosticLogs.Value)
            return;
        var logLevel = _unfinishedProcessingInRow is 0 ? LogLevel.Debug : LogLevel.Info;
#endif

        var elapsedMs = (Time.realtimeSinceStartupAsDouble - timeStartSeconds) * 1000;
        Logger.Log(logLevel,
            Invariant($"{nameof(Execute)} took {elapsedMs:F2} ms (budget: {timeBudgetSeconds * 1000:F2} ms) to process {processedZdos} of {totalZdos} ZDOs in {processedSectors} of {_playerSectors.Count} zones. Incomplete runs in row: {_unfinishedProcessingInRow}"));

        if (logLevel is > LogLevel.Info or LogLevel.None)
            return;

        (_processingTimes ??= new(Processors.Count)).Clear();
        foreach (var processor in Processors.AsEnumerable())
        {
            var time = Math.Round(processor.ProcessingTimeSeconds * 1000, 2);
            if (time <= 0)
                continue;
            _processingTimes.Add((processor, time));
        }
        if (_processingTimes.Count is 0)
            return;
        _processingTimes.Sort(static (a, b) => Math.Sign(b.Item2 - a.Item2));
        Logger.Log(logLevel, Invariant($"Processing Time: {string.Join($", ", _processingTimes.Select(static x => Invariant($"{x.Item1.GetType().Name}: {x.Item2}ms")))}"));
    }
}
