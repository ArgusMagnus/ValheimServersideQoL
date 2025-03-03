using BepInEx.Logging;
using BepInEx;
using System.Diagnostics;
using System.Collections.Concurrent;
using UnityEngine;

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

    static readonly List<ZDO> __zdos = new();
    //static readonly Dictionary<ZDO, string?> __tameFollow = new();

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

        //var icons = Minimap.instance.m_locationIcons.Select(x => x.m_name).Concat(Minimap.instance.m_icons.Select(x => x.m_name.ToString()));

        var peers = ZNet.instance.GetPeers();
        var playerSectors = peers.Select(x => ZoneSystem.GetZone(x.m_refPos)).ToHashSet();

        __zdos.Clear();
        foreach (var sector in playerSectors)
            ZDOMan.instance.FindSectorObjects(sector, 1, 0, __zdos);

        string? newText = null;
        byte[]? mapData = null;
        foreach (var zdo in __zdos)
        {
            if (zdo.GetPrefab() == SignEx.Prefab)
            {
                var text = zdo.GetString(ZDOVars.s_text);
                if (!__clockEmojis.Any(text.StartsWith))
                    continue;

                if (newText is null)
                {
                    var dayFraction = EnvMan.instance.GetDayFraction();
                    var emojiIdx = (int)Math.Floor(__clockEmojis.Count * 2 * dayFraction) % __clockEmojis.Count;
                    var time = TimeSpan.FromDays(dayFraction);
                    newText = $@"{__clockEmojis[emojiIdx]} {time:hh\:mm}";
                }

                if (text == newText)
                    continue;

                Logger.LogDebug($"Changing sign text from '{text}' to '{newText}'");
                zdo.Set(ZDOVars.s_text, newText);
                //zdo.Set(ZDOVars.s_author, );
            }
            else if (zdo.GetPrefab() == MapTableEx.Prefab)
            {
                // not working yet
                if (mapData is null)
                {
                    //taken from Minimap.instance.GetSharedMapData
                    var pkg = new ZPackage();
                    pkg.Write(3);
                    pkg.Write(0);

                    var portals = ZDOMan.instance.GetPortals().Where(x => playerSectors.Contains(ZoneSystem.GetZone(x.GetPosition()))).ToList();
                    pkg.Write(portals.Count);
                    foreach (var portal in portals)
                    {
                        pkg.Write(0L);
                        pkg.Write(portal.GetString(ZDOVars.s_tag));
                        pkg.Write(portal.GetPosition());
                        pkg.Write((int)Minimap.PinType.Icon4);
                        pkg.Write(true);
                        pkg.Write("");
                    }
                    mapData = Utils.Compress(pkg.GetArray());
                }
                zdo.Set(ZDOVars.s_data, mapData);
            }
            else if (zdo.GetBool(ZDOVars.s_tamed))
            {
                zdo.Set(ZDOVarsEx.TameableCommandable, true);
                zdo.Set(ZDOVarsEx.GetHasFields<Tameable>(), true);
                zdo.Set(ZDOVarsEx.HasFields, true);

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
        }

        Logger.Log(watch.ElapsedMilliseconds > 50 ? LogLevel.Warning : LogLevel.Debug, $"{nameof(Execute)} took {watch.ElapsedMilliseconds} ms to process");

        __zdos.Clear();
    }
}