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
    /// - Prevent <see cref="Catapult"/> from accepting equipment as ammo. Test what <see cref="Catapult.m_onlyUseIncludedProjectiles"/> does
    /// - Add status effects to players <see cref="SEMan.RPC_AddStatusEffect"/>, read status effects <see cref="ZDOVars.s_seAttrib"/> <see cref="SEMan.HaveStatusAttribute"/> <see cref="StatusEffect.StatusAttribute"/>
    /// - <see cref="Pathfinding"/> <see cref="SapCollector"/> <see cref="ResourceRoot"/>
    /// - <see cref="ShieldGenerator"/> <see cref="Trap"/> <see cref="WearNTear"/> <see cref="DamageText"/>
    /// - Summons: (<see cref="Tameable"/>
    ///   - Modify max instances <see cref="ZDOVars.s_maxInstances"/>
    ///   - Disable unsummon based on distance/logout <see cref="Tameable.m_unsummonDistance"/> <see cref="Tameable.m_unsummonOnOwnerLogoutSeconds"/>
    /// - Prevent traps from damaging themselves or friendlies <see cref="Aoe.m_damageSelf"/> <see cref="Aoe.m_hitFriendly"/>
    /// </Ideas>

    internal const string PluginName = "ServersideQoL";
    internal const string PluginGuid = $"argusmagnus.{PluginName}";
    internal static int PluginGuidHash { get; } = PluginGuid.GetStableHashCode();

    static Harmony HarmonyInstance { get; } = new Harmony(PluginGuid);
    internal static new ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource(PluginName);
    internal static new ModConfig Config { get; private set; } = default!;

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

    readonly List<Processor> _unregister = new();

    public Main()
    {
        Config ??= new(base.Config);

        base.Config.SettingChanged += (_, _) => _resetPrefabInfo = true;
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
    const string DummyConfigSection = "Dummy";

    public void Start()
    {
        if (!Config.General.Enabled.Value)
            return;

        var failed = false;
        var abort = false;
        if (RuntimeInformation.Instance.GameVersion != ExpectedGameVersion)
        {
            Logger.LogWarning($"Unsupported game version: {RuntimeInformation.Instance.GameVersion}, expected: {ExpectedGameVersion}");
            failed = true;
            abort |= !Config.General.IgnoreGameVersionCheck.Value;
        }
        if (RuntimeInformation.Instance.NetworkVersion != ExpectedNetworkVersion)
        {
            Logger.LogWarning($"Unsupported network version: {RuntimeInformation.Instance.NetworkVersion}, expected: {ExpectedNetworkVersion}");
            failed = true;
            abort |= !Config.General.IgnoreNetworkVersionCheck.Value;
        }
        if (RuntimeInformation.Instance.ItemDataVersion != ExpectedItemDataVersion)
        {
            Logger.LogWarning($"Unsupported item data version: {RuntimeInformation.Instance.ItemDataVersion}, expected: {ExpectedItemDataVersion}");
            failed = true;
            abort |= !Config.General.IgnoreItemDataVersionCheck.Value;
        }
        if (RuntimeInformation.Instance.WorldVersion != ExpectedWorldVersion)
        {
            Logger.LogWarning($"Unsupported world version: {RuntimeInformation.Instance.WorldVersion}, expected: {ExpectedWorldVersion}");
            failed = true;
            abort |= !Config.General.IgnoreWorldVersionCheck.Value;
        }

        if (failed)
        {
            if (!abort)
                Logger.LogError("Version checks failed, but you chose to ignore the checks (config). Continuing...");
            else
            {
                Logger.LogError("Version checks failed. Mod execution is stopped");
                return;
            }
        }

        //_logger.LogInfo($"World Preset: {Config.GlobalsKeys.Preset.Value}");
        //_logger.LogInfo(string.Join($"{Environment.NewLine}    ", Config.GlobalsKeys.Modifiers.Select(x => $"{x.Key} = {x.Value.Value}").Prepend("World Modifiers:")));
        var keyConfigs = Config.GlobalsKeys.KeyConfigs;

        if (Config.General.DiagnosticLogs.Value)
            Logger.LogInfo(string.Join($"{Environment.NewLine}    ", keyConfigs.Select(x => $"{x.Key} = {x.Value.BoxedValue}").Prepend("Global Keys:")));

#if DEBUG
        Logger.LogInfo($"Registered Processors: {Processor.DefaultProcessors.Count}");
#endif

        StartCoroutine(CallExecute());

        IEnumerator<YieldInstruction> CallExecute()
        {
            yield return new WaitForSeconds(Config.General.StartDelay.Value);
            while (true)
            {
                try { Execute(); }
                catch (OperationCanceledException) { yield break; }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    yield break;
                }
                yield return new WaitForSeconds(1f / Config.General.Frequency.Value);
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
            Logger.LogWarning("Mod should only be installed on the host");
            throw new OperationCanceledException();
        }

        if (ZNetScene.instance is null || ZDOMan.instance is null)
            return;

        if (_executeCounter++ is 0 || _resetPrefabInfo)
        {
            _resetPrefabInfo = false;

            if (!string.IsNullOrEmpty(Config.GlobalsKeys.Preset.Value))
            {
                try { MyTerminal.ExecuteCommand("setworldpreset", Config.GlobalsKeys.Preset.Value); }
                catch(Exception ex) { Logger.LogError(ex); }
            }

            foreach (var (modifier, value) in Config.GlobalsKeys.Modifiers.Select(x => (x.Key, x.Value.Value)).Where(x => !string.IsNullOrEmpty(x.Value)))
            {
                try { MyTerminal.ExecuteCommand("setworldmodifier", modifier, value); }
                catch (Exception ex) { Logger.LogError(ex); }
            }

            /// <see cref="FejdStartup.ParseServerArguments"/>
            /// This would not work correctly IF config was actually reloaded, as reset config values would not reset the global key
            foreach (var (key, entry) in Config.GlobalsKeys.KeyConfigs.Where(x => !Equals(x.Value.DefaultValue, x.Value.BoxedValue)).Select(x => (x.Key, x.Value)))
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
                        Logger.LogError(ex);
                        continue;
                    }
                    ZoneSystem.instance.SetGlobalKey(key, value);
                }
            }

            SharedProcessorState.Initialize(Config);
            foreach (var processor in Processor.DefaultProcessors)
                processor.Initialize();

#if DEBUG
            GenerateDefaultConfigMarkdown(base.Config);
            GeneratePrefabSheet();
#endif

            base.Config.Bind(DummyConfigSection, "Dummy", "", $"Dummy entry which does nothing, it's abused to include runtime information in the config file:{Environment.NewLine}{RuntimeInformation.Instance}");
            return;
        }

        if (ZNet.instance.GetPeers() is not { Count: > 0 } peers)
            return;

        _watch.Restart();

        // roughly once per minute
        if (_executeCounter % (ulong)(60 * Config.General.Frequency.Value) is 0)
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
            for (int x = playerSector.x - Config.General.ZonesAroundPlayers.Value; x <= playerSector.x + Config.General.ZonesAroundPlayers.Value; x++)
            {
                for (int y = playerSector.y - Config.General.ZonesAroundPlayers.Value; y <= playerSector.y + Config.General.ZonesAroundPlayers.Value; y++)
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

        foreach (var processor in Processor.DefaultProcessors)
            processor.PreProcess();

        foreach (var (sector, sectorInfo) in playerSectors.Select(x => (x.Key, x.Value)))
        {
            if (_watch.ElapsedMilliseconds > Config.General.MaxProcessingTime.Value)
                break;

            processedSectors++;

            if (sectorInfo is { ZDOs: { Count: 0 } })
                ZDOMan.instance.FindSectorObjects(sector, 1, 0, sectorInfo.ZDOs);

            totalZdos += sectorInfo.ZDOs.Count;

            while (sectorInfo is { ZDOs: { Count: > 0 } } && _watch.ElapsedMilliseconds < Config.General.MaxProcessingTime.Value)
            {
                processedZdos++;
                var zdo = (ExtendedZDO)sectorInfo.ZDOs[sectorInfo.ZDOs.Count - 1];
                sectorInfo.ZDOs.RemoveAt(sectorInfo.ZDOs.Count - 1);
                if (!zdo.IsValid() || ReferenceEquals(zdo.PrefabInfo, PrefabInfo.Dummy))
                    continue;

                var destroy = false;
                var recreate = false;
                _unregister.Clear();
                foreach (var processor in zdo.Processors)
                {
                    processor.Process(zdo, sectorInfo.Peers);
                    if (processor.UnregisterZdoProcessor)
                        _unregister.Add(processor);
                    if (destroy = processor.DestroyZdo)
                    {
                        zdo.Destroy();
                        break;
                    }
                    recreate = recreate || processor.RecreateZdo;
                }
                if (!destroy && recreate)
                    zdo.Recreate();
                else if (_unregister.Count > 0)
                    zdo.Unregister(_unregister);
            }
        }

        if (processedSectors < _playerSectors.Count || processedZdos < totalZdos)
            _unfinishedProcessingInRow++;
        else
            _unfinishedProcessingInRow = 0;

        _watch.Stop();

#if !DEBUG
        if (!Config.General.DiagnosticLogs.Value)
            return;
#endif

        var logLevel = _watch.ElapsedMilliseconds > Config.General.MaxProcessingTime.Value ? LogLevel.Info : LogLevel.Debug;
        Logger.Log(logLevel,
            $"{nameof(Execute)} took {_watch.ElapsedMilliseconds} ms to process {processedZdos} of {totalZdos} ZDOs in {processedSectors} of {_playerSectors.Count} zones. Uncomplete runs in row: {_unfinishedProcessingInRow}");

        Logger.Log(logLevel, $"Processing Time: {string.Join($", ", Processor.DefaultProcessors.Where(x => x.ProcessingTime.Ticks > 0).OrderByDescending(x => x.ProcessingTime.Ticks).Select(x => $"{x.GetType().Name}: {x.ProcessingTime.TotalMilliseconds}ms"))}");
        //Logger.LogDebug(string.Join($"{Environment.NewLine}  ", Processor.DefaultProcessors.Select(x => $"{x.GetType().Name}: {x.TotalProcessingTime}").Prepend("TotalProcessingTime:")));
    }

#if DEBUG
    static void GenerateDefaultConfigMarkdown(ConfigFile cfg)
    {
        using var writer = new StreamWriter(ConfigMarkdownPath, false, new UTF8Encoding(false));
        writer.WriteLine("|Category|Key|Default Value|Acceptable Values|Description|");
        writer.WriteLine("|---|---|---|---|---|");

        foreach (var (def, entry) in cfg.OrderBy(x => x.Key.Section).Select(x => (x.Key, x.Value)))
        {
            if (def.Section == DummyConfigSection)
                continue;

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

    static void GeneratePrefabSheet()
    {
        var path = Path.Combine(Path.GetDirectoryName(ConfigMarkdownPath), "Docs", "Prefabs");
        Directory.CreateDirectory(path);

        var prefabs = new ConcurrentBag<(string Prefab, string Components)>();

        Parallel.ForEach(ZNetScene.instance.m_prefabs, prefab =>
        {
            if (prefab.GetComponent<ZNetView>()?.gameObject.GetComponentsInChildren<MonoBehaviour>() is not { Length: > 0 } components)
                return;

            prefabs.Add((prefab.name, string.Join(", ", components.Select(x => x.GetType().Name).Distinct().OrderBy(x => x))));

            using var writer = new StreamWriter(Path.Combine(path, $"{prefab.name.Replace(" ", "")}.md"), false, new UTF8Encoding(false));

            writer.WriteLine($"## {prefab.name}");
            writer.WriteLine();

            foreach (var component in components)
            {
                if (component is ZNetView)
                    continue;

                writer.WriteLine($"### Component: {component.GetType().Name} ({component.name})");
                writer.WriteLine();

                /// <see cref="ZNetView.LoadFields"/>
                if (component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance) is not { Length: > 0 } fields)
                    continue;

                writer.WriteLine("|Field|Type|Default Value|");
                writer.WriteLine("|-----|----|-------------|");

                foreach (var field in fields)
                {
                    var value = field.GetValue(component);
                    switch (value)
                    {
                        case int:
                        case float:
                        case bool:
                        case Vector3:
                        case string:
                            break;
                        case GameObject:
                        case ItemDrop:
                            value = ((UnityEngine.Object)value).name;
                            break;
                        default:
                            continue;
                    }
                    writer.WriteLine($"|{field.Name}|{field.FieldType}|{value}|");
                }

                writer.WriteLine();
            }
        });

        {
            using var writer = new StreamWriter($"{path}.md", false, new UTF8Encoding(false));
            writer.WriteLine("## Prefabs");
            writer.WriteLine();
            writer.WriteLine("|Prefab|Components|");
            writer.WriteLine("|------|----------|");
            foreach (var (prefab, components) in prefabs.OrderBy(x => x.Prefab))
                writer.WriteLine($"|[{prefab}](Prefabs/{prefab.Replace(" ", "")}.md)|{components}|");
        }        
    }
#endif
}
