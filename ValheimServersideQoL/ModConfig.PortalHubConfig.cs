using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class PortalHubConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enable { get; } = cfg.BindEx(section, false, """
             True to automatically generate a portal hub.
             Placed portals which don't have a paired portal in the world will be connected to the portal hub.
             """);

        public ConfigEntry<string> Exclude { get; } = cfg.BindEx(section, "", "Portals with a tag that matches this filter are not connected to the portal hub");
        public ConfigEntry<string> Include { get; } = cfg.BindEx(section, "*", "Only portals with a tag that matches this filter are connected to the portal hub");
        public ConfigEntry<bool> AutoNameNewPortals { get; } = cfg.BindEx(section, false, $"True to automatically name new portals. Has no effect if '{nameof(Enable)}' is false");
        public ConfigEntry<string> AutoNameNewPortalsFormat { get; } = cfg.BindEx(section, "{0} {1:D2}",
            "Format string for auto-naming portals, the first argument is the biome name, the second is an automatically incremented integer",
            new AcceptableFormatString(["Test", 0]));
    }
}