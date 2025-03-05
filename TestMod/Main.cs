using BepInEx.Logging;
using BepInEx;
using System.Diagnostics;
using System.Collections.Concurrent;
using UnityEngine;
using System.Text.RegularExpressions;

namespace TestMod;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Main : BaseUnityPlugin
{
    const string PluginGuid = "argusmagnus.TestMod";
    const string PluginName = "TestMod";
    const string PluginVersion = "1.0.0";
    static int PluginGuidHash { get; } = PluginGuid.GetStableHashCode();

    const int MaxProcessingTimeMs = 50;

    //static Harmony HarmonyInstance { get; } = new Harmony(pluginGUID);
    static new ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource(PluginName);
    static readonly IReadOnlyList<string> __clockEmojis = ["🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠", "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"];
    static readonly Regex __clockRegex = new($@"(?:{string.Join("|", __clockEmojis.Select(Regex.Escape))})(?:\s*\d\d\:\d\d)?");

    static readonly HashSet<int> __fireplacePrefabs = new();
    static readonly IReadOnlyDictionary<int, Container> __containerPrefabs = new Dictionary<int, Container>();
    static readonly HashSet<int> __shipPrefabs = new();
    static readonly HashSet<int> __itemDropPrefabs = new();
    static readonly IReadOnlyDictionary<int, string> __pieceNames = new Dictionary<int, string>();
    static readonly ConcurrentHashSet<ZDOID> __ships = new();
    static readonly ConcurrentDictionary<ZDOID, uint> __dataRevisions = new();
    static readonly ConcurrentDictionary<string, ConcurrentDictionary<ZDOID, Inventory>> __containersByItemName = new();

    static ulong __executeCounter;
    static readonly HashSet<Vector2i> __playerSectors = new();
    static int __playerSectorsHash;
    static readonly List<ZDO> __currentZdos = new();

    record Pin(long OwnerId, string Tag, Vector3 Pos, Minimap.PinType Type, bool IsChecked, string Author);
    static readonly List<Pin> __pins = new();
    static int __pinsHash;

    static class Hashes
    {
        static readonly ConcurrentDictionary<string, int> __hashes = new();

        public static int Get(string key) => __hashes.GetOrAdd(key, static k => k.GetStableHashCode());
    }

    static class SignEx
    {
        public static int Prefab { get; } = Hashes.Get("sign");
    }

    static class MapTableEx
    {
        public static int Prefab { get; } = Hashes.Get("piece_cartographytable");
    }

    static class ZDOVarsEx
    {
        public static int HasFields { get; } = Hashes.Get(ZNetView.CustomFieldsStr);

        static class _HasFields<T> where T : MonoBehaviour
        {
            public static int HasFields { get; } = Hashes.Get($"{ZNetView.CustomFieldsStr}{typeof(T).Name}");
        }

        public static int GetHasFields<T>() where T : MonoBehaviour => _HasFields<T>.HasFields;

        public static int TameableCommandable { get; } = Hashes.Get($"{nameof(Tameable)}.{nameof(Tameable.m_commandable)}");

        public static int FireplaceInfiniteFuel { get; } = Hashes.Get($"{nameof(Fireplace)}.{nameof(Fireplace.m_infiniteFuel)}");
        public static int FireplaceCanTurnOff { get; } = Hashes.Get($"{nameof(Fireplace)}.{nameof(Fireplace.m_canTurnOff)}");
        public static int FireplaceCanRefill { get; } = Hashes.Get($"{nameof(Fireplace)}.{nameof(Fireplace.m_canRefill)}");
        public static int FireplaceFuelPerSec { get; } = Hashes.Get($"{nameof(Fireplace)}.{nameof(Fireplace.m_secPerFuel)}");

        public static int ContainerWidth { get; } = Hashes.Get($"{nameof(Container)}.{nameof(Container.m_width)}");
        public static int ContainerHeight { get; } = Hashes.Get($"{nameof(Container)}.{nameof(Container.m_height)}");
    }

    public void Awake()
    {
        Logger.LogInfo("Thank you for using my mod!");

        //Assembly assembly = Assembly.GetExecutingAssembly();
        //HarmonyInstance.PatchAll(assembly);

        //ItemManager.OnItemsRegistered += OnItemsRegistered;
        //PrefabManager.OnPrefabsRegistered += OnPrefabsRegistered;
    }

    public void Start()
    {
        Logger.LogInfo("Start called");
        InvokeRepeating(nameof(Execute), 10, 0.5f);
    }

    public void Execute()
    {
        if (ZNet.instance is null)
            return;

        if (ZNet.instance.IsServer() is false)
        {
            CancelInvoke(nameof(Execute));
            Logger.LogWarning("Mod should only be installed on the host");
            return;
        }

        if (ZNetScene.instance is null || ZDOMan.instance is null)
            return;

        var watch = Stopwatch.StartNew();

        if (__executeCounter++ is 0)
        {
            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                int hash = 0;
                int GetHash() => hash is 0 ? (hash = prefab.name.GetStableHashCode()) : hash;

                if (prefab.TryGetComponent<Fireplace>(out _))
                    __fireplacePrefabs.Add(GetHash());
                if (prefab.TryGetComponent<Container>(out var container))
                    ((IDictionary<int, Container>)__containerPrefabs).Add(GetHash(), container);
                if (prefab.TryGetComponent<Ship>(out _))
                    __shipPrefabs.Add(GetHash());
                if (prefab.TryGetComponent<Piece>(out var piece))
                    ((IDictionary<int, string>)__pieceNames).Add(GetHash(), piece.m_name);
                if (prefab.TryGetComponent<ItemDrop>(out _))
                    __itemDropPrefabs.Add(GetHash());
            }

            foreach (var zdo in ((IReadOnlyDictionary<ZDOID, ZDO>)typeof(ZDOMan)
                .GetField("m_objectsByID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(ZDOMan.instance)).Values.Where(x => __shipPrefabs.Contains(x.GetPrefab())))
                __ships.Add(zdo.m_uid);
        }
        else if (__executeCounter % 60 is 0)
        {
            foreach (var id in __dataRevisions.Keys)
            {
                if (ZDOMan.instance.GetZDO(id) is null)
                    __dataRevisions.TryRemove(id, out _);
            }

            foreach (var dict in __containersByItemName.Values)
            {
                foreach (var id in dict.Keys)
                {
                    if (ZDOMan.instance.GetZDO(id) is null)
                        dict.TryRemove(id, out _);
                }
            }
        }

        var peers = ZNet.instance.GetPeers();
        __playerSectors.Clear();
        (var oldPlayerSectorHash, __playerSectorsHash) = (__playerSectorsHash, 0);
        foreach (var sector in peers.Select(x => ZoneSystem.GetZone(x.m_refPos)))
        {
            if (__playerSectors.Add(sector))
                __playerSectorsHash = (__playerSectorsHash, sector).GetHashCode();
        }

        if (oldPlayerSectorHash != __playerSectorsHash)
            __currentZdos.Clear();

        if (__currentZdos is { Count: 0})
        {
            foreach (var sector in __playerSectors)
                ZDOMan.instance.FindSectorObjects(sector, 1, 0, __currentZdos);
        }

        string? timeText = null;
        List<Pin>? existingPins = null;
        byte[]? emptyExplored = null;
        __pins.Clear();
        int oldPinsHash = 0;

        while (__currentZdos is { Count: > 0 } && watch.ElapsedMilliseconds < MaxProcessingTimeMs)
        {
            var zdo = __currentZdos[__currentZdos.Count - 1];
            __currentZdos.RemoveAt(__currentZdos.Count - 1);

            if (zdo.GetPrefab() == SignEx.Prefab)
            {
                var text = zdo.GetString(ZDOVars.s_text);
                var newText = __clockRegex.Replace(text, match =>
                {
                    if (timeText is null)
                    {
                        var dayFraction = EnvMan.instance.GetDayFraction();
                        var emojiIdx = (int)Math.Floor(__clockEmojis.Count * 2 * dayFraction) % __clockEmojis.Count;
                        var time = TimeSpan.FromDays(dayFraction);
                        timeText = $@"{__clockEmojis[emojiIdx]} {time:hh\:mm}";
                    }
                    return timeText;
                });

                if (text == newText)
                    continue;

                Logger.LogDebug($"Changing sign text from '{text}' to '{newText}'");
                zdo.Set(ZDOVars.s_text, newText);
                //zdo.Set(ZDOVars.s_author, );
            }
            else if (zdo.GetPrefab() == MapTableEx.Prefab)
            {
                if (__pins is { Count: 0})
                {
                    foreach (var pin in ZDOMan.instance.GetPortals().Select(x => new Pin(PluginGuidHash, x.GetString(ZDOVars.s_tag), x.GetPosition(), Minimap.PinType.Icon4, false, PluginGuid))
                        .Concat(__ships
                            .Select(x =>
                            {
                                var y = ZDOMan.instance.GetZDO(x);
                                if (y is null)
                                    __ships.Remove(x);
                                return y;
                            })
                            .Where(x => x is not null)
                            .Select(x => new Pin(PluginGuidHash, __pieceNames.TryGetValue(x!.GetPrefab(), out var name) ? name : "", x.GetPosition(), Minimap.PinType.Player, false, PluginGuid))))
                    {
                        __pins.Add(pin);
                        oldPinsHash = (oldPinsHash, pin).GetHashCode();
                    }

                    (__pinsHash, oldPinsHash) = (oldPinsHash, __pinsHash);
                }

                if (__pinsHash == oldPinsHash && __dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
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

                pkg.Write(__pins.Count + (existingPins?.Count ?? 0));
                foreach (var pin in __pins.Concat(existingPins?.AsEnumerable() ?? []))
                {
                    pkg.Write(pin.OwnerId);
                    pkg.Write(pin.Tag);
                    pkg.Write(pin.Pos);
                    pkg.Write((int)pin.Type);
                    pkg.Write(pin.IsChecked);
                    pkg.Write(pin.Author);
                }

                zdo.Set(ZDOVars.s_data, Utils.Compress(pkg.GetArray()));
                __dataRevisions[zdo.m_uid] = zdo.DataRevision;

                ShowMessage(MessageHud.MessageType.TopLeft, "$msg_mapsaved");
            }

            if (__shipPrefabs.Contains(zdo.GetPrefab()))
                __ships.Add(zdo.m_uid);

            if (zdo.GetBool(ZDOVars.s_tamed))
            {
                if (__dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
                    continue;

                zdo.Set(ZDOVarsEx.HasFields, true);
                zdo.Set(ZDOVarsEx.GetHasFields<Tameable>(), true);
                zdo.Set(ZDOVarsEx.TameableCommandable, true);
                __dataRevisions[zdo.m_uid] = zdo.DataRevision;

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

            if (__fireplacePrefabs.Contains(zdo.GetPrefab()))
            {
                if (__dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
                    continue;

                zdo.Set(ZDOVarsEx.HasFields, true);
                zdo.Set(ZDOVarsEx.GetHasFields<Fireplace>(), true);
                // setting FireplaceInfiniteFuel to true works, but removes the turn on/off hover text (turning on/off still works)
                //zdo.Set(ZDOVarsEx.FireplaceInfiniteFuel, false);
                zdo.Set(ZDOVarsEx.FireplaceFuelPerSec, 0f);
                zdo.Set(ZDOVarsEx.FireplaceCanTurnOff, true);
                zdo.Set(ZDOVarsEx.FireplaceCanRefill, false);
                __dataRevisions[zdo.m_uid] = zdo.DataRevision;
            }

            if (__containerPrefabs.TryGetValue(zdo.GetPrefab(), out var container) && __pieceNames.TryGetValue(zdo.GetPrefab(), out var containerName))
            {
                if (__dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && zdo.DataRevision == dataRevision)
                    continue;

                if (zdo.GetBool(ZDOVars.s_inUse) || peers.Min(x => Utils.DistanceXZ(x.m_refPos, zdo.GetPosition())) < 5)
                    continue; // in use or player to close

                __dataRevisions[zdo.m_uid] = zdo.DataRevision;

                var data = zdo.GetString(ZDOVars.s_items);
                if (string.IsNullOrEmpty(data))
                    continue;

                /// <see cref="Container.Load"/>
                /// <see cref="Container.Save"/>
                var width = zdo.GetInt(ZDOVarsEx.ContainerWidth, container.m_width);
                var height = zdo.GetInt(ZDOVarsEx.ContainerHeight, container.m_height);
                Inventory inventory = new(container.m_name, container.m_bkg, width, height);
                inventory.Load(new(data));
                var changed = false;
                var x = 0;
                var y = 0;
                foreach (var item in inventory.GetAllItems()
                    .OrderBy(x => x.IsEquipable() ? 0 : 1)
                    .ThenBy(x => x.m_shared.m_name)
                    .ThenByDescending(x => x.m_stack))
                {
                    var dict = __containersByItemName.GetOrAdd(item.m_shared.m_name, static _ => new());
                    dict[zdo.m_uid] = inventory;
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
                    __dataRevisions.TryRemove(zdo.m_uid, out _);
                else
                {
                    var pkg = new ZPackage();
                    inventory.Save(pkg);
                    data = pkg.GetBase64();
                    zdo.Set(ZDOVars.s_items, data);
                    __dataRevisions[zdo.m_uid] = zdo.DataRevision;
                    ShowMessage(MessageHud.MessageType.TopLeft, $"{containerName} sorted");
                }
            }
            
            if (__itemDropPrefabs.Contains(zdo.GetPrefab()))
            {
                if (peers.Min(x => Utils.DistanceXZ(x.m_refPos, zdo.GetPosition())) < 10)
                    continue; // player to close

                var shared = ZNetScene.instance.GetPrefab(zdo.GetPrefab()).GetComponent<ItemDrop>().m_itemData.m_shared;
                if (!__containersByItemName.TryGetValue(shared.m_name, out var dict))
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

                    if (!__dataRevisions.TryGetValue(containerZdoId, out var containerDataRevision) || containerZdo.DataRevision != containerDataRevision)
                        continue; // inventory not up-to-date

                    if (Utils.DistanceXZ(zdo.GetPosition(), containerZdo.GetPosition()) > ZoneSystem.c_ZoneSize)
                        continue;

                    if (containerZdo.GetBool(ZDOVars.s_inUse) || peers.Min(x => Utils.DistanceXZ(x.m_refPos, containerZdo.GetPosition())) < 5)
                        continue; // in use or player to close

                    if (data is null)
                    {
                        data = new() { m_shared = shared };
                        PrivateAccessor.LoadFromZDO(data, zdo);
                    }

                    var stack = data.m_stack;
                    usedSlots ??= new();
                    usedSlots.Clear();

                    foreach (var slot in inventory.GetAllItems())
                    {
                        usedSlots.Add(new(slot.m_gridPos.x, slot.m_gridPos.y));
                        var maxAmount = slot.m_shared.m_maxStackSize - slot.m_stack;
                        if (slot.m_shared.m_name != shared.m_name || maxAmount <= 0 || slot.m_quality != data.m_quality || slot.m_variant != data.m_variant)
                            continue;

                        var amount = Math.Min(stack, maxAmount);
                        slot.m_stack += amount;
                        stack -= amount;
                        if (stack is 0)
                            break;
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
                                if (!usedSlots.Contains(new(x,y)))
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
                        __dataRevisions[containerZdo.m_uid] = containerZdo.DataRevision;
                        data.m_stack = stack;
                        zdo.SetOwner(ZDOMan.GetSessionID());
                        ItemDrop.SaveToZDO(data, zdo);
                        ShowMessage(MessageHud.MessageType.TopLeft, $"Dropped {shared.m_name} moved to {__pieceNames[containerZdo.GetPrefab()]}");
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
        }

        __currentZdos.Clear();

        Logger.Log(watch.ElapsedMilliseconds > MaxProcessingTimeMs ? LogLevel.Warning : LogLevel.Debug, $"{nameof(Execute)} took {watch.ElapsedMilliseconds} ms to process");
    }

    static void ShowMessage(MessageHud.MessageType type, string message)
    {
        /// Invoke <see cref="MessageHud.RPC_ShowMessage"/>
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", (int)type, message);
    }
}