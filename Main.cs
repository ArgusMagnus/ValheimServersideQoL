using BepInEx;
using BepInEx.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using UnityEngine;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed partial class Main : BaseUnityPlugin
{
    /// <Ideas>
    /// - Make tames lay eggs (by replacing spawned offspring with eggs and setting <see cref="EggGrow.m_grownPrefab"/>
    ///   Would probably not retain the value when picked up and dropped again. Could probably be solved by abusing same field in <see cref="EggGrow.m_item"/>
    /// - make ship pickup sunken items. How? Ships are not containers...
    /// - make serpent drops float
    /// - Allow carts through portals
    /// - Modify container inventory sizes
    /// - Make carts ignore weights <see cref="Vagon"/>
    /// - Expose <see cref="GlobalKeys"/> settings via config. Find a way to automatically detect type and valid range of global keys. <see cref="ZoneSystem.UpdateWorldRates"/>
    /// </summary>

    internal const string PluginName = "ServersideQoL";
    internal const string PluginGuid = $"argusmagnus.{PluginName}";
    internal static int PluginGuidHash { get; } = PluginGuid.GetStableHashCode();

    //static Harmony HarmonyInstance { get; } = new Harmony(pluginGUID);
    readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource(PluginName);

    readonly ModConfig _cfg;
    readonly Stopwatch _watch = new();

    ulong _executeCounter;
    uint _unfinishedProcessingInRow;
    bool _resetPrefabInfo;
    record SectorInfo(List<ZNetPeer> Peers, List<ZDO> ZDOs)
    {
        public int InverseWeight { get; set; }
    }
    ConcurrentDictionary<Vector2i, SectorInfo> _playerSectors = new();
    ConcurrentDictionary<Vector2i, SectorInfo> _playerSectorsOld = new();

    readonly SharedProcessorState _sharedProcessorState = new();
    readonly IReadOnlyList<Processor> _processors;

    public Main()
    {
        _cfg = new(Config);

        _processors = Processor.CreateInstances(_logger, _cfg, _sharedProcessorState);

        Config.SettingChanged += (_, _) => _resetPrefabInfo = true;
    }

    //public void Awake()
    //{
    //Logger.LogInfo("Thank you for using my mod!");

    //Assembly assembly = Assembly.GetExecutingAssembly();
    //HarmonyInstance.PatchAll(assembly);

    //ItemManager.OnItemsRegistered += OnItemsRegistered;
    //PrefabManager.OnPrefabsRegistered += OnPrefabsRegistered;
    //}

    readonly GameVersion ExpectedGameVersion = GameVersion.ParseGameVersion("0.220.3");
    const uint ExpectedNetworkVersion = 33;
    const uint ExpectedItemDataVersion = 106;
    const uint ExpectedWorldVersion = 35;

    public void Start()
    {
        if (!_cfg.General.Enabled.Value)
            return;

        var failed = false;
        var abort = false;
        var versionType = typeof(Game).Assembly.GetType("Version", true);
        if (versionType.GetProperty("CurrentVersion")?.GetValue(null) is not GameVersion gameVersion)
            gameVersion = default;
        if (gameVersion != ExpectedGameVersion)
        {
            _logger.LogWarning($"Unsupported game version: {gameVersion}, expected: {ExpectedGameVersion}");
            failed = true;
            abort |= !_cfg.General.IgnoreGameVersionCheck.Value;
        }
        if (versionType.GetField("m_networkVersion")?.GetValue(null) is not uint networkVersion)
            networkVersion = default;
        if (networkVersion != ExpectedNetworkVersion)
        {
            _logger.LogWarning($"Unsupported network version: {networkVersion}, expected: {ExpectedNetworkVersion}");
            failed = true;
            abort |= !_cfg.General.IgnoreNetworkVersionCheck.Value;
        }
        if (versionType.GetField("m_itemDataVersion")?.GetValue(null) is not int itemDataVersion)
            itemDataVersion = default;
        if (itemDataVersion != ExpectedItemDataVersion)
        {
            _logger.LogWarning($"Unsupported item data version: {itemDataVersion}, expected: {ExpectedItemDataVersion}");
            failed = true;
            abort |= !_cfg.General.IgnoreItemDataVersionCheck.Value;
        }
        if (versionType.GetField("m_worldVersion")?.GetValue(null) is not int worldVersion)
            worldVersion = default;
        if (worldVersion != ExpectedWorldVersion)
        {
            _logger.LogWarning($"Unsupported world version: {worldVersion}, expected: {ExpectedWorldVersion}");
            failed = true;
            abort |= !_cfg.General.IgnoreWorldVersionCheck.Value;
        }

        if (failed)
        {
            if (!abort)
                _logger.LogError("Version checks failed, but you chose to ignore the checks (config). Continuing...");
            else
            {
                _logger.LogError("Version checks failed. Mod execution is stopped");
                return;
            }
        }

        _logger.LogInfo($"Registered Processors: {_processors.Count}");

        StartCoroutine(CallExecute());

        IEnumerator<YieldInstruction> CallExecute()
        {
            yield return new WaitForSeconds(_cfg.General.StartDelay.Value);
            while (true)
            {
                try { Execute(); }
                catch (OperationCanceledException) { yield break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                    yield break;
                }
                yield return new WaitForSeconds(1f / _cfg.General.Frequency.Value);
            }
        }
    }

    void Execute()
    {
        if (ZNet.instance is null)
            return;

        if (ZNet.instance.IsServer() is false)
        {
            _logger.LogWarning("Mod should only be installed on the host");
            throw new OperationCanceledException();
        }

        if (ZNetScene.instance is null || ZDOMan.instance is null)
            return;

        if (_executeCounter++ is 0 || _resetPrefabInfo)
        {
            _resetPrefabInfo = false;
            _sharedProcessorState.Initialize(_cfg);
            foreach (var processor in _processors)
                processor.Initialize();
            return;
        }

        if (ZNet.instance.GetPeers() is not { Count: > 0 } peers)
            return;

        _watch.Restart();

        // roughly once per minute
        if (_executeCounter % (ulong)(60 * _cfg.General.Frequency.Value) is 0)
        {
            foreach (var id in _sharedProcessorState.DataRevisions.Keys)
            {
                if (ZDOMan.instance.GetZDO(id) is not { } zdo || !zdo.IsValid())
                    _sharedProcessorState.DataRevisions.TryRemove(id, out _);
            }

            foreach (var (key, dict) in _sharedProcessorState.ContainersByItemName.Select(x => (x.Key, x.Value)))
            {
                foreach (var id in dict.Keys)
                {
                    if (ZDOMan.instance.GetZDO(id) is not { } zdo || !zdo.IsValid())
                        dict.TryRemove(id, out _);
                }
                if (dict is { Count: 0 })
                    _sharedProcessorState.ContainersByItemName.TryRemove(key, out _);
            }

            foreach (var (key, set) in _sharedProcessorState.FollowingTamesByPlayerName.Select(x => (x.Key, x.Value)))
            {
                foreach (var id in set)
                {
                    if (ZDOMan.instance.GetZDO(id) is not { } zdo || !zdo.IsValid())
                        set.Remove(id);
                }
                if (set is { Count: 0 })
                    _sharedProcessorState.FollowingTamesByPlayerName.TryRemove(key, out _);
            }
        }

        (_playerSectors, _playerSectorsOld) = (_playerSectorsOld, _playerSectors);
        _playerSectors.Clear();
        const int SortPlayerSectorsThreshold = 10;
        foreach (var peer in peers)
        {
            var playerSector = ZoneSystem.GetZone(peer.m_refPos);
            for (int x = playerSector.x - _cfg.General.ZonesAroundPlayers.Value; x <= playerSector.x + _cfg.General.ZonesAroundPlayers.Value; x++)
            {
                for (int y = playerSector.y - _cfg.General.ZonesAroundPlayers.Value; y <= playerSector.y + _cfg.General.ZonesAroundPlayers.Value; y++)
                {
                    var sector = new Vector2i(x, y);
                    if (_playerSectorsOld.TryRemove(sector, out var sectorInfo))
                    {
                        _playerSectors.TryAdd(sector, sectorInfo);
                        sectorInfo.InverseWeight = 0;
                        sectorInfo.Peers.Clear();
                        sectorInfo.Peers.Add(peer);
                    }
                    else if (_playerSectors.TryGetValue(sector, out sectorInfo))
                    {
                        sectorInfo.InverseWeight = 0;
                        sectorInfo.Peers.Add(peer);
                    }
                    else
                    {
                        sectorInfo = new([peer], []);
                        _playerSectors.TryAdd(sector, sectorInfo);
                    }
                }
            }
        }

        if (_unfinishedProcessingInRow > SortPlayerSectorsThreshold)
        {
            // The idea here is to process zones in order of player proximity.
            // However, if all ZDOs are processed anyway, this ordering is a waste of time.
            foreach (var peer in peers)
            {
                var playerSector = ZoneSystem.GetZone(peer.m_refPos);
                foreach (var (sector, sectorInfo) in _playerSectors.Select(x => (x.Key, x.Value)))
                {
                    var dx = sector.x - playerSector.x;
                    var dy = sector.y - playerSector.y;
                    sectorInfo.InverseWeight += dx * dx + dy * dy;
                }
            }
        }

        int processedSectors = 0;
        int processedZdos = 0;
        int totalZdos = 0;

        var playerSectors = _playerSectors.AsEnumerable();
        if (_unfinishedProcessingInRow > SortPlayerSectorsThreshold)
            playerSectors = playerSectors.OrderBy(x => x.Value.InverseWeight);

        foreach (var processor in _processors)
            processor.PreProcess();

        foreach (var (sector, sectorInfo) in playerSectors.Select(x => (x.Key, x.Value)))
        {
            if (_watch.ElapsedMilliseconds > _cfg.General.MaxProcessingTime.Value)
                break;

            processedSectors++;

            if (sectorInfo is { ZDOs: { Count: 0 } })
                ZDOMan.instance.FindSectorObjects(sector, 1, 0, sectorInfo.ZDOs);

            totalZdos += sectorInfo.ZDOs.Count;

            while (sectorInfo is { ZDOs: { Count: > 0 } } && _watch.ElapsedMilliseconds < _cfg.General.MaxProcessingTime.Value)
            {
                processedZdos++;
                var zdo = sectorInfo.ZDOs[sectorInfo.ZDOs.Count - 1];
                sectorInfo.ZDOs.RemoveAt(sectorInfo.ZDOs.Count - 1);
                if (!zdo.IsValid() || !_sharedProcessorState.PrefabInfo.TryGetValue(zdo.GetPrefab(), out var prefabInfo))
                    continue;

                foreach (var processor in _processors)
                    processor.Process(ref zdo, prefabInfo, sectorInfo.Peers);
            }
        }

        if (processedSectors < _playerSectors.Count || processedZdos < totalZdos)
            _unfinishedProcessingInRow++;
        else
            _unfinishedProcessingInRow = 0;

        _watch.Stop();
        var logLevel = _watch.ElapsedMilliseconds > _cfg.General.MaxProcessingTime.Value ? LogLevel.Info : LogLevel.Debug;
        _logger.Log(logLevel,
            $"{nameof(Execute)} took {_watch.ElapsedMilliseconds} ms to process {processedZdos} of {totalZdos} ZDOs in {processedSectors} of {_playerSectors.Count} zones. Uncomplete runs in row: {_unfinishedProcessingInRow}");
        _logger.Log(logLevel, string.Join($"{Environment.NewLine}  ", _processors.Select(x => $"{x.GetType().Name}: {x.ProcessingTime.TotalMilliseconds}ms").Prepend("ProcessingTime:")));
        _logger.Log(logLevel, string.Join($"{Environment.NewLine}  ", _processors.Select(x => $"{x.GetType().Name}: {x.TotalProcessingTime}").Prepend("TotalProcessingTime:")));
    }

    internal static void ShowMessage(IEnumerable<ZNetPeer> peers, MessageHud.MessageType type, string message)
    {
        /// Invoke <see cref="MessageHud.RPC_ShowMessage"/>
        foreach (var peer in peers)
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "ShowMessage", (int)type, message);
    }
}
