using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
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
    /// - Make sharp stakes drop their resources when destroyed <see cref="Piece.DropResources(HitData)"/> <see cref="WearNTear.Remove(bool)"/>
    ///   Not easily possible: Responsible code in <see cref="Piece.DropResources(HitData)"/> uses <see cref="Piece.m_resources"/> / <see cref="Piece.Requirement.m_recover"/>
    ///   which cannot be modified via ZDO fields. We would have to somehow detect when a stakewall is destroyed and spawn the resources ourselves.
    /// - <see cref="Chat"/> <see cref="Humanoid"/> <see cref="Character"/> <see cref="InventoryGui.SortMethod"/> <see cref="Player"/>
    /// - <see cref="SpawnArea"/>
    /// - Stack player inventory into chests <see cref="Container.RPC_RequestStack"/>
    /// </Ideas>

    internal const string PluginName = "ServersideQoL";
    internal const string PluginGuid = $"argusmagnus.{PluginName}";
    internal static int PluginGuidHash { get; } = PluginGuid.GetStableHashCode();

    internal static Main Instance { get; private set; } = default!;

    internal static Harmony HarmonyInstance { get; } = new Harmony(PluginGuid);
    internal new ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource(PluginName);
    ModConfig? _mainConfig;
    ModConfig? _worldConfig;
    internal new ModConfig Config => _worldConfig ?? (_mainConfig ??= new(base.Config));

    readonly Stopwatch _watch = new();

    ulong _executeCounter;
    uint _unfinishedProcessingInRow;
    record SectorInfo(List<Peer> Peers, List<ZDO> ZDOs)
    {
        public int InverseWeight { get; set; }
    }
    readonly Stack<SectorInfo> _sectorInfoPool = [];
    Dictionary<Vector2i, SectorInfo> _playerSectors = [];
    Dictionary<Vector2i, SectorInfo> _playerSectorsOld = [];

    readonly List<Processor> _unregister = [];
    bool _configChanged = true;

    public Main()
    {
        Instance = this;
    }

    void Awake()
    {
//#if DEBUG
//        foreach (var type in typeof(Game).Assembly.ExportedTypes.Where(x => !x.IsEnum))
//        {
//            foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
//                .Where(x => x.IsLiteral && !x.IsInitOnly))
//            {
//                var value = field.GetRawConstantValue();
//                if (Equals(value, 3000f))
//                    Logger.LogError($"{type.Name}.{field.Name}: {value} ({field.FieldType.Name})");
//                else
//                    Logger.LogWarning($"{type.Name}.{field.Name}: {value} ({field.FieldType.Name})");
//            }
//        }
//#endif

        if (PluginVersion != PluginInformationalVersion)
            Logger.LogWarning($"You are running a pre-release version: {PluginInformationalVersion}");
        HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
    }

    readonly GameVersion ExpectedGameVersion = GameVersion.ParseGameVersion("0.220.5");
    const uint ExpectedNetworkVersion = 34;
    const uint ExpectedItemDataVersion = 106;
    const uint ExpectedWorldVersion = 35;
    internal const string DummyConfigSection = "Z - Dummy";

    void Start()
    {
        StartCoroutine(CallExecute());

        IEnumerator<YieldInstruction> CallExecute()
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
                    yield return new WaitForSeconds(1f / Config.General.Frequency.Value);

                    if (ZNet.instance is null)
                        break;

                    try { Execute(peers); }
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
        if (_mainConfig is not null)
            _mainConfig.ConfigFile.SettingChanged -= OnConfigChanged;
        if (_worldConfig is not null)
            _worldConfig.ConfigFile.SettingChanged -= OnConfigChanged;
        _worldConfig = null;
        _executeCounter = 0;

        if (Config.General.ConfigPerWorld.Value)
        {
            var path = ZNet.World.GetRootPath(FileHelpers.FileSource.Local);
            path = $"{path}.{PluginName}.cfg";
            if (!File.Exists(path) && File.Exists(base.Config.ConfigFilePath))
                File.Copy(base.Config.ConfigFilePath, path);
            Logger.LogInfo("Using world config file");
            _worldConfig = new(new(path, saveOnInit: false, new(PluginGuid, PluginName, PluginVersion)));
        }
        
        if (!Config.General.Enabled.Value)
            return false;

        var failed = false;
        var abort = false;
        if (RuntimeInformation.Instance.GameVersion != ExpectedGameVersion)
        {
            Logger.LogWarning(Invariant($"Unsupported game version: {RuntimeInformation.Instance.GameVersion}, expected: {ExpectedGameVersion}"));
            failed = true;
            abort |= !Config.General.IgnoreGameVersionCheck.Value;
        }
        if (RuntimeInformation.Instance.NetworkVersion != ExpectedNetworkVersion)
        {
            Logger.LogWarning(Invariant($"Unsupported network version: {RuntimeInformation.Instance.NetworkVersion}, expected: {ExpectedNetworkVersion}"));
            failed = true;
            abort |= !Config.General.IgnoreNetworkVersionCheck.Value;
        }
        if (RuntimeInformation.Instance.ItemDataVersion != ExpectedItemDataVersion)
        {
            Logger.LogWarning(Invariant($"Unsupported item data version: {RuntimeInformation.Instance.ItemDataVersion}, expected: {ExpectedItemDataVersion}"));
            failed = true;
            abort |= !Config.General.IgnoreItemDataVersionCheck.Value;
        }
        if (RuntimeInformation.Instance.WorldVersion != ExpectedWorldVersion)
        {
            Logger.LogWarning(Invariant($"Unsupported world version: {RuntimeInformation.Instance.WorldVersion}, expected: {ExpectedWorldVersion}"));
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
                return false;
            }
        }

#if DEBUG
        Logger.LogInfo(Invariant($"Registered Processors: {Processor.DefaultProcessors.Count}"));
#endif
        return true;
    }

    void OnConfigChanged(object sender, EventArgs e) => _configChanged = true;

    void Execute(PeersEnumerable peers)
    {
        _executeCounter++;
        if (_configChanged)
        {
            _configChanged = false;

            if (Config.GlobalsKeys.SetGlobalKeysFromConfig.Value)
                ZoneSystem.instance.ResetWorldKeys();

            if (Config.WorldModifiers.SetPresetFromConfig.Value)
            {
                try { MyTerminal.ExecuteCommand("setworldpreset", Invariant($"{Config.WorldModifiers.Preset.Value}")); }
                catch (Exception ex) { Logger.LogError(ex); }
            }

            if (Config.WorldModifiers.SetModifiersFromConfig.Value)
            {
                foreach (var (modifier, value) in Config.WorldModifiers.Modifiers.Select(x => (x.Key, x.Value.Value)))
                {
                    try { MyTerminal.ExecuteCommand("setworldmodifier", Invariant($"{modifier}"), Invariant($"{value}")); }
                    catch (Exception ex) { Logger.LogError(ex); }
                }
            }

            if (Config.GlobalsKeys.SetGlobalKeysFromConfig.Value)
            {
                /// <see cref="FejdStartup.ParseServerArguments"/>
                foreach (var (key, entry) in Config.GlobalsKeys.KeyConfigs.Where(x => !Equals(x.Value.BoxedValue, x.Value.DefaultValue)))
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
            }
            
            foreach (var processor in Processor.DefaultProcessors)
                processor.Initialize(_executeCounter is 1);

            if (_executeCounter is 1)
            {
#if DEBUG
                GenerateDefaultConfigMarkdown(base.Config);
                GenerateDocs();
#endif

                //base.Config.Bind(DummyConfigSection, "Dummy", "", Invariant($"Dummy entry which does nothing, it's abused to include runtime information in the config file:{Environment.NewLine}{RuntimeInformation.Instance}"));
                Config.ConfigFile.SettingChanged -= OnConfigChanged;
                Config.ConfigFile.SettingChanged += OnConfigChanged;
            }
            else
            {
                Logger.LogInfo("Configuration changed");
                foreach (var zdo in ZDOMan.instance.GetObjectsByID().Values.Cast<ExtendedZDO>())
                    zdo.ReregisterAllProcessors();
            }

            return;
        }

        _watch.Restart();

        peers.Update();
        if (peers.Count is 0)
            return;

        (_playerSectors, _playerSectorsOld) = (_playerSectorsOld, _playerSectors);
        foreach (var peer in peers)
        {
            var playerSector = ZoneSystem.GetZone(peer.m_refPos);
            for (int x = playerSector.x - Config.General.ZonesAroundPlayers.Value; x <= playerSector.x + Config.General.ZonesAroundPlayers.Value; x++)
            {
                for (int y = playerSector.y - Config.General.ZonesAroundPlayers.Value; y <= playerSector.y + Config.General.ZonesAroundPlayers.Value; y++)
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
            sectorInfo.InverseWeight = 0;
            sectorInfo.Peers.Clear();
            sectorInfo.ZDOs.Clear();
            _sectorInfoPool.Push(sectorInfo);
        }
        _playerSectorsOld.Clear();

        var playerSectors = _playerSectors.AsEnumerable();
        if (_unfinishedProcessingInRow > 10)
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
            playerSectors = playerSectors.OrderBy(x => x.Value.InverseWeight);
        }

        foreach (var processor in Processor.DefaultProcessors)
            processor.PreProcess();

        int processedSectors = 0;
        int processedZdos = 0;
        int totalZdos = 0;

        foreach (var (sector, sectorInfo) in playerSectors)
        {
            if (_watch.ElapsedMilliseconds >= Config.General.MaxProcessingTime.Value)
                break;

            processedSectors++;

            if (sectorInfo is { ZDOs: { Count: 0 } })
                ZDOMan.instance.FindSectorObjects(sector, 0, 0, sectorInfo.ZDOs);

            totalZdos += sectorInfo.ZDOs.Count;

            while (sectorInfo is { ZDOs.Count: > 0 } && _watch.ElapsedMilliseconds < Config.General.MaxProcessingTime.Value)
            {
                processedZdos++;
                var zdo = (ExtendedZDO)sectorInfo.ZDOs[sectorInfo.ZDOs.Count - 1];
                sectorInfo.ZDOs.RemoveAt(sectorInfo.ZDOs.Count - 1);
                if (!zdo.IsValid() || ReferenceEquals(zdo.PrefabInfo, PrefabInfo.Dummy))
                    continue;

                if (zdo.Processors.Count > 1)
                {
                    Processor? claimedExclusiveBy = null;
                    foreach (var processor in zdo.Processors)
                    {
                        if (!processor.ClaimExclusive(zdo))
                            continue;
                        if (claimedExclusiveBy is null)
                            claimedExclusiveBy = processor;
                        else if (Config.General.DiagnosticLogs.Value)
                            Logger.LogError(Invariant($"ZDO {zdo.m_uid} claimed exclusive by {processor.GetType().Name} while already claimed by {claimedExclusiveBy.GetType().Name}"));
                    }

                    if (claimedExclusiveBy is not null)
                        zdo.UnregisterProcessors(zdo.Processors.Where(x => x != claimedExclusiveBy));
                }

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
                else if (!destroy && _unregister.Count > 0)
                    zdo.UnregisterProcessors(_unregister);
            }
        }

        foreach (var processor in Processor.DefaultProcessors)
            processor.PostProcess();

        if (processedSectors < _playerSectors.Count || processedZdos < totalZdos)
            _unfinishedProcessingInRow++;
        else
            _unfinishedProcessingInRow = 0;

        _watch.Stop();

#if DEBUG
        var logLevel = _unfinishedProcessingInRow is 0 ? LogLevel.Debug : LogLevel.Info;
#else
        if (!Config.General.DiagnosticLogs.Value)
            return;
        var logLevel = _unfinishedProcessingInRow is 0 ? LogLevel.Debug : LogLevel.Info;
#endif

        Logger.Log(logLevel,
            Invariant($"{nameof(Execute)} took {_watch.ElapsedMilliseconds} ms to process {processedZdos} of {totalZdos} ZDOs in {processedSectors} of {_playerSectors.Count} zones. Incomplete runs in row: {_unfinishedProcessingInRow}"));

        Logger.Log(logLevel, Invariant($"Processing Time: {string.Join($", ", Processor.DefaultProcessors.Where(x => x.ProcessingTime.Ticks > 0).OrderByDescending(x => x.ProcessingTime.Ticks).Select(x => Invariant($"{x.GetType().Name}: {x.ProcessingTime.TotalMilliseconds}ms")))}"));
        //Logger.LogDebug(string.Join(Invariant(.$"{Environment.NewLine}  ", Processor.DefaultProcessors.Select(x => Invariant(.$"{x.GetType().Name}: {x.TotalProcessingTime}").Prepend("TotalProcessingTime:")));
    }

#if DEBUG
    static void GenerateDefaultConfigMarkdown(ConfigFile cfg)
    {
        using var writer = new StreamWriter(ConfigMarkdownPath, false, new UTF8Encoding(false));
        writer.WriteLine("|Category|Key|Default Value|Acceptable Values|Description|");
        writer.WriteLine("|--------|---|-------------|-----------------|-----------|");

        foreach (var (def, entry) in cfg.OrderBy(x => x.Key.Section).Select(x => (x.Key, x.Value)))
        {
            //if (def.Section == DummyConfigSection)
            //    continue;

            var section = Regex.Replace(def.Section, @"^[A-Z] - ", "");

            var accetableValues = entry.Description.AcceptableValues?.ToDescriptionString();
            if (accetableValues is not null)
                accetableValues = Regex.Replace(accetableValues, @"^#.+?\:\s*", "");
            else if (entry.SettingType == typeof(bool))
                accetableValues = Invariant($"{bool.TrueString}/{bool.FalseString}");
            else if (entry.SettingType.IsEnum)
            {
                if (entry.SettingType.GetCustomAttribute<FlagsAttribute>() is null)
                    accetableValues = Invariant($"One of {string.Join(", ", Enum.GetNames(entry.SettingType))}");
                else
                    accetableValues = Invariant($"Combination of {string.Join(", ", Enum.GetNames(entry.SettingType))}");
            }

            writer.WriteLine(Invariant($"|{section}|{def.Key}|{entry.DefaultValue}|{accetableValues}|{entry.Description.Description}|"));
        }
    }

    static void GenerateDocs()
    {
        var docsPath = Path.Combine(Path.GetDirectoryName(ConfigMarkdownPath), "Docs");
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
                .Where(x => x is not ZNetView)
                .ToList();

            if (components is not { Count: > 0})
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
                    .Select(x => (Type: x.GetType().Name, Name: x.name))
                    .OrderBy(x => x.Type).ThenBy(x => x.Name)
                    .Select(x => Invariant($"[{x.Type} ({x.Name})](Components/{x.Type}.md#{prefab.name.ToLowerInvariant().Replace(' ', '-')}-{x.Name.ToLowerInvariant().Replace(' ', '-')})")))));
        });

        Parallel.ForEach(componentsBag.GroupBy(x => x.Key.GetType()), group =>
        {
            var componentType = group.Key;
            var fields = componentFields[componentType];

            using var writer = new StreamWriter(Path.Combine(docsComponentsPath, Invariant($"{componentType.Name}.md")), false, new UTF8Encoding(false));
            writer.WriteLine(Invariant($"# {componentType.Name}"));
            writer.WriteLine();
            writer.WriteLine("The following section headers are in the format `Prefab.name: Component.name`.");
            writer.WriteLine();
            foreach (var (component, header) in group.Select(x => (x.Key, Invariant($"## {x.Value}: {x.Key.name}"))).OrderBy(x => x.Item2))
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

        return;

        static void WritePrefabsFile(string path, string filename, IEnumerable<(string Prefab, string? Name, string Components)> prefabs)
        {
            using var writer = new StreamWriter(Path.Combine(path, filename), false, new UTF8Encoding(false));
            writer.WriteLine("# Prefabs");
            writer.WriteLine();
            writer.WriteLine("|Prefab|Components|");
            writer.WriteLine("|------|----------|");
            foreach (var (prefab, name, components) in prefabs.OrderBy(x => x.Prefab))
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
            foreach (var (key, value) in Localization.instance.GetStrings().Select(x => (x.Key, x.Value)).OrderBy(x => x.Key))
                writer.WriteLine(Invariant($"|{key}|{value?.Replace("\n", "<br>") ?? "*null*"}|"));
        }

        static void WriteEventsFile(string path, string filename)
        {
            using var writer = new StreamWriter(Path.Combine(path, filename), false, new UTF8Encoding(false));
            writer.WriteLine("# Random Events");
            writer.WriteLine();
            writer.WriteLine("|Name|Player: required **not** known items (all)|Player: required **not** set keys (all)|Player: required known items (any)|Player: required keys (any)|Player: required keys (all)|");
            writer.WriteLine("|----|------------------------------------------|---------------------------------------|----------------------------------|---------------------------|---------------------------|");
            foreach (var ev in RandEventSystem.instance.m_events.Where(x => x.m_enabled && x.m_random).OrderBy(x => x.m_name))
            {
                /// <see cref="RandEventSystem.PlayerIsReadyForEvent(Player, RandomEvent)"/>
                var altRequiredNotKnownItems = string.Join("<br>", ev.m_altRequiredNotKnownItems.Select(x => $"- {x.name}"));
                var altNotRequiredPlayerKeys = string.Join("<br>", ev.m_altNotRequiredPlayerKeys.Select(x => $"- {x}"));
                var altRequiredKnownItems = string.Join("<br>", ev.m_altRequiredKnownItems.Select(x => $"- {x.name}"));
                var altRequiredPlayerKeysAny = string.Join("<br>", ev.m_altRequiredPlayerKeysAny.Select(x => $"- {x}"));
                var altRequiredPlayerKeysAll = string.Join("<br>", ev.m_altRequiredPlayerKeysAll.Select(x => $"- {x}"));
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

            foreach (var type in typeof(ZNet).Assembly.ExportedTypes.OrderBy(x => x.Name))
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).OrderBy(x => x.Name))
                {
                    if (!method.Name.StartsWith("RPC_"))
                        continue;
                    var parameters = string.Join(", ", method.GetParameters().Select(x => $"{x.ParameterType.Name} {x.Name}"));
                    writer.WriteLine($"|{type.Name}|{method.Name}|{parameters}|");
                }
            }
        }
    }
#endif

    sealed class PeersEnumerable(ZNetPeer? localPeer) : IEnumerable<Peer>
    {
        readonly ZNetPeer? _localPeer = localPeer;
        IReadOnlyList<ZNetPeer> _peers = [];

        public int Count => _peers.Count + (_localPeer is null ? 0 : 1);

        public void Update()
        {
            if (_localPeer is not null)
                _localPeer.m_refPos = ZNet.instance.GetReferencePosition();
            _peers = ZNet.instance.GetPeers();
        }

        public IEnumerator<Peer> GetEnumerator()
        {
            if (_localPeer is not null)
                yield return new(_localPeer);
            foreach (var peer in _peers)
                yield return new(peer);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    sealed class MyTerminal : Terminal
    {
        protected override Terminal m_terminalInstance => throw new NotImplementedException();

        //public static IReadOnlyDictionary<string, ConsoleCommand> Commands => commands;

        public static void ExecuteCommand(string command, params string[] args)
        {
            var cmd = commands[command];
            if (cmd.GetAction() is { } consoleEvent)
                consoleEvent(new MyConsoleEventArgs(command, args));
            else if (cmd.GetActionFailable() is { } consoleEventFailable)
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

    sealed class DummySocket : ISocket
    {
        public ISocket Accept()
        {
            throw new NotImplementedException();
        }

        public void Close() { }

        public void Dispose() { }

        public bool Flush()
        {
            throw new NotImplementedException();
        }

        public void GetAndResetStats(out int totalSent, out int totalRecv)
        {
            throw new NotImplementedException();
        }

        public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
        {
            throw new NotImplementedException();
        }

        public int GetCurrentSendRate()
        {
            throw new NotImplementedException();
        }

        public string GetEndPointString()
        {
            throw new NotImplementedException();
        }

        public string GetHostName() => "";

        public int GetHostPort()
        {
            throw new NotImplementedException();
        }

        public int GetSendQueueSize()
        {
            throw new NotImplementedException();
        }

        public bool GotNewData()
        {
            throw new NotImplementedException();
        }

        public bool IsConnected() => true;

        public bool IsHost()
        {
            throw new NotImplementedException();
        }

        public ZPackage Recv()
        {
            throw new NotImplementedException();
        }

        public void Send(ZPackage pkg)
        {
            throw new NotImplementedException();
        }

        public void VersionMatch()
        {
            throw new NotImplementedException();
        }
    }
}
