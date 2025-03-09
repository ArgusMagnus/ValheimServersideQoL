using BepInEx.Logging;
using BepInEx;
using System.Diagnostics;
using System.Collections.Concurrent;
using UnityEngine;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Valheim.ServersideQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed partial class Main : BaseUnityPlugin
{
    /// <Ideas>
    /// - Make tames lay eggs (by replacing spawned offspring with eggs and setting <see cref="EggGrow.m_grownPrefab"/>
    ///   Would probably not retain the value when picked up and dropped again. Could probably be solved by abusing same field in <see cref="EggGrow.m_item"/>
    /// - Option to make fireplaces consume fuel from containers to have an alternative to infinite fuel when making them toggleable
    /// - Scale eggs by quality by setting <see cref="ItemDrop.ItemData.SharedData.m_scaleByQuality". Not sure if we can modify shared data on clients though.
    ///   Check <see cref="ZNetView.LoadFields"/>
    ///   -> Probably not possible
    /// - Scale mobs by level
    ///   -> Probably not possible
    /// - make ship pickup sunken items
    /// - Change effect of <see cref="GlobalKeys.NoPortals"/> to prevent building of portal, but not the use of existing portals.
    ///   Show $msg_nobuildzone <see cref="Player.TryPlacePiece(Piece)"/>
    /// - Allow tames to follow through portals
    /// - Allow carts through portals
    /// - Modify container inventory sizes
    /// - Make carts ignore weights <see cref="Vagon"/>
    /// </summary>

    const string PluginName = "ServersideQoL";
    const string PluginGuid = $"argusmagnus.{PluginName}";
    static int PluginGuidHash { get; } = PluginGuid.GetStableHashCode();

    //static Harmony HarmonyInstance { get; } = new Harmony(pluginGUID);
    static new ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource(PluginName);

    readonly ModConfig _cfg;
    readonly Stopwatch _watch = new();

    internal static IReadOnlyList<string> ClockEmojis { get; } = ["🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠", "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"];
    readonly Regex _clockRegex = new($@"(?:{string.Join("|", ClockEmojis.Select(Regex.Escape))})(?:\s*\d\d\:\d\d)?");

    record PrefabInfo(GameObject Prefab, IReadOnlyDictionary<Type, Component> Components)
    {
        static T? Get<T>(IReadOnlyDictionary<Type, Component> prefabs) where T : Component => prefabs.TryGetValue(typeof(T), out var value) ? (T)value : null;
        public Sign? Sign { get; } = Get<Sign>(Components);
        public MapTable? MapTable { get; } = Get<MapTable>(Components);
        public Tameable? Tameable { get; } = Get<Tameable>(Components);
        public Character? Character { get; } = Get<Character>(Components);
        public Fireplace? Fireplace { get; } = Get<Fireplace>(Components);
        public Container? Container { get; } = Get<Container>(Components);
        public Ship? Ship { get; } = Get<Ship>(Components);
        public ItemDrop? ItemDrop { get; } = Get<ItemDrop>(Components);
        public Piece? Piece { get; } = Get<Piece>(Components);
        public Smelter? Smelter { get; } = Get<Smelter>(Components);
        public Windmill? Windmill { get; } = Get<Windmill>(Components);
    }

    readonly IReadOnlyDictionary<int, PrefabInfo> _prefabInfo = new Dictionary<int, PrefabInfo>();
    readonly ConcurrentHashSet<ZDOID> _ships = new();
    readonly ConcurrentDictionary<ZDOID, uint> _dataRevisions = new();

    record struct ItemKey(string Name, int Quality, int Variant)
    {
        public static implicit operator ItemKey(ItemDrop.ItemData data) => new(data);
        public ItemKey(ItemDrop.ItemData data) : this(data.m_shared.m_name, data.m_quality, data.m_variant) { }
    }

    record struct SharedItemDataKey(string Name)
    {
        public static implicit operator SharedItemDataKey(ItemDrop.ItemData.SharedData data) => new(data.m_name);
    }

    readonly ConcurrentDictionary<SharedItemDataKey, ConcurrentDictionary<ZDOID, Inventory>> _containersByItemName = new();

    ulong _executeCounter;
    uint _unfinishedProcessingInRow;
    bool _resetPrefabInfo;
    record SectorInfo(List<ZNetPeer> Peers, List<ZDO> ZDOs)
    {
        public int InverseWeight { get; set; }
    }
    ConcurrentDictionary<Vector2i, SectorInfo> _playerSectors = new();
    ConcurrentDictionary<Vector2i, SectorInfo> _playerSectorsOld = new();

    record Pin(long OwnerId, string Tag, Vector3 Pos, Minimap.PinType Type, bool IsChecked, string Author);
    readonly List<Pin> _pins = new();
    int _pinsHash;
    Regex? _includePortalRegex;
    Regex? _excludePortalRegex;

    public Main()
    {
        _cfg = new(Config);
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
            Logger.LogWarning($"Unsupported game version: {gameVersion}, expected: {ExpectedGameVersion}");
            failed = true;
            abort |= !_cfg.General.IgnoreGameVersionCheck.Value;
        }
        if (versionType.GetField("m_networkVersion")?.GetValue(null) is not uint networkVersion)
            networkVersion = default;
        if (networkVersion != ExpectedNetworkVersion)
        {
            Logger.LogWarning($"Unsupported network version: {networkVersion}, expected: {ExpectedNetworkVersion}");
            failed = true;
            abort |= !_cfg.General.IgnoreNetworkVersionCheck.Value;
        }
        if (versionType.GetField("m_itemDataVersion")?.GetValue(null) is not int itemDataVersion)
            itemDataVersion = default;
        if (itemDataVersion != ExpectedItemDataVersion)
        {
            Logger.LogWarning($"Unsupported item data version: {itemDataVersion}, expected: {ExpectedItemDataVersion}");
            failed = true;
            abort |= !_cfg.General.IgnoreItemDataVersionCheck.Value;
        }
        if (versionType.GetField("m_worldVersion")?.GetValue(null) is not int worldVersion)
            worldVersion = default;
        if (worldVersion != ExpectedWorldVersion)
        {
            Logger.LogWarning($"Unsupported world version: {worldVersion}, expected: {ExpectedWorldVersion}");
            failed = true;
            abort |= !_cfg.General.IgnoreWorldVersionCheck.Value;
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

        StartCoroutine(CallExecute());

        IEnumerator<YieldInstruction> CallExecute()
        {
            yield return new WaitForSeconds(_cfg.General.StartDelay.Value);
            while (true)
            {
                try { Execute(); }
                catch (OperationCanceledException) { break; }
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
            Logger.LogWarning("Mod should only be installed on the host");
            throw new OperationCanceledException();
        }

        if (ZNetScene.instance is null || ZDOMan.instance is null)
            return;

        _watch.Restart();

        if (_executeCounter++ is 0 || _resetPrefabInfo)
        {
            _resetPrefabInfo = false;
            var dict = (IDictionary<int, PrefabInfo>)_prefabInfo;
            dict.Clear();
            _ships.Clear();

            var filter = _cfg.MapTables.AutoUpdatePortalsInclude.Value.Trim();
            _includePortalRegex = string.IsNullOrEmpty(filter) ? null : new(ConvertToRegexPattern(filter));
            filter = _cfg.MapTables.AutoUpdatePortalsExclude.Value.Trim();
            _excludePortalRegex = string.IsNullOrEmpty(filter) ? null : new(ConvertToRegexPattern(filter));

            List<HashSet<Type>> requiredTypes = new();
            foreach (var sectionProperty in _cfg.GetType().GetProperties().Where(x => x.PropertyType.IsClass))
            {
                object? section = null;
                IEnumerable<RequiredPrefabsAttribute>? classAttr = null;
                foreach (var keyProperty in sectionProperty.PropertyType.GetProperties())
                {
                    var attrs = keyProperty.GetCustomAttributes<RequiredPrefabsAttribute>();
                    if (keyProperty.PropertyType != typeof(ConfigEntry<bool>))
                    {
                        if (attrs.Any())
                            throw new Exception($"{nameof(RequiredPrefabsAttribute)} only supported on classes and properties of type {nameof(ConfigEntry<bool>)}");
                        continue;
                    }

                    section ??= sectionProperty.GetValue(_cfg);

                    if (!((ConfigEntry<bool>)keyProperty.GetValue(section)).Value)
                        continue;

                    classAttr ??= sectionProperty.PropertyType.GetCustomAttributes<RequiredPrefabsAttribute>();

                    foreach (var attr in attrs)
                    {
                        var types = attr.Prefabs.ToHashSet();
                        if (!requiredTypes.Any(x => x.SequenceEqual(types)))
                            requiredTypes.Add(types);
                    }
                }

                foreach (var attr in classAttr ?? [])
                {
                    if (attr.Prefabs.ToHashSet() is { } classTypes && !requiredTypes.Any(x => x.SequenceEqual(classTypes)))
                        requiredTypes.Add(classTypes);
                }
            }

            if (requiredTypes is { Count: > 0 })
            {
                var needsShips = false;
                foreach (var prefab in ZNetScene.instance.m_prefabs)
                {
                    Dictionary<Type, Component>? components = null;
                    foreach (var requiredTypeList in requiredTypes)
                    {
                        var prefabs = requiredTypeList.Select(x => (Type: x, Component: prefab.GetComponent(x))).Where(x => x.Component is not null).ToList();
                        if (prefabs.Count != requiredTypeList.Count)
                            continue;
                        foreach (var (type, component) in prefabs)
                        {
                            components ??= new();
                            if (!components.ContainsKey(type))
                                components.Add(type, component);
                        }
                    }
                    if (components is not null)
                    {
                        var prefabInfo = new PrefabInfo(prefab, components);
                        dict.Add(prefab.name.GetStableHashCode(), prefabInfo);
                        needsShips = needsShips || prefabInfo.Ship is not null;
                    }
                }

                if (needsShips)
                {
                    foreach (var zdo in ((IReadOnlyDictionary<ZDOID, ZDO>)typeof(ZDOMan)
                        .GetField("m_objectsByID", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(ZDOMan.instance)).Values.Where(x => _prefabInfo.TryGetValue(x.GetPrefab(), out var info) && info.Ship is not null))
                        _ships.Add(zdo.m_uid);
                }
            }
            return;
        }

        if (_executeCounter % (ulong)(60 * _cfg.General.Frequency.Value) is 0)
        {
            foreach (var id in _dataRevisions.Keys)
            {
                if (ZDOMan.instance.GetZDO(id) is null)
                    _dataRevisions.TryRemove(id, out _);
            }

            foreach (var dict in _containersByItemName.Values)
            {
                foreach (var id in dict.Keys)
                {
                    if (ZDOMan.instance.GetZDO(id) is null)
                        dict.TryRemove(id, out _);
                }
            }
        }

        if (ZNet.instance.GetPeers() is not { Count: > 0 } peers)
            return;

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

        string? timeText = null;
        List<Pin>? existingPins = null;
        byte[]? emptyExplored = null;
        _pins.Clear();
        int oldPinsHash = 0;

        var playerSectors = _playerSectors.AsEnumerable();
        if (_unfinishedProcessingInRow > SortPlayerSectorsThreshold)
            playerSectors = playerSectors.OrderBy(x => x.Value.InverseWeight);

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
                if (!zdo.IsValid() || !_prefabInfo.TryGetValue(zdo.GetPrefab(), out var prefabInfo))
                    continue;

                if (!_cfg.Signs.TimeSigns.Value || prefabInfo.Sign is not null)
                {
                    var text = zdo.GetString(ZDOVars.s_text);
                    var newText = _clockRegex.Replace(text, match =>
                    {
                        if (timeText is null)
                        {
                            var dayFraction = EnvMan.instance.GetDayFraction();
                            var emojiIdx = (int)Math.Floor(ClockEmojis.Count * 2 * dayFraction) % ClockEmojis.Count;
                            var time = TimeSpan.FromDays(dayFraction);
                            timeText = $@"{ClockEmojis[emojiIdx]} {time:hh\:mm}";
                        }
                        return timeText;
                    });

                    if (text != newText)
                    {
                        Logger.LogDebug($"Changing sign text from '{text}' to '{newText}'");
                        zdo.Set(ZDOVars.s_text, newText);
                        //zdo.Set(ZDOVars.s_author, );
                    }
                }

                if (prefabInfo.MapTable is not null && (_cfg.MapTables.AutoUpdatePortals.Value || _cfg.MapTables.AutoUpdateShips.Value))
                {
                    if (_pins is { Count: 0 })
                    {
                        var pins = Enumerable.Empty<Pin>();
                        if (_cfg.MapTables.AutoUpdatePortals.Value)
                        {
                            pins = pins.Concat(ZDOMan.instance.GetPortals().Select(x => new Pin(PluginGuidHash, x.GetString(ZDOVars.s_tag), x.GetPosition(), Minimap.PinType.Icon4, false, PluginGuid)));
                            if ((_includePortalRegex ?? _excludePortalRegex) is not null)
                                pins = pins.Where(x => _includePortalRegex?.IsMatch(x.Tag) is not false && _excludePortalRegex?.IsMatch(x.Tag) is not true);
                        }
                        if (_cfg.MapTables.AutoUpdateShips.Value)
                        {
                            pins = pins.Concat(_ships
                                .Select(x =>
                                {
                                    var y = ZDOMan.instance.GetZDO(x);
                                    if (y is null)
                                        _ships.Remove(x);
                                    return y;
                                })
                                .Where(x => x is not null)
                                .Select(x => new Pin(PluginGuidHash, _prefabInfo.TryGetValue(x!.GetPrefab(), out var info) ? info.Piece?.m_name ?? "" : "", x.GetPosition(), Minimap.PinType.Player, false, PluginGuid)));
                        }

                        foreach (var pin in pins)
                        {
                            _pins.Add(pin);
                            oldPinsHash = (oldPinsHash, pin).GetHashCode();
                        }

                        (_pinsHash, oldPinsHash) = (oldPinsHash, _pinsHash);
                    }

                    if (_pinsHash == oldPinsHash && _dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
                        continue;

                    existingPins?.Clear();
                    ZPackage pkg;
                    var data = zdo.GetByteArray(ZDOVars.s_data);
                    if (data is not null)
                    {
                        data = Utils.Decompress(data);
                        pkg = new ZPackage(data);
                        var version = pkg.ReadInt();
                        if (version is not 3)
                        {
                            Logger.LogWarning($"MapTable data version {version} is not supported");
                            continue;
                        }
                        data = pkg.ReadByteArray();
                        if (data.Length != Minimap.instance.m_textureSize * Minimap.instance.m_textureSize)
                        {
                            Logger.LogWarning("Invalid explored map data length");
                            data = null;
                        }

                        var pinCount = pkg.ReadInt();
                        existingPins ??= new(pinCount);
                        if (existingPins.Capacity < pinCount)
                            existingPins.Capacity = pinCount;

                        foreach (var i in Enumerable.Range(0, pinCount))
                        {
                            var pin = new Pin(pkg.ReadLong(), pkg.ReadString(), pkg.ReadVector3(), (Minimap.PinType)pkg.ReadInt(), pkg.ReadBool(), pkg.ReadString());
                            if (pin.OwnerId != PluginGuidHash)
                                existingPins.Add(pin);
                        }
                    }

                    /// taken from <see cref="Minimap.GetSharedMapData"/> and <see cref="MapTable.GetMapData"/> 
                    pkg = new ZPackage();
                    pkg.Write(3);

                    pkg.Write(data ?? (emptyExplored ??= new byte[Minimap.instance.m_textureSize * Minimap.instance.m_textureSize]));

                    pkg.Write(_pins.Count + (existingPins?.Count ?? 0));
                    foreach (var pin in _pins.Concat(existingPins?.AsEnumerable() ?? []))
                    {
                        pkg.Write(pin.OwnerId);
                        pkg.Write(pin.Tag);
                        pkg.Write(pin.Pos);
                        pkg.Write((int)pin.Type);
                        pkg.Write(pin.IsChecked);
                        pkg.Write(pin.Author);
                    }

                    zdo.Set(ZDOVars.s_data, Utils.Compress(pkg.GetArray()));
                    _dataRevisions[zdo.m_uid] = zdo.DataRevision;

                    ShowMessage(sectorInfo.Peers, MessageHud.MessageType.TopLeft, "$msg_mapsaved");
                    continue;
                }

                if (prefabInfo.Tameable is not null)
                {
                    if (_dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
                        continue;

                    OptionalBool tamed = default;

                    if (_cfg.Tames.MakeCommandable.Value && !prefabInfo.Tameable.m_commandable && (tamed = zdo.GetBool(ZDOVars.s_tamed)))
                    {
                        zdo.Set(ZDOVarsEx.HasFields, true);
                        zdo.Set(ZDOVarsEx.GetHasFields<Tameable>(), true);
                        zdo.Set(ZDOVarsEx.TameableCommandable, true);
                    }
                    if (_cfg.Tames.SendTamingPogressMessages.Value && !(tamed.HasValue ? tamed : (tamed = zdo.GetBool(ZDOVars.s_tamed))))
                    {
                        /// <see cref="Tameable.GetRemainingTime()"/>
                        var tameTime = prefabInfo.Tameable.m_tamingTime;
                        var hasFields = zdo.GetBool(ZDOVarsEx.GetHasFields<Tameable>());
                        if (hasFields)
                            tameTime = zdo.GetFloat(ZDOVarsEx.TameableTamingTime, tameTime);
                        var tameTimeLeft = zdo.GetFloat(ZDOVars.s_tameTimeLeft, tameTime);
                        if (tameTimeLeft < tameTime)
                        {
                            var tameness = 1f - Mathf.Clamp01(tameTimeLeft / tameTime);
                            var range = prefabInfo.Tameable.m_tamingSpeedMultiplierRange;
                            if (hasFields)
                                range = zdo.GetFloat(ZDOVarsEx.TameableTamingSpeedMultiplierRange, range);
                            var playersInRange = peers.Where(x => Vector3.Distance(x.m_refPos, zdo.GetPosition()) < range);
                            ShowMessage(playersInRange, MessageHud.MessageType.TopLeft, $"{prefabInfo.Character?.m_name}: $hud_tameness {tameness:P0}");
                        }
                    }
                    _dataRevisions[zdo.m_uid] = zdo.DataRevision;

                    //zdo.GetConnection().m_type
                    //zdo.GetConnectionType() is ZDOExtraData.ConnectionType.Target
                    //if (zdo.GetString(ZDOVars.s_follow) is { Length: > 0} follow)
                    //{
                    //    Logger.LogInfo($"Following {follow}");
                    //    if (peers.FirstOrDefault(x => x.m_playerName == follow) is { } player && Utils.DistanceXZ(player.m_refPos, zdo.GetPosition()) < 10)
                    //    {
                    //        Logger.LogInfo($"Pause following {follow}");
                    //        __tameFollow[zdo] = follow;
                    //        zdo.Set($"{nameof(Tameable)}.{nameof(Tameable.m_commandable)}".GetStableHashCode(), true);
                    //        zdo.Set(ZDOVars.s_follow, "");
                    //    }
                    //}
                    //else if (__tameFollow.TryGetValue(zdo, out follow) && !string.IsNullOrEmpty(follow))
                    //{
                    //    if (peers.FirstOrDefault(x => x.m_playerName == follow) is { } player && Utils.DistanceXZ(player.m_refPos, zdo.GetPosition()) >= 10)
                    //    {
                    //        Logger.LogInfo($"Resume following {follow}");
                    //        __tameFollow[zdo] = null;
                    //        zdo.Set(ZDOVars.s_follow, follow);
                    //    }
                    //}
                }

                if (prefabInfo.Ship is not null)
                    _ships.Add(zdo.m_uid);

                if (prefabInfo.Fireplace is not null && _cfg.Fireplaces.MakeToggleable.Value)
                {
                    if (_dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
                        continue;

                    zdo.Set(ZDOVarsEx.HasFields, true);
                    zdo.Set(ZDOVarsEx.GetHasFields<Fireplace>(), true);
                    // setting FireplaceInfiniteFuel to true works, but removes the turn on/off hover text (turning on/off still works)
                    //zdo.Set(ZDOVarsEx.FireplaceInfiniteFuel, false);
                    zdo.Set(ZDOVarsEx.FireplaceFuelPerSec, 0f);
                    zdo.Set(ZDOVarsEx.FireplaceCanTurnOff, true);
                    zdo.Set(ZDOVarsEx.FireplaceCanRefill, false);
                    _dataRevisions[zdo.m_uid] = zdo.DataRevision;
                }

                if (prefabInfo is { Container: not null, Piece: not null })
                {
                    if (_dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && zdo.DataRevision == dataRevision)
                        continue;

                    if (zdo.GetLong(ZDOVars.s_creator) is 0)
                        continue; // ignore non-player-built chests (such as TreasureChest_*)

                    if (zdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, zdo))
                        continue; // in use or player to close

                    _dataRevisions[zdo.m_uid] = zdo.DataRevision;

                    var data = zdo.GetString(ZDOVars.s_items);
                    if (string.IsNullOrEmpty(data))
                        continue;

                    /// <see cref="Container.Load"/>
                    /// <see cref="Container.Save"/>
                    var width = prefabInfo.Container.m_width;
                    var height = prefabInfo.Container.m_height;
                    if (zdo.GetBool(ZDOVarsEx.GetHasFields<Container>()))
                    {
                        width = zdo.GetInt(ZDOVarsEx.ContainerWidth, width);
                        height = zdo.GetInt(ZDOVarsEx.ContainerHeight, height);
                    }
                    Inventory inventory = new(prefabInfo.Container.m_name, prefabInfo.Container.m_bkg, width, height);
                    inventory.Load(new(data));
                    var changed = false;
                    var x = 0;
                    var y = 0;
                    foreach (var item in inventory.GetAllItems()
                        .OrderBy(x => x.IsEquipable() ? 0 : 1)
                        .ThenBy(x => x.m_shared.m_name)
                        .ThenByDescending(x => x.m_stack))
                    {
                        var dict = _containersByItemName.GetOrAdd(item.m_shared, static _ => new());
                        dict[zdo.m_uid] = inventory;
                        if (!_cfg.Containers.AutoSort.Value)
                            continue;

                        // todo: merge stacks

                        if (item.m_gridPos.x != x || item.m_gridPos.y != y)
                        {
                            item.m_gridPos.x = x;
                            item.m_gridPos.y = y;
                            changed = true;
                        }
                        if (++x >= width)
                        {
                            x = 0;
                            y++;
                        }
                    }

                    if (!changed)
                        continue;

                    var pkg = new ZPackage();
                    inventory.Save(pkg);
                    data = pkg.GetBase64();
                    zdo.Set(ZDOVars.s_items, data);
                    _dataRevisions[zdo.m_uid] = zdo.DataRevision;
                    ShowMessage(sectorInfo.Peers, MessageHud.MessageType.TopLeft, $"{prefabInfo.Piece.m_name} sorted");
                }

                if (prefabInfo.ItemDrop is not null && _cfg.Containers.AutoPickup.Value)
                {
                    if (prefabInfo.Piece is not null && zdo.GetBool(ZDOVars.s_piece))
                        continue; // ignore placed items (such as feasts)

                    if (!CheckMinDistance(peers, zdo, _cfg.Containers.AutoPickupMinPlayerDistance.Value))
                        continue; // player to close

                    var shared = ZNetScene.instance.GetPrefab(zdo.GetPrefab()).GetComponent<ItemDrop>().m_itemData.m_shared;
                    if (!_containersByItemName.TryGetValue(shared, out var dict))
                        continue;

                    HashSet<Vector2i>? usedSlots = null;
                    ItemDrop.ItemData? item = null;

                    foreach (var (containerZdoId, inventory) in dict.Select(x => (x.Key, x.Value)))
                    {
                        if (ZDOMan.instance.GetZDO(containerZdoId) is not { } containerZdo)
                        {
                            dict.TryRemove(containerZdoId, out _);
                            continue;
                        }

                        if (!_dataRevisions.TryGetValue(containerZdoId, out var containerDataRevision) || containerZdo.DataRevision != containerDataRevision)
                            continue;

                        if (Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) > _cfg.Containers.AutoPickupRange.Value * _cfg.Containers.AutoPickupRange.Value)
                            continue;

                        if (containerZdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, containerZdo))
                            continue; // in use or player to close

                        inventory.Update(containerZdo);

                        if (item is null)
                        {
                            item = new() { m_shared = shared };
                            PrivateAccessor.LoadFromZDO(item, zdo);
                        }

                        var stack = item.m_stack;
                        usedSlots ??= new();
                        usedSlots.Clear();

                        ItemDrop.ItemData? containerItem = null;
                        foreach (var slot in inventory.GetAllItems())
                        {
                            usedSlots.Add(slot.m_gridPos);
                            if (new ItemKey(item) != slot)
                                continue;

                            containerItem ??= slot;

                            var maxAmount = slot.m_shared.m_maxStackSize - slot.m_stack;
                            if (maxAmount <= 0)
                                continue;

                            var amount = Math.Min(stack, maxAmount);
                            slot.m_stack += amount;
                            stack -= amount;
                            if (stack is 0)
                                break;
                        }

                        if (containerItem is null)
                        {
                            dict.TryRemove(containerZdoId, out _);
                            if (dict is { Count: 0 })
                                _containersByItemName.TryRemove(item.m_shared, out _);
                            continue;
                        }

                        if (!ReferenceEquals(inventory.GetAllItems(), inventory.GetAllItems()))
                            throw new Exception("Algorithm assumption violated");

                        for (var emptySlots = inventory.GetEmptySlots(); stack > 0 && emptySlots > 0; emptySlots--)
                        {
                            var amount = Math.Min(stack, item.m_shared.m_maxStackSize);

                            var slot = containerItem.Clone();
                            slot.m_stack = amount;
                            slot.m_gridPos.x = -1;
                            for (int x = 0; x < inventory.GetWidth() && slot.m_gridPos.x < 0; x++)
                            {
                                for (int y = 0; y < inventory.GetHeight(); y++)
                                {
                                    if (usedSlots.Add(new(x, y)))
                                    {
                                        (slot.m_gridPos.x, slot.m_gridPos.y) = (x, y);
                                        break;
                                    }
                                }
                            }
                            inventory.GetAllItems().Add(slot);
                            stack -= amount;
                        }

                        if (stack != item.m_stack)
                        {
                            var pkg = new ZPackage();
                            inventory.Save(pkg);
                            containerZdo.Set(ZDOVars.s_items, pkg.GetBase64());
                            _dataRevisions[containerZdo.m_uid] = containerZdo.DataRevision;
                            (item.m_stack, stack) = (stack, item.m_stack);
                            zdo.SetOwner(ZDOMan.GetSessionID());
                            ItemDrop.SaveToZDO(item, zdo);
                            ShowMessage(sectorInfo.Peers, MessageHud.MessageType.TopLeft, $"{_prefabInfo[containerZdo.GetPrefab()].Piece!.m_name}: $msg_added {item.m_shared.m_name} {stack}x");
                        }

                        if (item.m_stack is 0)
                            break;
                    }

                    if (item?.m_stack is 0)
                    {
                        zdo.SetOwner(ZDOMan.GetSessionID());
                        ZDOMan.instance.DestroyZDO(zdo);
                    }
                }

                if (_cfg.Smelters.FeedFromContainers.Value && prefabInfo.Smelter is not null)
                {
                    if (!CheckMinDistance(peers, zdo))
                        continue; // player to close

                    var hasFields = zdo.GetBool(ZDOVarsEx.GetHasFields<Smelter>());

                    /// <see cref="Smelter.OnAddFuel"/>
                    {
                        int maxFuel = prefabInfo.Smelter.m_maxFuel;
                        if (hasFields)
                            maxFuel = zdo.GetInt(ZDOVarsEx.SmelterMaxFuel, maxFuel);
                        var currentFuel = zdo.GetFloat(ZDOVars.s_fuel);
                        var maxFuelAdd = (int)(maxFuel - currentFuel);
                        if (maxFuelAdd > maxFuel / 2)
                        {
                            var fuelItem = prefabInfo.Smelter.m_fuelItem.m_itemData;
                            var addedFuel = 0;
                            if (_containersByItemName.TryGetValue(fuelItem.m_shared, out var containers))
                            {
                                List<ItemDrop.ItemData>? removeSlots = null;
                                foreach (var (containerZdoId, inventory) in containers.Select(x => (x.Key, x.Value)))
                                {
                                    if (ZDOMan.instance.GetZDO(containerZdoId) is not { } containerZdo)
                                    {
                                        containers.TryRemove(containerZdoId, out _);
                                        continue;
                                    }

                                    if (!_dataRevisions.TryGetValue(containerZdoId, out var containerDataRevision) || containerZdo.DataRevision != containerDataRevision)
                                        continue;

                                    if (Utils.DistanceXZ(zdo.GetPosition(), containerZdo.GetPosition()) > 4)
                                        continue;

                                    if (containerZdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, containerZdo))
                                        continue; // in use or player to close

                                    inventory.Update(containerZdo);

                                    removeSlots?.Clear();
                                    float addFuel = 0;
                                    var leave = _cfg.Smelters.FeedFromContainersLeaveAtLeastFuel.Value;
                                    foreach (var slot in inventory.GetAllItems().Where(x => new ItemKey(x) == fuelItem).OrderBy(x => x.m_stack))
                                    {
                                        var take = Math.Min(maxFuelAdd, slot.m_stack);
                                        var leaveDiff = Math.Min(take, leave);
                                        leave -= leaveDiff;
                                        take -= leaveDiff;
                                        if (take is 0)
                                            continue;

                                        addFuel += take;
                                        slot.m_stack -= take;
                                        if (slot.m_stack is 0)
                                            (removeSlots ??= new()).Add(slot);

                                        maxFuelAdd -= take;
                                        if (maxFuelAdd is 0)
                                            break;
                                    }

                                    if (addFuel is 0)
                                    {
                                        containers.TryRemove(containerZdoId, out _);
                                        if (containers is { Count: 0 })
                                            _containersByItemName.TryRemove(fuelItem.m_shared, out _);
                                        continue;
                                    }

                                    if (removeSlots is { Count: > 0 })
                                    {
                                        if (!ReferenceEquals(inventory.GetAllItems(), inventory.GetAllItems()))
                                            throw new Exception("Algorithm assumption violated");
                                        foreach (var remove in removeSlots)
                                            inventory.GetAllItems().Remove(remove);

                                        if (inventory.GetAllItems() is { Count: 0 })
                                        {
                                            containers.TryRemove(containerZdoId, out _);
                                            if (containers is { Count: 0 })
                                                _containersByItemName.TryRemove(fuelItem.m_shared, out _);
                                        }
                                    }

                                    zdo.Set(ZDOVars.s_fuel, currentFuel + addFuel);

                                    var pkg = new ZPackage();
                                    inventory.Save(pkg);
                                    containerZdo.Set(ZDOVars.s_items, pkg.GetBase64());
                                    _dataRevisions[containerZdo.m_uid] = containerZdo.DataRevision;

                                    addedFuel += (int)addFuel;

                                    if (maxFuelAdd is 0)
                                        break;
                                }
                            }

                            if (addedFuel is not 0)
                                ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{prefabInfo.Piece?.m_name ?? prefabInfo.Smelter.m_name}: $msg_added {fuelItem.m_shared.m_name} {addedFuel}x");
                        }
                    }

                    /// <see cref="Smelter.OnAddOre"/> <see cref="Smelter.QueueOre"/>
                    {
                        int maxOre = prefabInfo.Smelter.m_maxOre;
                        if (hasFields)
                            maxOre = zdo.GetInt(ZDOVarsEx.SmelterMaxOre, maxOre);
                        var currentOre = zdo.GetInt(ZDOVars.s_queued);
                        var maxOreAdd = maxOre - zdo.GetInt(ZDOVars.s_queued);
                        if (maxOreAdd > maxOre / 2)
                        {
                            foreach (var conversion in prefabInfo.Smelter.m_conversion)
                            {
                                var oreItem = conversion.m_from.m_itemData;
                                var addedOre = 0;
                                if (_containersByItemName.TryGetValue(oreItem.m_shared, out var containers))
                                {
                                    List<ItemDrop.ItemData>? removeSlots = null;
                                    foreach (var (containerZdoId, inventory) in containers.Select(x => (x.Key, x.Value)))
                                    {
                                        if (ZDOMan.instance.GetZDO(containerZdoId) is not { } containerZdo)
                                        {
                                            containers.TryRemove(containerZdoId, out _);
                                            continue;
                                        }

                                        if (!_dataRevisions.TryGetValue(containerZdoId, out var containerDataRevision) || containerZdo.DataRevision != containerDataRevision)
                                            continue;

                                        if (Utils.DistanceXZ(zdo.GetPosition(), containerZdo.GetPosition()) > 4)
                                            continue;

                                        if (containerZdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, containerZdo))
                                            continue; // in use or player to close

                                        inventory.Update(containerZdo);

                                        removeSlots?.Clear();
                                        int addOre = 0;
                                        var leave = _cfg.Smelters.FeedFromContainersLeaveAtLeastOre.Value;
                                        foreach (var slot in inventory.GetAllItems().Where(x => new ItemKey(x) == oreItem).OrderBy(x => x.m_stack))
                                        {
                                            var take = Math.Min(maxOreAdd, slot.m_stack);
                                            var leaveDiff = Math.Min(take, leave);
                                            leave -= leaveDiff;
                                            take -= leaveDiff;
                                            if (take is 0)
                                                continue;

                                            addOre += take;
                                            slot.m_stack -= take;
                                            if (slot.m_stack is 0)
                                                (removeSlots ??= new()).Add(slot);

                                            maxOreAdd -= take;
                                            if (maxOreAdd is 0)
                                                break;
                                        }

                                        if (addOre is 0)
                                        {
                                            containers.TryRemove(containerZdoId, out _);
                                            if (containers is { Count: 0 })
                                                _containersByItemName.TryRemove(oreItem.m_shared, out _);
                                            continue;
                                        }

                                        if (removeSlots is { Count: > 0 })
                                        {
                                            if (!ReferenceEquals(inventory.GetAllItems(), inventory.GetAllItems()))
                                                throw new Exception("Algorithm assumption violated");
                                            foreach (var remove in removeSlots)
                                                inventory.GetAllItems().Remove(remove);

                                            if (inventory.GetAllItems() is { Count: 0 })
                                            {
                                                containers.TryRemove(containerZdoId, out _);
                                                if (containers is { Count: 0 })
                                                    _containersByItemName.TryRemove(oreItem.m_shared, out _);
                                            }
                                        }

                                        zdo.SetOwner(ZDOMan.GetSessionID());
                                        for (int i = 0; i < addOre; i++)
                                            zdo.Set($"item{currentOre + i}", conversion.m_from.gameObject.name);
                                        zdo.Set(ZDOVars.s_queued, currentOre + addOre);

                                        var pkg = new ZPackage();
                                        inventory.Save(pkg);
                                        containerZdo.Set(ZDOVars.s_items, pkg.GetBase64());
                                        _dataRevisions[containerZdo.m_uid] = containerZdo.DataRevision;

                                        addedOre += addOre;

                                        if (maxOreAdd is 0)
                                            break;
                                    }
                                }

                                if (addedOre is not 0)
                                    ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{prefabInfo.Piece?.m_name ?? prefabInfo.Smelter.m_name}: $msg_added {oreItem.m_shared.m_name} {addedOre}x");
                            }
                        }
                    }

                }

                if (prefabInfo.Windmill is not null && _cfg.Windmills.IgnoreWind.Value)
                {
                    if (_dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
                        continue;

                    /// <see cref="Windmill.GetPowerOutput()"/>
                    zdo.Set(ZDOVarsEx.HasFields, true);
                    zdo.Set(ZDOVarsEx.GetHasFields<Windmill>(), true);
                    zdo.Set(ZDOVarsEx.WindmillMinWindSpeed, float.MinValue);
                }
            }
        }

        if (processedSectors < _playerSectors.Count || processedZdos < totalZdos)
            _unfinishedProcessingInRow++;
        else
            _unfinishedProcessingInRow = 0;

        _watch.Stop();
        Logger.Log(_watch.ElapsedMilliseconds > _cfg.General.MaxProcessingTime.Value ? LogLevel.Info : LogLevel.Debug,
            $"{nameof(Execute)} took {_watch.ElapsedMilliseconds} ms to process {processedZdos} of {totalZdos} ZDOs in {processedSectors} of {_playerSectors.Count} zones. Uncomplete runs in row: {_unfinishedProcessingInRow}");
    }

    static void Log(LogLevel logLevel, string text = "", [CallerLineNumber] int lineNo = default)
        => Logger.Log(logLevel, string.IsNullOrEmpty(text) ? $"Line: {lineNo}" : $"Line: {lineNo}: {text}");

    static void ShowMessage(IEnumerable<ZNetPeer> peers, MessageHud.MessageType type, string message)
    {
        /// Invoke <see cref="MessageHud.RPC_ShowMessage"/>
        foreach (var peer in peers)
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "ShowMessage", (int)type, message);
    }

    bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo)
        => CheckMinDistance(peers, zdo, _cfg.General.MinPlayerDistance.Value);

    bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo, float minDistance)
        => peers.Min(x => Utils.DistanceSqr(x.m_refPos, zdo.GetPosition())) >= minDistance * minDistance;

    static string ConvertToRegexPattern(string searchPattern)
    {
        searchPattern = Regex.Escape(searchPattern);
        searchPattern = searchPattern.Replace("\\*", ".*").Replace("\\?", ".?");
        return $"(?i)^{searchPattern}$";
    }

    readonly struct OptionalBool
    {
        readonly int _value;
        public bool HasValue => _value is not 0;

        public OptionalBool(bool value) => _value = value ? 1 : -1;
        public static implicit operator OptionalBool(bool value) => new(value);
        public static implicit operator bool(OptionalBool value) => value.HasValue ? (value._value is 1) : throw new InvalidOperationException();
    }

    static class ZDOVarsEx
    {
        public static int HasFields { get; } = ZNetView.CustomFieldsStr.GetStableHashCode();

        static class _HasFields<T> where T : MonoBehaviour
        {
            public static int HasFields { get; } = $"{ZNetView.CustomFieldsStr}{typeof(T).Name}".GetStableHashCode();
        }

        public static int GetHasFields<T>() where T : MonoBehaviour => _HasFields<T>.HasFields;

        public static int TameableCommandable { get; } = $"{nameof(Tameable)}.{nameof(Tameable.m_commandable)}".GetStableHashCode();
        public static int TameableTamingTime { get; } = $"{nameof(Tameable)}.{nameof(Tameable.m_tamingTime)}".GetStableHashCode();
        public static int TameableTamingSpeedMultiplierRange { get; } = $"{nameof(Tameable)}.{nameof(Tameable.m_tamingSpeedMultiplierRange)}".GetStableHashCode();

        public static int FireplaceInfiniteFuel { get; } = $"{nameof(Fireplace)}.{nameof(Fireplace.m_infiniteFuel)}".GetStableHashCode();
        public static int FireplaceCanTurnOff { get; } = $"{nameof(Fireplace)}.{nameof(Fireplace.m_canTurnOff)}".GetStableHashCode();
        public static int FireplaceCanRefill { get; } = $"{nameof(Fireplace)}.{nameof(Fireplace.m_canRefill)}".GetStableHashCode();
        public static int FireplaceFuelPerSec { get; } = $"{nameof(Fireplace)}.{nameof(Fireplace.m_secPerFuel)}".GetStableHashCode();

        public static int ContainerWidth { get; } = $"{nameof(Container)}.{nameof(Container.m_width)}".GetStableHashCode();
        public static int ContainerHeight { get; } = $"{nameof(Container)}.{nameof(Container.m_height)}".GetStableHashCode();

        public static int SmelterMaxFuel { get; } = $"{nameof(Smelter)}.{nameof(Smelter.m_maxFuel)}".GetStableHashCode();
        public static int SmelterMaxOre { get; } = $"{nameof(Smelter)}.{nameof(Smelter.m_maxOre)}".GetStableHashCode();

        public static int WindmillMinWindSpeed { get; } = $"{nameof(Windmill)}.{nameof(Windmill.m_minWindSpeed)}".GetStableHashCode();
    }
}