using BepInEx;
using System.Reflection;

namespace Valheim.ServersideQoL;

sealed record RuntimeInformation(GameVersion GameVersion, uint NetworkVersion, int ItemDataVersion, int WorldVersion, string LoadedMods)
{
    sealed record Mod(string GUID, string Name, string? Version);

    public static RuntimeInformation Instance { get; } = Initialize();

    static RuntimeInformation Initialize()
    {
        var versionType = typeof(Game).Assembly.GetType("Version", true);
        if (versionType.GetProperty("CurrentVersion")?.GetValue(null) is not GameVersion gameVersion)
            gameVersion = default;
        if (versionType.GetField("m_networkVersion")?.GetValue(null) is not uint networkVersion)
            networkVersion = default;
        if (versionType.GetField("m_itemDataVersion")?.GetValue(null) is not int itemDataVersion)
            itemDataVersion = default;
        if (versionType.GetField("m_worldVersion")?.GetValue(null) is not int worldVersion)
            worldVersion = default;

        var loadedMods = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => x != typeof(Main).Assembly && !x.IsDynamic)
            .SelectMany(x => x.GetTypes().Where(y => y.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(y)).Select(y => y.GetCustomAttribute<BepInPlugin>()).Where(y => y is not null))
            .Select(x => new Mod(x.GUID, x.Name, $"{x.Version}"))
            .ToList();

        var loadedModsStr = $"{{ {string.Join(", ", loadedMods)} }}";

        return new(gameVersion, networkVersion, itemDataVersion, worldVersion, loadedModsStr);
    }
}