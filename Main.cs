using BepInEx.Logging;
using BepInEx;
using System.Diagnostics;
using System.Collections.Concurrent;
using UnityEngine;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using System.Reflection;

namespace Valheim.ServersideQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Main : BaseUnityPlugin
{
    /// <Ideas>
    /// - Make tames lay eggs (by replacing spawned offspring with eggs and setting <see cref="EggGrow.m_grownPrefab"/>
    /// - Option to make fireplaces consume fuel from containers to have an alternative to infinite fuel when making them toggleable
    /// - Scale eggs by quality by setting <see cref="ItemDrop.ItemData.SharedData.m_scaleByQuality". Not sure if we can modify shared data on clients though. />
    /// - Show taming progress to nearby players via messages (<see cref="Tameable.GetTameness"/>
    /// </summary>

    const string PluginGuid = "argusmagnus.TestMod";
    const string PluginName = "TestMod";
    const string PluginVersion = "0.1.0";
    static int PluginGuidHash { get; } = PluginGuid.GetStableHashCode();

    //static Harmony HarmonyInstance { get; } = new Harmony(pluginGUID);
    static new ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource(PluginName);

    readonly ModConfig _cfg;

    internal static IReadOnlyList<string> ClockEmojis { get; } = ["🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠", "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"];
    readonly Regex _clockRegex = new($@"(?:{string.Join("|", ClockEmojis.Select(Regex.Escape))})(?:\s*\d\d\:\d\d)?");

    record PrefabInfo(IReadOnlyDictionary<Type, Component> Prefabs)
    {
        static T? Get<T>(IReadOnlyDictionary<Type, Component> prefabs) where T : Component => prefabs.TryGetValue(typeof(T), out var value) ? (T)value : null;
        public Sign? Sign { get; } = Get<Sign>(Prefabs);
        public MapTable? MapTable { get; } = Get<MapTable>(Prefabs);
        public Tameable? Tameable { get; } = Get<Tameable>(Prefabs);
        public Fireplace? Fireplace { get; } = Get<Fireplace>(Prefabs);
        public Container? Container { get; } = Get<Container>(Prefabs);
        public Ship? Ship { get; } = Get<Ship>(Prefabs);
        public ItemDrop? ItemDrop { get; } = Get<ItemDrop>(Prefabs);
        public Piece? Piece { get; } = Get<Piece>(Prefabs);
        public Smelter? Smelter { get; } = Get<Smelter>(Prefabs);
    }

    readonly IReadOnlyDictionary<int, PrefabInfo> _prefabInfo = new Dictionary<int, PrefabInfo>();
    readonly ConcurrentHashSet<ZDOID> _ships = new();
    readonly ConcurrentDictionary<ZDOID, uint> _dataRevisions = new();
    readonly ConcurrentDictionary<string, ConcurrentDictionary<ZDOID, Inventory>> _containersByItemName = new();

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

    public void Start()
    {
        if (!_cfg.General.Enabled.Value)
            return;

        StartCoroutine(CallExecute());

        IEnumerator<YieldInstruction> CallExecute()
        {
            yield return new WaitForSeconds(_cfg.General.StartDelay.Value);
            while (true)
            {
                Execute();
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

        var watch = Stopwatch.StartNew();

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
                RequiredPrefabsAttribute? classAttr = null;
                foreach (var keyProperty in sectionProperty.PropertyType.GetProperties())
                {
                    var attr = keyProperty.GetCustomAttribute<RequiredPrefabsAttribute>();
                    if (keyProperty.PropertyType != typeof(ConfigEntry<bool>))
                    {
                        if (attr is not null)
                            throw new Exception($"{nameof(RequiredPrefabsAttribute)} only supported on classes and properties of type {nameof(ConfigEntry<bool>)}");
                        continue;
                    }

                    section ??= sectionProperty.GetValue(_cfg);

                    if (!((ConfigEntry<bool>)keyProperty.GetValue(section)).Value)
                        continue;

                    classAttr ??= sectionProperty.PropertyType.GetCustomAttribute<RequiredPrefabsAttribute>();

                    if (attr is null)
                        continue;

                    var types = attr.Prefabs.ToHashSet();
                    if (!requiredTypes.Any(x => x.SequenceEqual(types)))
                        requiredTypes.Add(types);
                }

                if (classAttr?.Prefabs.ToHashSet() is { } classTypes && !requiredTypes.Any(x => x.SequenceEqual(classTypes)))
                    requiredTypes.Add(classTypes);
            }

            if (requiredTypes is { Count: > 0 })
            {
                var needsShips = false;
                foreach (var prefab in ZNetScene.instance.m_prefabs)
                {
                    Dictionary<Type, Component>? prefabDict = null;
                    foreach (var requiredTypeList in requiredTypes)
                    {
                        var prefabs = requiredTypeList.Select(x => (Type: x, Component: prefab.GetComponent(x))).Where(x => x.Component is not null).ToList();
                        if (prefabs.Count != requiredTypeList.Count)
                            continue;
                        foreach (var (type, component) in prefabs)
                        {
                            prefabDict ??= new();
                            if (!prefabDict.ContainsKey(type))
                                prefabDict.Add(type, component);
                        }
                    }
                    if (prefabDict is not null)
                    {
                        var prefabInfo = new PrefabInfo(prefabDict);
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
            if (watch.ElapsedMilliseconds > _cfg.General.MaxProcessingTime.Value)
                break;

            processedSectors++;

            if (sectorInfo is { ZDOs: { Count: 0 } })
                ZDOMan.instance.FindSectorObjects(sector, 1, 0, sectorInfo.ZDOs);

            totalZdos += sectorInfo.ZDOs.Count;

            while (sectorInfo is { ZDOs: { Count: > 0 } } && watch.ElapsedMilliseconds < _cfg.General.MaxProcessingTime.Value)
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

                if (prefabInfo.Tameable is not null && _cfg.Tames.MakeCommandable.Value && !prefabInfo.Tameable.m_commandable && zdo.GetBool(ZDOVars.s_tamed))
                {
                    if (_dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
                        continue;

                    zdo.Set(ZDOVarsEx.HasFields, true);
                    zdo.Set(ZDOVarsEx.GetHasFields<Tameable>(), true);
                    zdo.Set(ZDOVarsEx.TameableCommandable, true);
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
                    // todo: ignore non-player-built chests (such as TreasureChest_*)
                    if (_dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && zdo.DataRevision == dataRevision)
                        continue;

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
                        var dict = _containersByItemName.GetOrAdd(item.m_shared.m_name, static _ => new());
                        dict[zdo.m_uid] = inventory;
                        if (!_cfg.Containers.AutoSort.Value)
                            continue;

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

                    if (zdo.GetBool(ZDOVars.s_inUse))
                        _dataRevisions.TryRemove(zdo.m_uid, out _);
                    else
                    {
                        var pkg = new ZPackage();
                        inventory.Save(pkg);
                        data = pkg.GetBase64();
                        zdo.Set(ZDOVars.s_items, data);
                        _dataRevisions[zdo.m_uid] = zdo.DataRevision;
                        ShowMessage(sectorInfo.Peers, MessageHud.MessageType.TopLeft, $"{prefabInfo.Piece.m_name} sorted");
                    }
                }

                if (prefabInfo.ItemDrop is not null && _cfg.Containers.AutoPickup.Value)
                {
                    if (!CheckMinDistance(peers, zdo, _cfg.Containers.AutoPickupMinPlayerDistance.Value))
                        continue; // player to close

                    var shared = ZNetScene.instance.GetPrefab(zdo.GetPrefab()).GetComponent<ItemDrop>().m_itemData.m_shared;
                    if (!_containersByItemName.TryGetValue(shared.m_name, out var dict))
                        continue;

                    ItemDrop.ItemData? data = null;
                    HashSet<Vector2i>? usedSlots = null;

                    foreach (var (containerZdoId, inventory) in dict.Select(x => (x.Key, x.Value)))
                    {
                        if (ZDOMan.instance.GetZDO(containerZdoId) is not { } containerZdo)
                        {
                            dict.TryRemove(containerZdoId, out _);
                            continue;
                        }

                        if (!_dataRevisions.TryGetValue(containerZdoId, out var containerDataRevision) || containerZdo.DataRevision != containerDataRevision)
                            continue;

                        if (Utils.DistanceXZ(zdo.GetPosition(), containerZdo.GetPosition()) > _cfg.Containers.AutoPickupRange.Value)
                            continue;

                        if (containerZdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, containerZdo))
                            continue; // in use or player to close

                        inventory.Update(containerZdo);

                        if (data is null)
                        {
                            data = new() { m_shared = shared };
                            PrivateAccessor.LoadFromZDO(data, zdo);
                        }

                        var stack = data.m_stack;
                        usedSlots ??= new();
                        usedSlots.Clear();
                        bool found = false;

                        foreach (var slot in inventory.GetAllItems())
                        {
                            usedSlots.Add(new(slot.m_gridPos.x, slot.m_gridPos.y));
                            var maxAmount = slot.m_shared.m_maxStackSize - slot.m_stack;
                            if (slot.m_shared.m_name != shared.m_name || maxAmount <= 0 || slot.m_quality != data.m_quality || slot.m_variant != data.m_variant)
                                continue;

                            found = true;
                            var amount = Math.Min(stack, maxAmount);
                            slot.m_stack += amount;
                            stack -= amount;
                            if (stack is 0)
                                break;
                        }

                        if (!found)
                        {
                            dict.TryRemove(containerZdoId, out _);
                            if (dict is { Count: 0 })
                                _containersByItemName.TryRemove(shared.m_name, out _);
                            continue;
                        }

                        if (!ReferenceEquals(inventory.GetAllItems(), inventory.GetAllItems()))
                            throw new Exception("Algorithm assumption violated");

                        for (var emptySlots = inventory.GetEmptySlots(); stack > 0 && emptySlots > 0; emptySlots--)
                        {
                            var amount = Math.Min(stack, shared.m_maxStackSize);

                            var slot = data.Clone();
                            slot.m_stack = amount;
                            for (int x = 0; x < inventory.GetWidth(); x++)
                            {
                                for (int y = 0; y < inventory.GetHeight(); y++)
                                {
                                    if (!usedSlots.Contains(new(x, y)))
                                    {
                                        (slot.m_gridPos.x, slot.m_gridPos.y) = (x, y);
                                        break;
                                    }
                                }
                            }
                            inventory.GetAllItems().Add(slot);
                            stack -= amount;
                        }

                        if (stack != data.m_stack && !containerZdo.GetBool(ZDOVars.s_inUse))
                        {
                            var pkg = new ZPackage();
                            inventory.Save(pkg);
                            containerZdo.Set(ZDOVars.s_items, pkg.GetBase64());
                            _dataRevisions[containerZdo.m_uid] = containerZdo.DataRevision;
                            data.m_stack = stack;
                            zdo.SetOwner(ZDOMan.GetSessionID());
                            ItemDrop.SaveToZDO(data, zdo);
                            ShowMessage(sectorInfo.Peers, MessageHud.MessageType.TopLeft, $"Dropped {shared.m_name} moved to {_prefabInfo[containerZdo.GetPrefab()].Piece!.m_name}");
                        }

                        if (data.m_stack is 0)
                            break;
                    }

                    if (data?.m_stack is 0)
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
                            var fuelItem = prefabInfo.Smelter.m_fuelItem.m_itemData.m_shared.m_name;
                            var addedFuel = 0;
                            if (_containersByItemName.TryGetValue(fuelItem, out var containers))
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
                                    foreach (var slot in inventory.GetAllItems().Where(x => x.m_shared.m_name == fuelItem).OrderBy(x => x.m_stack))
                                    {
                                        var take = Math.Min(maxFuelAdd, slot.m_stack);
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
                                            _containersByItemName.TryRemove(fuelItem, out _);
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
                                                _containersByItemName.TryRemove(fuelItem, out _);
                                            continue;
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
                                ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{prefabInfo.Piece?.m_name ?? prefabInfo.Smelter.m_name} $msg_added {addedFuel} {fuelItem}");
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
                                var oreItem = conversion.m_from.m_itemData.m_shared.m_name;
                                var addedOre = 0;
                                if (_containersByItemName.TryGetValue(oreItem, out var containers))
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
                                        foreach (var slot in inventory.GetAllItems().Where(x => x.m_shared.m_name == oreItem).OrderBy(x => x.m_stack))
                                        {
                                            var take = Math.Min(maxOreAdd, slot.m_stack);
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
                                                _containersByItemName.TryRemove(oreItem, out _);
                                            continue;
                                        }

                                        if (removeSlots is { Count: > 0 })
                                        {
                                            if (!ReferenceEquals(inventory.GetAllItems(), inventory.GetAllItems()))
                                                throw new Exception("Algorithm assumption violated");
                                            foreach (var remove in removeSlots)
                                                inventory.GetAllItems().Remove(remove);

                                            if (inventory.GetAllItems() is { Count: 0})
                                            {
                                                containers.TryRemove(containerZdoId, out _);
                                                if (containers is { Count: 0 })
                                                    _containersByItemName.TryRemove(oreItem, out _);
                                                continue;
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
                                    ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{prefabInfo.Piece?.m_name ?? prefabInfo.Smelter.m_name} $msg_added {addedOre} {oreItem}");
                            }
                        }
                    }

                }
            }
        }

        Logger.Log(watch.ElapsedMilliseconds > _cfg.General.MaxProcessingTime.Value ? LogLevel.Info : LogLevel.Debug,
            $"{nameof(Execute)} took {watch.ElapsedMilliseconds} ms to process {processedZdos} of {totalZdos} ZDOs in {processedSectors} of {_playerSectors.Count} zones");

        if (processedSectors < _playerSectors.Count || processedZdos < totalZdos)
            _unfinishedProcessingInRow++;
        else
            _unfinishedProcessingInRow = 0;
    }

    static void ShowMessage(IEnumerable<ZNetPeer> peers, MessageHud.MessageType type, string message)
    {
        /// Invoke <see cref="MessageHud.RPC_ShowMessage"/>
        foreach (var peer in peers)
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "ShowMessage", (int)type, message);
    }

    bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo)
        => CheckMinDistance(peers, zdo, _cfg.General.MinPlayerDistance.Value);

    bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo, float minDistance)
        => peers.Min(x => Utils.DistanceSqr(x.m_refPos, zdo.GetPosition())) >= minDistance;

    static string ConvertToRegexPattern(string searchPattern)
    {
        searchPattern = Regex.Escape(searchPattern);
        searchPattern = searchPattern.Replace("\\*", ".*").Replace("\\?", ".?");
        return $"(?i)^{searchPattern}$";
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

        public static int FireplaceInfiniteFuel { get; } = $"{nameof(Fireplace)}.{nameof(Fireplace.m_infiniteFuel)}".GetStableHashCode();
        public static int FireplaceCanTurnOff { get; } = $"{nameof(Fireplace)}.{nameof(Fireplace.m_canTurnOff)}".GetStableHashCode();
        public static int FireplaceCanRefill { get; } = $"{nameof(Fireplace)}.{nameof(Fireplace.m_canRefill)}".GetStableHashCode();
        public static int FireplaceFuelPerSec { get; } = $"{nameof(Fireplace)}.{nameof(Fireplace.m_secPerFuel)}".GetStableHashCode();

        public static int ContainerWidth { get; } = $"{nameof(Container)}.{nameof(Container.m_width)}".GetStableHashCode();
        public static int ContainerHeight { get; } = $"{nameof(Container)}.{nameof(Container.m_height)}".GetStableHashCode();

        public static int SmelterMaxFuel { get; } = $"{nameof(Smelter)}.{nameof(Smelter.m_maxFuel)}".GetStableHashCode();
        public static int SmelterMaxOre { get; } = $"{nameof(Smelter)}.{nameof(Smelter.m_maxOre)}".GetStableHashCode();
    }
}