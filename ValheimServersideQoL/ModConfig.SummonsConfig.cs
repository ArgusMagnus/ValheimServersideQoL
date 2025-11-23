using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class SummonsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> UnsummonDistanceMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply unsummon distance by this factor. 0 to disable distance-based unsummoning", new AcceptableValueRange<float>(0, float.PositiveInfinity));
        public ConfigEntry<float> UnsummonLogoutTimeMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply the time after which summons are unsummoned when the player logs out. 0 to disable logout-based unsummoning", new AcceptableValueRange<float>(0, float.PositiveInfinity));
    }
}