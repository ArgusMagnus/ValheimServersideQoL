using System;
using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class WearNTearConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DisableRainDamage { get; } = cfg.BindEx(section, false,
            "True to prevent rain from damaging build pieces");

        public ConfigEntry<DisableSupportRequirementsOptions> DisableSupportRequirements { get; } = cfg.BindEx(section, DisableSupportRequirementsOptions.None,
            "Ignore support requirements on build pieces", AcceptableEnum<DisableSupportRequirementsOptions>.Default);

        public ConfigEntry<bool> MakeIndestructible { get; } = cfg.BindEx(section, false,
            "True to make player-built pieces indestructible");

        [Flags]
        public enum DisableSupportRequirementsOptions
        {
            None,
            PlayerBuilt = 1 << 0,
            World = 1 << 1
        }
    }
}