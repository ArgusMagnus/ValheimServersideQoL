using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class FermentersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> FermentationDurationMultiplier { get; } = cfg.BindEx(section, 1f, "Multiply the time fermentation takes by this factor.");
    }
}