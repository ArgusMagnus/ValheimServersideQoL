using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class PlantsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> GrowTimeMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply plant grow time by this factor. 0 to make them grow almost instantly.", new AcceptableValueRange<float>(0, float.PositiveInfinity));
        public ConfigEntry<float> SpaceRequirementMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply plant space requirement by this factor. 0 to disable space requirements.", new AcceptableValueRange<float>(0, float.PositiveInfinity));
        public ConfigEntry<bool> DontDestroyIfCantGrow { get; } = cfg.BindEx(section, false,
            "True to keep plants that can't grow alive");
        //public ConfigEntry<bool> MakeHarvestableWithScythe { get; } = cfg.BindEx(section, false, "True to make all crops harvestable with the scythe");
    }
}