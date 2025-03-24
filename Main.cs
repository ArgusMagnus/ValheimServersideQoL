using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed partial class Main : BaseUnityPlugin
{
    /// <Ideas>
    /// - Make tames lay eggs (by replacing spawned offspring with eggs and setting <see cref="EggGrow.m_grownPrefab"/>
    ///   Would probably not retain the value when picked up and dropped again. Could probably be solved by abusing some field in <see cref="EggGrow.m_item"/>
    /// - make ship pickup sunken items. <see cref="ZoneSystem.c_WaterLevel"/>
    /// - Allow carts through portals
    /// - Modify crafting station ranges <see cref="CraftingStation.m_rangeBuild"/>
    /// - Modify crafting station extension max distances <see cref="StationExtension.m_maxStationDistance"/>
    /// - Feed tames from containers
    /// - Prevent <see cref="Catapult"/> from accepting equipment as ammo. Test what <see cref="Catapult.m_onlyUseIncludedProjectiles"/> does
    /// - Increase wisp light radius <see cref="Demister"/> <see cref="SE_Demister"/> <see cref="MistEmitter"/> <see cref="Mister"/>
    /// - Log/kick players with illegal equipment. Automatic via <see cref="ZDOVars.s_crafterID"/> == 0 or via configurable list of forbidden items
    ///   <see cref="VisEquipment"/> <see cref="ZDOVars.s_rightItem"/>, etc.
    /// </Ideas>

    internal const string PluginName = "ServersideQoL";
    internal const string PluginGuid = $"argusmagnus.{PluginName}";
    internal static int PluginGuidHash { get; } = PluginGuid.GetStableHashCode();

    static Harmony HarmonyInstance { get; } = new Harmony(PluginGuid);
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

    readonly IReadOnlyList<Processor> _processors;

    public Main()
    {
        _cfg = new(Config);

        _processors = Processor.CreateInstances(_logger, _cfg);

        Config.SettingChanged += (_, _) => _resetPrefabInfo = true;
    }

    public void Awake()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        HarmonyInstance.PatchAll(assembly);
    }

    readonly GameVersion ExpectedGameVersion = GameVersion.ParseGameVersion("0.220.4");
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

        //_logger.LogInfo($"World Preset: {_cfg.GlobalsKeys.Preset.Value}");
        //_logger.LogInfo(string.Join($"{Environment.NewLine}    ", _cfg.GlobalsKeys.Modifiers.Select(x => $"{x.Key} = {x.Value.Value}").Prepend("World Modifiers:")));
        var keyConfigs = _cfg.GlobalsKeys.KeyConfigs;

        if (_cfg.General.DiagnosticLogs.Value)
            _logger.LogInfo(string.Join($"{Environment.NewLine}    ", keyConfigs.Select(x => $"{x.Key} = {x.Value.BoxedValue}").Prepend("Global Keys:")));

#if DEBUG
        _logger.LogInfo($"Registered Processors: {_processors.Count}");
#endif

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

    sealed class MyTerminal : Terminal
    {
        protected override Terminal m_terminalInstance => throw new NotImplementedException();

        //public static IReadOnlyDictionary<string, ConsoleCommand> Commands => commands;

        public static void ExecuteCommand(string command, params string[] args)
        {
            var cmd = commands[command];
            // var action = typeof(ConsoleCommand).GetField("action", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cmd)
            //     ?? typeof(ConsoleCommand).GetField("actionFailable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cmd);
            if (PrivateAccessor.GetCommandAction(cmd) is { } consoleEvent)
                consoleEvent(new MyConsoleEventArgs(command, args));
            else if (PrivateAccessor.GetCommandActionFailable(cmd) is { } consoleEventFailable)
            {
                var result = consoleEventFailable(new MyConsoleEventArgs(command, args));
                if (result is not bool b || !b)
                    throw new Exception(result.ToString());
            }
            else
                throw new ArgumentException(nameof(command));
        }

        sealed class MyConsoleEventArgs : ConsoleEventArgs
        {
            public MyConsoleEventArgs(string command, params string[] args)
                : base("", null)
                => Args = [command, .. args];
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

            if (!string.IsNullOrEmpty(_cfg.GlobalsKeys.Preset.Value))
            {
                try { MyTerminal.ExecuteCommand("setworldpreset", _cfg.GlobalsKeys.Preset.Value); }
                catch(Exception ex) { _logger.LogError(ex); }
            }

            foreach (var (modifier, value) in _cfg.GlobalsKeys.Modifiers.Select(x => (x.Key, x.Value.Value)).Where(x => !string.IsNullOrEmpty(x.Value)))
            {
                try { MyTerminal.ExecuteCommand("setworldmodifier", modifier, value); }
                catch (Exception ex) { _logger.LogError(ex); }
            }

            /// <see cref="FejdStartup.ParseServerArguments"/>
            /// This would not work correctly IF config was actually reloaded, as reset config values would not reset the global key
            foreach (var (key, entry) in _cfg.GlobalsKeys.KeyConfigs.Where(x => !Equals(x.Value.DefaultValue, x.Value.BoxedValue)).Select(x => (x.Key, x.Value)))
            {
                if (entry.BoxedValue is bool boolValue)
                {
                    if (boolValue)
                        ZoneSystem.instance.SetGlobalKey(key);
                    else
                        ZoneSystem.instance.RemoveGlobalKey(key);
                }
                else
                {
                    float value;
                    try { value = (float)Convert.ChangeType(entry.BoxedValue, typeof(float)); }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex);
                        continue;
                    }
                    ZoneSystem.instance.SetGlobalKey(key, value);
                }
            }

            SharedProcessorState.Initialize(_cfg);
            foreach (var processor in _processors)
                processor.Initialize();

#if DEBUG
            GenerateDefaultConfigMarkdown(Config);
#endif

            return;
        }

        if (ZNet.instance.GetPeers() is not { Count: > 0 } peers)
            return;

        _watch.Restart();

        // roughly once per minute
        if (_executeCounter % (ulong)(60 * _cfg.General.Frequency.Value) is 0)
        {
            //foreach (var (key, dict) in SharedProcessorState.ContainersByItemName.Select(x => (x.Key, x.Value)))
            //{
            //    foreach (var id in dict.Keys)
            //    {
            //        if (ZDOMan.instance.GetZDO(id) is not { } zdo || !zdo.IsValid())
            //            dict.TryRemove(id, out _);
            //    }
            //    if (dict is { Count: 0 })
            //        SharedProcessorState.ContainersByItemName.TryRemove(key, out _);
            //}

            foreach (var (key, set) in SharedProcessorState.FollowingTamesByPlayerName.Select(x => (x.Key, x.Value)))
            {
                foreach (var id in set)
                {
                    if (ZDOMan.instance.GetZDO(id) is not { } zdo || !zdo.IsValid())
                        set.Remove(id);
                }
                if (set is { Count: 0 })
                    SharedProcessorState.FollowingTamesByPlayerName.TryRemove(key, out _);
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
                var zdo = (ExtendedZDO)sectorInfo.ZDOs[sectorInfo.ZDOs.Count - 1];
                sectorInfo.ZDOs.RemoveAt(sectorInfo.ZDOs.Count - 1);
                if (!zdo.IsValid() || ReferenceEquals(zdo.PrefabInfo, PrefabInfo.Dummy))
                    continue;

                var destroy = false;
                var recreate = false;
                foreach (var processor in _processors)
                {
                    processor.Process(zdo, sectorInfo.Peers, ref destroy, ref recreate);
                    if (destroy)
                    {
                        zdo.Destroy();
                        break;
                    }
                    if (recreate)
                        zdo = zdo.Recreate();
                }
            }
        }

        if (processedSectors < _playerSectors.Count || processedZdos < totalZdos)
            _unfinishedProcessingInRow++;
        else
            _unfinishedProcessingInRow = 0;

        _watch.Stop();

        if (!_cfg.General.DiagnosticLogs.Value)
            return;

        var logLevel = _watch.ElapsedMilliseconds > _cfg.General.MaxProcessingTime.Value ? LogLevel.Info : LogLevel.Debug;
        _logger.Log(logLevel,
            $"{nameof(Execute)} took {_watch.ElapsedMilliseconds} ms to process {processedZdos} of {totalZdos} ZDOs in {processedSectors} of {_playerSectors.Count} zones. Uncomplete runs in row: {_unfinishedProcessingInRow}");

        _logger.LogDebug(string.Join($"{Environment.NewLine}  ", _processors.Select(x => $"{x.GetType().Name}: {x.ProcessingTime.TotalMilliseconds}ms").Prepend("ProcessingTime:")));
        _logger.LogDebug(string.Join($"{Environment.NewLine}  ", _processors.Select(x => $"{x.GetType().Name}: {x.TotalProcessingTime}").Prepend("TotalProcessingTime:")));
    }

#if DEBUG
    static void GenerateDefaultConfigMarkdown(ConfigFile cfg)
    {
        using var writer = new StreamWriter(ConfigMarkdownPath, false, new UTF8Encoding(false));
        writer.WriteLine("|Category|Key|Default Value|Acceptable Values|Description|");
        writer.WriteLine("|---|---|---|---|---|");

        foreach (var (def, entry) in cfg.OrderBy(x => x.Key.Section).Select(x => (x.Key, x.Value)))
        {
            var section = Regex.Replace(def.Section, @"^[A-Z] - ", "");

            var accetableValues = entry.Description.AcceptableValues?.ToDescriptionString();
            if (accetableValues is not null)
                accetableValues = Regex.Replace(accetableValues, @"^#.+?\:\s*", "");
            else if (entry.SettingType == typeof(bool))
                accetableValues = $"{bool.TrueString}/{bool.FalseString}";
            else if (entry.SettingType.IsEnum)
            {
                if (entry.SettingType.GetCustomAttribute<FlagsAttribute>() is null)
                    accetableValues = $"One of {string.Join(", ", Enum.GetNames(entry.SettingType))}";
                else
                    accetableValues = $"Combination of {string.Join(", ", Enum.GetNames(entry.SettingType))}";
            }

            writer.WriteLine($"|{section}|{def.Key}|{entry.DefaultValue}|{accetableValues}|{entry.Description.Description}|");
        }
    }
#endif
}
