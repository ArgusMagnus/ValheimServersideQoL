using BepInEx;
using BepInEx.Bootstrap;
using System.Reflection;

namespace Valheim.ServersideQoL;

sealed record RuntimeInformation(string ModVersion, GameVersion GameVersion, uint NetworkVersion, int ItemDataVersion, int WorldVersion)
{
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

        return new(Main.PluginInformationalVersion, gameVersion, networkVersion, itemDataVersion, worldVersion);
    }
}