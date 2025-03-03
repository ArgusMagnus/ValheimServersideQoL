using BepInEx.Logging;
using BepInEx;
using System.Linq.Expressions;
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

    static Func<ZDOMan, IReadOnlyList<ZDO>[]> GetObjectsBySector { get; } = Expression.Lambda<Func<ZDOMan, IReadOnlyList<ZDO>[]>>(
        Expression.Field(
            Expression.Parameter(typeof(ZDOMan)) is { } par ? par : throw new Exception(),
            typeof(ZDOMan).GetField("m_objectsBySector", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)),
        par).Compile();

    static Func<ZDOMan, IReadOnlyDictionary<Vector2i, List<ZDO>>> GetObjectsByOutsideSector { get; } = Expression.Lambda<Func<ZDOMan, IReadOnlyDictionary<Vector2i, List<ZDO>>>>(
        Expression.Field(
            Expression.Parameter(typeof(ZDOMan)) is { } par ? par : throw new Exception(),
            typeof(ZDOMan).GetField("m_objectsByOutsideSector", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)),
        par).Compile();

    static readonly int __signPefab = "sign".GetStableHashCode();

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

        string? newText = null;

        foreach (var zdo in GetObjectsBySector(ZDOMan.instance).Where(x => x is not null).SelectMany(x => x)
            .Concat(GetObjectsByOutsideSector(ZDOMan.instance).Values.SelectMany(x => x).Where(x => x.IsValid())))
        {
            if (zdo.GetPrefab() == __signPefab)
            {
                if (!ZNet.instance.GetPeers().Any(x => Utils.DistanceXZ(x.m_refPos, zdo.GetPosition()) < 32))
                    continue;

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
        }

        Logger.LogDebug($"{nameof(Execute)} took {watch.ElapsedMilliseconds} ms to process");
        if (watch.ElapsedMilliseconds > 50)
            Logger.LogWarning($"{nameof(Execute)} took more than 50 ms to process");
    }
}