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

    static ulong __executeCounter;
    static readonly List<ZDO> __zdos = new();
    static int __zdoIdx;
    static HashSet<int>? __fireplacePrefabs;
    static IReadOnlyDictionary<int, Container>? __containerPrefabs;
    static HashSet<int>? __shipPrefabs;
    static IReadOnlyDictionary<int, string>? __pieceNames;
    static HashSet<ZDOID>? __ships;
    static readonly Dictionary<ZDOID, uint> __dataRevisions = new();

    record Pin(long OwnerId, string Tag, Vector3 Pos, Minimap.PinType Type, bool IsChecked, string Author);
    static IReadOnlyList<Pin> __pins = [];

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
        public static int HasFields { get; } = Hashes.Get("HasFields");

        static class _HasFields<T> where T : MonoBehaviour
        {
            public static int HasFields { get; } = Hashes.Get($"HasFields{typeof(T).Name}");
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
        InvokeRepeating(nameof(Execute), 10, 1);
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

        __executeCounter++;
        var watch = Stopwatch.StartNew();

        __fireplacePrefabs ??= ZNetScene.instance.m_prefabs.Where(x => x.TryGetComponent<Fireplace>(out _)).Select(x => x.name.GetStableHashCode()).ToHashSet();
        __containerPrefabs ??= ZNetScene.instance.m_prefabs.Select(x => (Prefab: x.name, Container: x.GetComponent<Container>()))
            .Where(x => x.Container is not null)
            .ToDictionary(x => x.Prefab.GetStableHashCode(), x => x.Container);
        __shipPrefabs ??= ZNetScene.instance.m_prefabs.Where(x => x.TryGetComponent<Ship>(out _)).Select(x => x.name.GetStableHashCode()).ToHashSet();
        __ships ??= ((IReadOnlyDictionary<ZDOID, ZDO>)typeof(ZDOMan).GetField("m_objectsByID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(ZDOMan.instance)).Values.Where(x => __shipPrefabs.Contains(x.GetPrefab()))
            .Select(x => x.m_uid)
            .ToHashSet();
        __pieceNames ??= ZNetScene.instance.m_prefabs.Select(x => (Prefab: x.name, Piece: x.GetComponent<Piece>()))
            .Where(x => x.Piece is not null)
            .ToDictionary(x => x.Prefab.GetStableHashCode(), x => x.Piece.m_name);

        //var icons = Minimap.instance.m_locationIcons.Select(x => x.m_name).Concat(Minimap.instance.m_icons.Select(x => x.m_name.ToString()));

        if (__executeCounter % 60 is 0)
        {
            List<ZDOID>? remove = null;
            foreach (var id in __dataRevisions.Keys)
            {
                if (ZDOMan.instance.GetZDO(id) is null)
                    (remove ??= []).Add(id);
            }
            if (remove is { Count: > 0})
            {
                foreach (var id in remove)
                    __dataRevisions.Remove(id);
            }
        }

        var peers = ZNet.instance.GetPeers();
        var playerSectors = peers.Select(x => ZoneSystem.GetZone(x.m_refPos)).ToHashSet();

        __zdos.Clear();
        foreach (var sector in playerSectors)
            ZDOMan.instance.FindSectorObjects(sector, 1, 0, __zdos);

        string? timeText = null;
        List<Pin>? pins = null;
        List<ZDOID>? invalidShips = null;
        List<Pin>? existingPins = null;
        byte[]? emptyExplored = null;

        for (int idx = 0; idx < __zdos.Count && watch.ElapsedMilliseconds < MaxProcessingTimeMs; ++idx, ++__zdoIdx)
        {
            if (__zdoIdx >= __zdos.Count)
                __zdoIdx = 0;
            var zdo = __zdos[__zdoIdx];

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
                if (pins is null)
                {
                    pins = ZDOMan.instance.GetPortals().Select(x => new Pin(PluginGuidHash, x.GetString(ZDOVars.s_tag), x.GetPosition(), Minimap.PinType.Icon4, false, PluginGuid))
                        .Concat(__ships
                            .Select(x =>
                            {
                                var y = ZDOMan.instance.GetZDO(x);
                                if (y is null)
                                    (invalidShips ??= []).Add(x);
                                return y;
                            })
                            .Where(x => x is not null)
                            .Select(x => new Pin(PluginGuidHash, __pieceNames.TryGetValue(x!.GetPrefab(), out var name) ? name : "", x.GetPosition(), Minimap.PinType.Player, false, PluginGuid)))
                        .OrderBy(x => x.Pos.x).ThenBy(x => x.Pos.z)
                        .ToList();

                    if (!pins.SequenceEqual(__pins))
                        __pins = pins;
                }

                byte[]? data = null;
                if (ReferenceEquals(pins, __pins) || (data = zdo.GetByteArray(ZDOVars.s_data)) is null)
                {
                    existingPins?.Clear();
                    ZPackage pkg;
                    data = null;
                    //data ??= zdo.GetByteArray(ZDOVars.s_data);
                    //if (data is not null)
                    //{
                    //    data = Utils.Decompress(data);
                    //    pkg = new ZPackage(data);
                    //    var version = pkg.ReadInt();
                    //    if (version is not 3)
                    //    {
                    //        Logger.LogWarning($"MapTable data version {version} is not supported");
                    //        continue;
                    //    }
                    //    data = pkg.ReadByteArray();
                    //    if (data.Length != Minimap.instance.m_textureSize * Minimap.instance.m_textureSize)
                    //    {
                    //        Logger.LogWarning("Invalid explored map data length");
                    //        data = null;
                    //    }

                    //    var pinCount = pkg.ReadInt();
                    //    existingPins ??= new(pinCount);
                    //    if (existingPins.Capacity < pinCount)
                    //        existingPins.Capacity = pinCount;

                    //    foreach (var i in Enumerable.Range(0, pinCount))
                    //    {
                    //        try
                    //        {
                    //            var ownerId = pkg.ReadLong();
                    //            if (ownerId != PluginGuidHash)
                    //                existingPins.Add(new(ownerId,
                    //                    pkg.ReadString(),
                    //                    pkg.ReadVector3(),
                    //                    (Minimap.PinType)pkg.ReadInt(),
                    //                    pkg.ReadBool(),
                    //                    pkg.ReadString()));
                    //        }
                    //        catch (EndOfStreamException ex)
                    //        {
                    //            data = null;
                    //            Logger.LogError($"Error reading pin {i} of {pinCount}: {ex}");
                    //            break;
                    //        }
                    //    }
                    //}

                    /// taken from <see cref="Minimap.GetSharedMapData"/> and <see cref="MapTable.GetMapData"/> 
                    pkg = new ZPackage();
                    pkg.Write(3);

                    pkg.Write(data ?? (emptyExplored ??= new byte[Minimap.instance.m_textureSize * Minimap.instance.m_textureSize]));

                    pkg.Write(pins.Count + (existingPins?.Count ?? 0));
                    foreach (var pin in pins.Concat(existingPins?.AsEnumerable() ?? []))
                    {
                        pkg.Write(pin.OwnerId);
                        pkg.Write(pin.Tag);
                        pkg.Write(pin.Pos);
                        pkg.Write((int)pin.Type);
                        pkg.Write(pin.IsChecked);
                        pkg.Write(pin.Author);
                    }

                    zdo.Set(ZDOVars.s_data, Utils.Compress(pkg.GetArray()));

                    ShowMessage(MessageHud.MessageType.TopLeft, "$piece_cartographytable updated");
                }
            }
            else if (__shipPrefabs.Contains(zdo.GetPrefab()))
            {
                __ships.Add(zdo.m_uid);
            }
            else if (zdo.GetBool(ZDOVars.s_tamed))
            {
                zdo.Set(ZDOVarsEx.TameableCommandable, true);
                zdo.Set(ZDOVarsEx.GetHasFields<Tameable>(), true);
                zdo.Set(ZDOVarsEx.HasFields, true);

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
            else if (__fireplacePrefabs.Contains(zdo.GetPrefab()))
            {
                // setting FireplaceInfiniteFuel to true works, but removes the turn on/off hover text (turning on/off still works)
                //zdo.Set(ZDOVarsEx.FireplaceInfiniteFuel, true);
                zdo.Set(ZDOVarsEx.FireplaceFuelPerSec, 0f);
                zdo.Set(ZDOVarsEx.FireplaceCanTurnOff, true);
                zdo.Set(ZDOVarsEx.FireplaceCanRefill, false);
                zdo.Set(ZDOVarsEx.GetHasFields<Fireplace>(), true);
                zdo.Set(ZDOVarsEx.HasFields, true);
            }
            else if (__containerPrefabs.TryGetValue(zdo.GetPrefab(), out var container))
            {
                if (__dataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && zdo.DataRevision == dataRevision)
                    continue;

                if (zdo.GetBool(ZDOVars.s_inUse) || peers.Min(x => Utils.DistanceXZ(x.m_refPos, zdo.GetPosition())) < 5)
                    continue;

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
                    __dataRevisions.Remove(zdo.m_uid);
                else
                {
                    var pkg = new ZPackage();
                    inventory.Save(pkg);
                    data = pkg.GetBase64();
                    zdo.Set(ZDOVars.s_items, data);
                    ShowMessage(MessageHud.MessageType.TopLeft, $"{(__pieceNames.TryGetValue(zdo.GetPrefab(), out var name) ? name : "Container")} sorted");
                }
            }
        }

        if (invalidShips is { Count: > 0})
        {
            foreach (var x in invalidShips)
                __ships.Remove(x);
        }

        __zdos.Clear();

        Logger.Log(watch.ElapsedMilliseconds > MaxProcessingTimeMs ? LogLevel.Warning : LogLevel.Debug, $"{nameof(Execute)} took {watch.ElapsedMilliseconds} ms to process");
    }

    static void ShowMessage(MessageHud.MessageType type, string message)
    {
        /// Invoke <see cref="MessageHud.RPC_ShowMessage"/>
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", (int)type, message);
    }
}