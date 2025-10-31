using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class WindmillsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> IgnoreWind { get; } = cfg.BindEx(section, false,
            "True to make windmills ignore wind (Cover still decreases operating efficiency though)");
    }
}