using BepInEx;
using System.Reflection;

namespace Valheim.ServersideQoL;

sealed record RuntimeInformation(GameVersion GameVersion, uint NetworkVersion, int ItemDataVersion, int WorldVersion, string LoadedMods, bool ExceptionWhenReadingLoadedMods)
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

        var excpetionsCaught = false;
        var loadedMods = new List<Mod>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x != typeof(Main).Assembly && !x.IsDynamic))
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    try
                    {
                        if (type.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(type) && type.GetCustomAttribute<BepInPlugin>() is { } plugin)
                            loadedMods.Add(new(plugin.GUID, plugin.Name, $"{plugin.Version}"));
                    }
                    catch (Exception) { excpetionsCaught = true; }
                }
            }
            catch (Exception) { excpetionsCaught = true; }
        }

        var loadedModsStr = $"{{ {string.Join(", ", loadedMods)} }}";

        return new(gameVersion, networkVersion, itemDataVersion, worldVersion, loadedModsStr, excpetionsCaught);
    }
}