using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class FireplacesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MakeToggleable { get; } = cfg.BindEx(section, false,
            "True to make all fireplaces (including torches, braziers, etc.) toggleable");
        public ConfigEntry<bool> InfiniteFuel { get; } = cfg.BindEx(section, false,
            "True to make all fireplaces have infinite fuel");
        public ConfigEntry<IgnoreRainOptions> IgnoreRain { get; } = cfg.BindEx(section, IgnoreRainOptions.Never,
            "Options to make all fireplaces ignore rain", AcceptableEnum<IgnoreRainOptions>.Default);

        public enum IgnoreRainOptions
        {
            Never,
            Always,
            InsideShield
        }
    }
}