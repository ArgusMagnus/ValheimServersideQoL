using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class ShipsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DeconstructWithHammer { get; } = cfg.BindEx(section, false,
            "If enabled, ships can be deconstructed with the build hammer");
    }
}