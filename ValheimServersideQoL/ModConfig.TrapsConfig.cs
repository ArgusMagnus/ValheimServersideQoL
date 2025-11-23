using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class TrapsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DisableTriggeredByPlayers { get; } = cfg.BindEx(section, false,
            "True to stop traps from being triggered by players");
        public ConfigEntry<bool> DisableFriendlyFire { get; } = cfg.BindEx(section, false,
            "True to stop traps from damaging players and tames. Does not work reliably (yet).");
        public ConfigEntry<float> SelfDamageMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply the damage the trap takes when it is triggered by this factor. 0 to make the trap take no damage",
            new AcceptableValueRange<float>(0, float.PositiveInfinity));
        public ConfigEntry<bool> AutoRearm { get; } = cfg.BindEx(section, false,
            "True to automatically rearm traps when they are triggered");
    }
}