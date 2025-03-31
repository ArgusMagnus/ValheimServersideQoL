using System.Linq.Expressions;
using static Terminal;
using System.Reflection;

namespace Valheim.ServersideQoL;

static class PrivateAccessor
{
    static Action<ItemDrop.ItemData, ZDO>? __loadFromZDO;
    /// <summary>
    /// Calls <see cref="ItemDrop.LoadFromZDO(ItemDrop.ItemData, ZDO)"/>
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="zdo"></param>
    public static Action<ItemDrop.ItemData, ZDO> LoadFromZDO => __loadFromZDO ??= Expression.Lambda<Action<ItemDrop.ItemData, ZDO>>(
        Expression.Call(
            typeof(ItemDrop).GetMethod("LoadFromZDO", BindingFlags.NonPublic | BindingFlags.Static),
            Expression.Parameter(typeof(ItemDrop.ItemData)) is var par1 ? par1 : throw new Exception(),
            Expression.Parameter(typeof(ZDO)) is var par2 ? par2 : throw new Exception()),
        par1, par2).Compile();

    static Func<ConsoleCommand, ConsoleEvent?>? __getCommandAction;
    public static ConsoleEvent? GetAction(this ConsoleCommand command) => (__getCommandAction ??= Expression.Lambda<Func<ConsoleCommand, ConsoleEvent?>>(
        Expression.Field(
            Expression.Parameter(typeof(ConsoleCommand)) is var par1 ? par1 : throw new Exception(),
            typeof(ConsoleCommand).GetField("action", BindingFlags.Instance | BindingFlags.NonPublic)),
        par1).Compile()).Invoke(command);

    static Func<ConsoleCommand, ConsoleEventFailable?>? __getCommandActionFailable;
    public static ConsoleEventFailable? GetActionFailable(this ConsoleCommand command) => (__getCommandActionFailable ??= Expression.Lambda<Func<ConsoleCommand, ConsoleEventFailable?>>(
        Expression.Field(
            Expression.Parameter(typeof(ConsoleCommand)) is var par1 ? par1 : throw new Exception(),
            typeof(ConsoleCommand).GetField("actionFailable", BindingFlags.Instance | BindingFlags.NonPublic)),
        par1).Compile()).Invoke(command);

    static Func<IReadOnlyList<KeyButton>>? __getServerOptionsGUIPresets;
    public static Func<IReadOnlyList<KeyButton>> GetServerOptionsGUIPresets => __getServerOptionsGUIPresets ??= Expression.Lambda<Func<IReadOnlyList<KeyButton>>>(
        Expression.Field(null, typeof(ServerOptionsGUI).GetField("m_presets", BindingFlags.Static | BindingFlags.NonPublic))).Compile();

    static Func<IReadOnlyList<KeyUI>>? __getServerOptionsGUIModifiers;
    public static Func<IReadOnlyList<KeyUI>> GetServerOptionsGUIModifiers => __getServerOptionsGUIModifiers ??= Expression.Lambda<Func<IReadOnlyList<KeyUI>>>(
        Expression.Field(null, typeof(ServerOptionsGUI).GetField("m_modifiers", BindingFlags.Static | BindingFlags.NonPublic))).Compile();

    static Func<ZDOMan, IReadOnlyDictionary<ZDOID, ZDO>>? __getZDOManObjectsByID;
    public static IReadOnlyDictionary<ZDOID, ZDO> GetObjectsByID(this ZDOMan instance) => (__getZDOManObjectsByID ??= Expression.Lambda<Func<ZDOMan, IReadOnlyDictionary<ZDOID, ZDO>>>(
        Expression.Field(
            Expression.Parameter(typeof(ZDOMan)) is var par1 ? par1 : throw new Exception(),
            typeof(ZDOMan).GetField("m_objectsByID", BindingFlags.NonPublic | BindingFlags.Instance)),
        par1).Compile()).Invoke(instance);

    static Func<Localization, IReadOnlyDictionary<string, string>>? __getLocalizationStrings;
    public static IReadOnlyDictionary<string, string> GetStrings(this Localization instance) => (__getLocalizationStrings ??= Expression.Lambda<Func<Localization, IReadOnlyDictionary<string, string>>>(
        Expression.Field(
            Expression.Parameter(typeof(Localization)) is var par1 ? par1 : throw new Exception(),
            typeof(Localization).GetField("m_translations", BindingFlags.NonPublic | BindingFlags.Instance)),
        par1).Compile()).Invoke(instance);

    static Action<ZDOMan>? __convertPortals;
    public static void ConvertPortals(this ZDOMan instance) => (__convertPortals ??= Expression.Lambda<Action<ZDOMan>>(
        Expression.Call(
            Expression.Parameter(typeof(ZDOMan)) is var par1 ? par1 : throw new Exception(),
            typeof(ZDOMan).GetMethod("ConvertPortals", BindingFlags.NonPublic | BindingFlags.Instance)),
        par1).Compile()).Invoke(instance);
}
