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

        public ConfigEntry<int> BloodmagicSummonsMinLevelUpChance { get; } = cfg.BindEx(section, -1, """
            The chance (in percent) at skill level 0 to summon a creature with an increased level.
            A value of -1 disables this feature.
            """, new AcceptableValueRange<int>(0, 100));

        public ConfigEntry<int> BloodmagicSummonsMaxLevelUpChance { get; } = cfg.BindEx(section, -1, """
            The chance (in percent) at skill level 100 to summon a creature with an increased level.
            A value of -1 disables this feature.
            """, new AcceptableValueRange<int>(0, 100));

        public ConfigEntry<int> BloodmagicSummonsMaxLevel { get; } = cfg.BindEx(section, 3, """
            The maximum level a summoned creature can reach.
            """, new AcceptableValueRange<int>(2, 9));

        public ConfigEntry<int> BloodmagicMakeHostileSummonsFriendlyMinChance { get; } = cfg.BindEx(section, -1, """
            The chance (in percent) at skill level 0 for hostile summons to be made friendly.
            A value of -1 disables this feature.
            This will not affect summons that are already friendly by default.
            """, new AcceptableValueRange<int>(-1, 100));

        public ConfigEntry<int> BloodmagicMakeHostileSummonsFriendlyMaxChance { get; } = cfg.BindEx(section, -1, """
            The chance (in percent) at skill level 100 for hostile summons to be made friendly.
            A value of -1 disables this feature.
            This will not affect summons that are already friendly by default.
            """, new AcceptableValueRange<int>(-1, 100));

        public ConfigEntry<int> BloodmagicMakeSummonsTolerateLavaMinChance { get; } = cfg.BindEx(section, -1, """
            The chance (in percent) at skill level 0 for a summoned creature to tolerate lava.
            A value of -1 disables this feature.
            This will not affect summons that already tolerate lava by default.
            """, new AcceptableValueRange<int>(-1, 100));

        public ConfigEntry<int> BloodmagicMakeSummonsTolerateLavaMaxChance { get; } = cfg.BindEx(section, -1, """
            The chance (in percent) at skill level 0 for a summoned creature to tolerate lava.
            A value of -1 disables this feature.
            This will not affect summons that already tolerate lava by default.
            """, new AcceptableValueRange<int>(-1, 100));
    }
}