using BepInEx.Configuration;
using static System.Collections.Specialized.BitVector32;

namespace Valheim.ServersideQoL;

sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
    const string Section = "General";

    public ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
        "Enables/disables the entire mod");
    public ConfigEntry<bool> DiagnosticLogs { get; } = BindEx(cfg, Section, false,
            "Enables/disables diagnostic logs");
    public ConfigEntry<bool> IgnoreGameVersionCheck { get; } = BindEx(cfg, Section, true,
        "True to ignore the game version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
    public ConfigEntry<bool> IgnoreNetworkVersionCheck { get; } = BindEx(cfg, Section, false,
        "True to ignore the network version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
    public ConfigEntry<bool> IgnoreItemDataVersionCheck { get; } = BindEx(cfg, Section, false,
        "True to ignore the item data version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
    public ConfigEntry<bool> IgnoreWorldVersionCheck { get; } = BindEx(cfg, Section, false,
        "True to ignore the world version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
}
