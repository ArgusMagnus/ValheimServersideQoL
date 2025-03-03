using BepInEx.Logging;
using BepInEx;
using System.Diagnostics;

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

    static readonly int __signPefab = "sign".GetStableHashCode();
    static readonly int __mapTablePrefab = "piece_cartographytable".GetStableHashCode();
    static readonly List<ZDO> __zdos = new();

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

        var playerSectors = ZNet.instance.GetPeers().Select(x => ZoneSystem.GetZone(x.m_refPos)).ToHashSet();

        __zdos.Clear();
        foreach (var sector in playerSectors)
            ZDOMan.instance.FindSectorObjects(sector, 1, 0, __zdos);

        string? newText = null;
        byte[]? mapData = null;
        foreach (var zdo in __zdos)
        {
            if (zdo.GetPrefab() == __signPefab)
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
            else if (zdo.GetPrefab() == __mapTablePrefab)
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
                zdo.Set($"{nameof(Tameable)}.{nameof(Tameable.m_commandable)}".GetStableHashCode(), true);
                zdo.Set($"HasFields{nameof(Tameable)}".GetStableHashCode(), true);
                zdo.Set("HasFields".GetStableHashCode(), true);
            }
        }

        Logger.Log(watch.ElapsedMilliseconds > 50 ? LogLevel.Warning : LogLevel.Debug, $"{nameof(Execute)} took {watch.ElapsedMilliseconds} ms to process");

        __zdos.Clear();
    }
}