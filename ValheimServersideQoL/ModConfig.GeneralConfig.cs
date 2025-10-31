using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class GeneralConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enabled { get; } = cfg.BindEx(section, true,
            "Enables/disables the entire mod");
        public ConfigEntry<bool> ConfigPerWorld { get; } = cfg.BindEx(section, false,
            "Use one config file per world. The file is saved next to the world file");
        public ConfigEntry<bool> InWorldConfigRoom { get; } = cfg.BindEx(section, false,
            "True to generate an in-world room which admins can enter to configure this mod by editing signs. A portal is placed at the start location");
        public ConfigEntry<float> FarMessageRange { get; } = cfg.BindEx(section, ZoneSystem.c_ZoneSize,
            $"Max distance a player can have to a modified object to receive messages of type {MessageTypes.TopLeftFar} or {MessageTypes.CenterFar}");

        public ConfigEntry<bool> DiagnosticLogs { get; } = cfg.BindEx(section, false,
            "Enables/disables diagnostic logs");
        public ConfigEntry<int> ZonesAroundPlayers { get; } = cfg.BindEx(section, ZoneSystem.instance.GetActiveArea(),
            "Zones to process around each player");
        public ConfigEntry<float> MinPlayerDistance { get; } = cfg.BindEx(section, 4f,
            "Min distance all players must have to a ZDO for it to be modified");
        public ConfigEntry<bool> IgnoreGameVersionCheck { get; } = cfg.BindEx(section, true,
            "True to ignore the game version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreNetworkVersionCheck { get; } = cfg.BindEx(section, false,
            "True to ignore the network version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreItemDataVersionCheck { get; } = cfg.BindEx(section, false,
            "True to ignore the item data version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreWorldVersionCheck { get; } = cfg.BindEx(section, false,
            "True to ignore the world version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
    }
}
