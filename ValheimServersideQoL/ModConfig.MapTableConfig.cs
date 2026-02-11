using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class MapTableConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> AutoUpdatePortals { get; } = cfg.BindEx(section, false,
            "True to update map tables with portal pins");
        public ConfigEntry<string> AutoUpdatePortalsExclude { get; } = cfg.BindEx(section, "",
            "Portals with a tag that matches this filter are not added to map tables");
        public ConfigEntry<string> AutoUpdatePortalsInclude { get; } = cfg.BindEx(section, "*",
            "Only portals with a tag that matches this filter are added to map tables");

        public ConfigEntry<bool> AutoUpdateShips { get; } = cfg.BindEx(section, false,
            "True to update map tables with ship pins");
        public ConfigEntry<MessageTypes> UpdatedMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when a map table is updated", AcceptableEnum<MessageTypes>.Default);

        public ConfigEntry<float>? MapViewDistance { get; } = ZNet.instance.IsServer() && ZNet.instance.IsDedicated() ?
            cfg.BindEx(section, 0f, $"""
                Max distance to a map table at which players can view their map.
                Has no effect unless the {GlobalKeys.NoMap} global key is set.
                0 to disable this feature.
                """) : null;
    }
}