using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class CartsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> ContentMassMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiplier for a carts content weight. E.g. set to 0 to ignore a cart's content weight",
            new AcceptableValueRange<float>(0, float.PositiveInfinity));
    }
}