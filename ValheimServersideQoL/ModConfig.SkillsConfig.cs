using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class SkillsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> PickaxeAffectsRockDestruction { get; } = cfg.BindEx(section, false, """
             True to make the pickaxe skill affect the destruction of rocks and ore deposits.
             If true, rocks and ore deposits will be completely destroyed once more than (100 - Pickaxe Skill Level)%
             of their parts have been destroyed.
             E.g. at skill level 10, 90% of the parts need to be destroyed while at skill level 80, 20% destroyed parts are enough
             to destroy the whole rock/ore deposit
             """);
    }
}