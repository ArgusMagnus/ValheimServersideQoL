using BepInEx.Logging;
using BepInEx;
using System.Diagnostics;
using System.Collections.Concurrent;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;

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

    static readonly List<ZDO> __zdos = new();
    static int __zdoIdx;
    static HashSet<int>? __fireplacePrefabs;
    static HashSet<int>? __containerPrefabs;
    static HashSet<int>? __shipPrefabs;
    static HashSet<ZDOID>? __ships;

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

        var watch = Stopwatch.StartNew();

        __fireplacePrefabs ??= ZNetScene.instance.m_prefabs.Where(x => x.TryGetComponent<Fireplace>(out _)).Select(x => x.name.GetStableHashCode()).ToHashSet();
        __containerPrefabs ??= ZNetScene.instance.m_prefabs.Where(x => x.TryGetComponent<Container>(out _)).Select(x => x.name.GetStableHashCode()).ToHashSet();
        __shipPrefabs ??= ZNetScene.instance.m_prefabs.Where(x => x.TryGetComponent<Ship>(out _)).Select(x => x.name.GetStableHashCode()).ToHashSet();
        __ships ??= ((IReadOnlyDictionary<ZDOID, ZDO>)typeof(ZDOMan).GetField("m_objectsByID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(ZDOMan.instance)).Values.Where(x => __shipPrefabs.Contains(x.GetPrefab()))
            .Select(x => x.m_uid)
            .ToHashSet();

        //var icons = Minimap.instance.m_locationIcons.Select(x => x.m_name).Concat(Minimap.instance.m_icons.Select(x => x.m_name.ToString()));

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
                            .Select(x => new Pin(PluginGuidHash, $"${ZNetScene.instance.GetPrefab(x.GetPrefab())?.name}", x.GetPosition(), Minimap.PinType.Player, false, PluginGuid)))
                        .OrderBy(x => x.Pos.x).ThenBy(x => x.Pos.z)
                        .ToList();

                    if (!pins.SequenceEqual(__pins))
                        __pins = pins;
                }

                if (ReferenceEquals(pins, __pins) || zdo.GetByteArray(ZDOVars.s_data) is null)
                {
                    existingPins?.Clear();
                    ZPackage pkg;
                    byte[]? data = null; //zdo.GetByteArray(ZDOVars.s_data);
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
                    //        }
                    //    }
                    //}

                    /// taken from <see cref="Minimap.GetSharedMapData"/> and <see cref="MapTable.GetMapData"/> 
                    pkg = new ZPackage();
                    pkg.Write(3);

                    pkg.Write(data ?? (emptyExplored ??= new byte[Minimap.instance.m_textureSize * Minimap.instance.m_textureSize]));

                    int pinCount = pins.Count + (existingPins?.Count ?? 0);
                    pkg.Write(pinCount);
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

                    Logger.LogInfo($"{pinCount} pins written to map table {zdo.m_uid}");

                    /// Call <see cref="Chat.RPC_ChatMessage"/>
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", zdo.GetPosition(), (int)Talker.Type.Shout, new UserInfo { Gamertag = "Server", Name = "Server", NetworkUserId = PrivilegeManager.GetNetworkUserId() }, "MapTable updated", PrivilegeManager.GetNetworkUserId());
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
            else if (__containerPrefabs.Contains(zdo.GetPrefab()))
            {
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
}