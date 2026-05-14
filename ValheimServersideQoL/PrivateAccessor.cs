using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using static Terminal;
using static ZoneSystem;

namespace Valheim.ServersideQoL;

static class PrivateAccessor
{
    const BindingFlags AccessFlags = BindingFlags.NonPublic
#if !DEBUG
         | BindingFlags.Public
#endif
        ;

    static Func<ConsoleCommand, ConsoleEvent?> GetCommandAction
#if DEBUG
    { get; } =
#else
    => field ??=
#endif
        Expression.Lambda<Func<ConsoleCommand, ConsoleEvent?>>(
        Expression.Field(
            Expression.Parameter(typeof(ConsoleCommand)) is var par1 ? par1 : throw new Exception(),
            typeof(ConsoleCommand).GetField("action", BindingFlags.Instance | AccessFlags)),
        par1).Compile();

    public static ConsoleEvent? GetAction(this ConsoleCommand command) => GetCommandAction(command);

    static Func<ConsoleCommand, ConsoleEventFailable?> GetCommandActionFailable
#if DEBUG
    { get; } =
#else
    => field ??=
#endif
        Expression.Lambda<Func<ConsoleCommand, ConsoleEventFailable?>>(
        Expression.Field(
            Expression.Parameter(typeof(ConsoleCommand)) is var par1 ? par1 : throw new Exception(),
            typeof(ConsoleCommand).GetField("actionFailable", BindingFlags.Instance | AccessFlags)),
        par1).Compile();

    public static ConsoleEventFailable? GetActionFailable(this ConsoleCommand command) => GetCommandActionFailable(command);

    public static Func<IReadOnlyList<KeyButton>> GetServerOptionsGUIPresets
#if DEBUG
    { get; } =
#else
    => field ??=
#endif
        Expression.Lambda<Func<IReadOnlyList<KeyButton>>>(
        Expression.Field(null, typeof(ServerOptionsGUI).GetField("m_presets", BindingFlags.Static | AccessFlags))).Compile();

    public static Func<IReadOnlyList<KeyUI>> GetServerOptionsGUIModifiers
#if DEBUG
    { get; } =
#else
    => field ??=
#endif
        Expression.Lambda<Func<IReadOnlyList<KeyUI>>>(
        Expression.Field(null, typeof(ServerOptionsGUI).GetField("m_modifiers", BindingFlags.Static | AccessFlags))).Compile();

    static Func<ZDOMan, Dictionary<ZDOID, ZDO>> GetZDOManObjectsByID
#if DEBUG
    { get; } =
#else
    => field ??=
#endif
        Expression.Lambda<Func<ZDOMan, Dictionary<ZDOID, ZDO>>>(
        Expression.Field(
            Expression.Parameter(typeof(ZDOMan)) is var par1 ? par1 : throw new Exception(),
            typeof(ZDOMan).GetField("m_objectsByID", AccessFlags | BindingFlags.Instance)),
        par1).Compile();

    static Dictionary<ZDOID, ZDO> GetObjectsByIDCore(this ZDOMan instance) => GetZDOManObjectsByID(instance);
    public static Dictionary<ZDOID, ZDO>.ValueCollection GetObjects(this ZDOMan instance) => GetObjectsByIDCore(instance).Values;

    static Func<Localization, IReadOnlyDictionary<string, string>> GetLocalizationStrings
#if DEBUG
    { get; } =
#else
    => field ??=
#endif
        Expression.Lambda<Func<Localization, IReadOnlyDictionary<string, string>>>(
        Expression.Field(
            Expression.Parameter(typeof(Localization)) is var par1 ? par1 : throw new Exception(),
            typeof(Localization).GetField("m_translations", AccessFlags | BindingFlags.Instance)),
        par1).Compile();

    public static IReadOnlyDictionary<string, string> GetStrings(this Localization instance) => GetLocalizationStrings(instance);

    static Func<RandEventSystem, RandomEvent?> GetCurrentEventFunc
#if DEBUG
    { get; } =
#else
    => field ??=
#endif
        Expression.Lambda<Func<RandEventSystem, RandomEvent>>(
        Expression.Field(
            Expression.Parameter(typeof(RandEventSystem)) is var par1 ? par1 : throw new Exception(),
            typeof(RandEventSystem).GetField("m_randomEvent", AccessFlags | BindingFlags.Instance)),
        par1).Compile();

    public static RandomEvent? GetCurrentEvent(this RandEventSystem instance) => GetCurrentEventFunc(instance);

    static Func<ZoneSystem, IReadOnlyDictionary<int, ZoneLocation>> GetLocationsByHashFunc
#if DEBUG
    { get; } =
#else
    => field ??=
#endif
        Expression.Lambda<Func<ZoneSystem, IReadOnlyDictionary<int, ZoneLocation>>>(
        Expression.Field(
            Expression.Parameter(typeof(ZoneSystem)) is var par1 ? par1 : throw new Exception(),
            typeof(ZoneSystem).GetField("m_locationsByHash", AccessFlags | BindingFlags.Instance)),
        par1).Compile();

    public static IReadOnlyDictionary<int, ZoneLocation> GetLocationsByHash(this ZoneSystem instance) => GetLocationsByHashFunc(instance);

    static Func<ZoneSystem, ZoneLocation, int, Vector3, Quaternion, SpawnMode, List<GameObject>, GameObject> SpawnLocationFunc
#if DEBUG
    { get; } =
#else
    => field ??=
#endif
        Expression.Lambda<Func<ZoneSystem, ZoneLocation, int, Vector3, Quaternion, SpawnMode, List<GameObject>, GameObject>>(
        Expression.Call(
            Expression.Parameter(typeof(ZoneSystem)) is var par1 ? par1 : throw new Exception(),
            typeof(ZoneSystem).GetMethod("SpawnLocation", AccessFlags | BindingFlags.Instance),
            Expression.Parameter(typeof(ZoneLocation)) is var par2 ? par2 : throw new Exception(),
            Expression.Parameter(typeof(int)) is var par3 ? par3 : throw new Exception(),
            Expression.Parameter(typeof(Vector3)) is var par4 ? par4 : throw new Exception(),
            Expression.Parameter(typeof(Quaternion)) is var par5 ? par5 : throw new Exception(),
            Expression.Parameter(typeof(SpawnMode)) is var par6 ? par6 : throw new Exception(),
            Expression.Parameter(typeof(List<GameObject>)) is var par7 ? par7 : throw new Exception()),
        par1, par2, par3, par4, par5, par6, par7).Compile();

    public static GameObject SpawnLocation(this ZoneSystem instance, ZoneLocation location, int seed, Vector3 pos, Quaternion rot, SpawnMode mode, List<GameObject>? spawnedGhostObjects = null)
        => SpawnLocationFunc(instance, location, seed, pos, rot, mode, spawnedGhostObjects ?? []);

    static Action<ZoneSystem, long> SendGlobalKeysAction
#if DEBUG
    { get; } =
#else
    => field ??=
#endif
        Expression.Lambda<Action<ZoneSystem, long>>(
        Expression.Call(
            Expression.Parameter(typeof(ZoneSystem)) is var par1 ? par1 : throw new Exception(),
            typeof(ZoneSystem).GetMethod("SendGlobalKeys", AccessFlags | BindingFlags.Instance),
            Expression.Parameter(typeof(long)) is var par2 ? par2 : throw new Exception()),
        par1, par2).Compile();

    public static void SendGlobalKeys(this ZoneSystem instance, long peerID) => SendGlobalKeysAction(instance, peerID);

    public static int ZSyncAnimationZDOSalt { get; } = (int)typeof(ZSyncAnimation).GetField("c_ZDOSalt", AccessFlags | BindingFlags.Static).GetRawConstantValue();
    public static int CharacterAnimationHashEncumbered { get; } = (int)typeof(Character).GetField("s_encumbered", AccessFlags | BindingFlags.Static).GetValue(null);
    public static int CharacterAnimationHashInWater { get; } = (int)typeof(Character).GetField("s_inWater", AccessFlags | BindingFlags.Static).GetValue(null);
    public static int PlayerAnimationHashCrouching { get; } = (int)typeof(Player).GetField("s_crouching", AccessFlags | BindingFlags.Static).GetValue(null);
}
