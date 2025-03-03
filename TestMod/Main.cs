using BepInEx.Logging;
using BepInEx;
using System.Diagnostics;
using System.Collections.Concurrent;
using UnityEngine;
using System.Text.RegularExpressions;

namespace TestMod;

[BepInPlugin(pluginGUID, pluginName, pluginVersion)]
public class Main : BaseUnityPlugin
{
    const string pluginGUID = "argusmagnus.TestMod";
    const string pluginName = "TestMod";
    const string pluginVersion = "1.0.0";

    //static Harmony HarmonyInstance { get; } = new Harmony(pluginGUID);
    static new ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource(pluginName);
    static readonly IReadOnlyList<string> __clockEmojis = ["🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠", "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"];
    static readonly Regex __clockRegex = new($@"(?:{string.Join("|", __clockEmojis.Select(Regex.Escape))})(?:\s*\d\d\:\d\d)?");

    static readonly List<ZDO> __zdos = new();
    static HashSet<int>? __fireplacePrefabs;
    static HashSet<int>? __containerPrefabs;
    static IReadOnlyList<(string Tag, Vector3 Pos)> __portals = [];

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

        //var icons = Minimap.instance.m_locationIcons.Select(x => x.m_name).Concat(Minimap.instance.m_icons.Select(x => x.m_name.ToString()));

        var peers = ZNet.instance.GetPeers();
        var playerSectors = peers.Select(x => ZoneSystem.GetZone(x.m_refPos)).ToHashSet();

        __zdos.Clear();
        foreach (var sector in playerSectors)
            ZDOMan.instance.FindSectorObjects(sector, 1, 0, __zdos);

        string? timeText = null;
        byte[]? mapData = null;
        foreach (var zdo in __zdos)
        {
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
                if (mapData is null)
                {
                    var portals = ZDOMan.instance.GetPortals().Select(x => (Tag: x.GetString(ZDOVars.s_tag), Pos: x.GetPosition())).OrderBy(x => x.Tag).ToList();
                    if (portals.SequenceEqual(__portals))
                    {
                        mapData = [];
                        continue;
                    }
                    __portals = portals;

                    /// taken from <see cref="Minimap.GetSharedMapData"/> and <see cref="MapTable.GetMapData"/> 
                    var pkg = new ZPackage();
                    pkg.Write(3);

                    pkg.Write(new byte[Minimap.instance.m_textureSize * Minimap.instance.m_textureSize]);

                    pkg.Write(portals.Count);
                    foreach (var portal in portals)
                    {
                        pkg.Write(1L); // dummy ownerId
                        pkg.Write(portal.Tag);
                        pkg.Write(portal.Pos);
                        pkg.Write((int)Minimap.PinType.Icon4);
                        pkg.Write(false); // isChecked
                        pkg.Write("");
                    }

                    mapData = Utils.Compress(pkg.GetArray());
                }

                if (mapData is { Length: > 0 })
                {
                    zdo.Set(ZDOVars.s_data, mapData);
                    /// Call <see cref="Chat.RPC_ChatMessage"/>
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", zdo.GetPosition(), (int)Talker.Type.Shout, new UserInfo { Gamertag = "Server", Name = "Server", NetworkUserId = PrivilegeManager.GetNetworkUserId() }, "MapTable updated", PrivilegeManager.GetNetworkUserId());
                }
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

        __zdos.Clear();

        Logger.Log(watch.ElapsedMilliseconds > 50 ? LogLevel.Warning : LogLevel.Debug, $"{nameof(Execute)} took {watch.ElapsedMilliseconds} ms to process");
    }
}