using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class SkillsConfig(ConfigFile cfg, string section)
    {
        [Obsolete(null, true)]
        public ConfigEntry<bool> PickaxeAffectsRockDestruction { get; } = cfg.BindEx(section, false, """
             True to make the pickaxe skill affect the destruction of rocks and ore deposits.
             If true, rocks and ore deposits will be completely destroyed once more than (100 - Pickaxe Skill Level)%
             of their parts have been destroyed.
             E.g. at skill level 10, 90% of the parts need to be destroyed while at skill level 80, 20% destroyed parts are enough
             to destroy the whole rock/ore deposit
             """,
            deprecated: new($"Use {nameof(PickaxeRockCollapseThresholdAtMinSkill)}/{nameof(PickaxeRockCollapseThresholdAtMaxSkill)} instead", static cfg =>
            {
                cfg.Skills.PickaxeRockCollapseThresholdAtMinSkill.Value = 100;
                cfg.Skills.PickaxeRockCollapseThresholdAtMaxSkill.Value = 0;
            }));

        public ConfigEntry<int> PickaxeRockCollapseThresholdAtMinSkill { get; } = cfg.BindEx(section, -1, $"""
            The percentage of destroyed parts required to collapse a rock or ore deposit at pickaxe skill level 0.
            The actual required percentage scales linearly between this value and {nameof(PickaxeRockCollapseThresholdAtMaxSkill)} with skill level.
            Set both of these values to -1 to disable this feature.
            """);

        public ConfigEntry<int> PickaxeRockCollapseThresholdAtMaxSkill { get; } = cfg.BindEx(section, -1, $"""
            The percentage of destroyed parts required to collapse a rock or ore deposit at pickaxe skill level 100.
            The actual required percentage scales linearly between this value and {nameof(PickaxeRockCollapseThresholdAtMinSkill)} with skill level.
            Set both of these values to -1 to disable this feature.
            """);

        public bool PickaxeRockCollapseEnabled => Math.Max(PickaxeRockCollapseThresholdAtMinSkill.Value, PickaxeRockCollapseThresholdAtMaxSkill.Value) > 0;

        public ConfigEntry<int> BloodmagicSummonsLevelUpChanceAtMinSkill { get; } = cfg.BindEx(section, -1, $"""
            The chance (in percent) at skill level 0 to summon a creature with an increased level.
            The actual chance scales linearly between this value and {nameof(BloodmagicSummonsLevelUpChanceAtMaxSkill)} with skill level.
            Set both of these values to -1 to disable this feature.
            """);

        public ConfigEntry<int> BloodmagicSummonsLevelUpChanceAtMaxSkill { get; } = cfg.BindEx(section, -1, $"""
            The chance (in percent) at skill level 100 to summon a creature with an increased level.
            The actual chance scales linearly between this value and {nameof(BloodmagicSummonsLevelUpChanceAtMinSkill)} with skill level.
            Set both of these values to -1 to disable this feature.
            """);

        public bool BloodmagicSummonsLevelUpEnabled => Math.Max(BloodmagicSummonsLevelUpChanceAtMinSkill.Value, BloodmagicSummonsLevelUpChanceAtMaxSkill.Value) > 0;

        public ConfigEntry<int> BloodmagicSummonsMaxLevel { get; } = cfg.BindEx(section, 3, """
            The maximum level a summoned creature can reach.
            """, new AcceptableValueRange<int>(2, 9));

        public ConfigEntry<int> BloodmagicMakeSummonsFriendlyChanceAtMinSkill { get; } = cfg.BindEx(section, -1, $"""
            The chance (in percent) at skill level 0 for hostile summons to be made friendly.
            The actual chance scales linearly between this value and {nameof(BloodmagicMakeSummonsFriendlyChanceAtMaxSkill)} with skill level.
            Set both of these values to -1 to disable this feature.
            This will not affect summons that are already friendly by default.
            """);

        public ConfigEntry<int> BloodmagicMakeSummonsFriendlyChanceAtMaxSkill { get; } = cfg.BindEx(section, -1, $"""
            The chance (in percent) at skill level 100 for hostile summons to be made friendly.
            The actual chance scales linearly between this value and {nameof(BloodmagicMakeSummonsFriendlyChanceAtMinSkill)} with skill level.
            Set both of these values to -1 to disable this feature.
            This will not affect summons that are already friendly by default.
            """);

        public bool BloodmagicMakeSummonsFriendlyEnabled => Math.Max(BloodmagicMakeSummonsFriendlyChanceAtMinSkill.Value, BloodmagicMakeSummonsFriendlyChanceAtMaxSkill.Value) > 0;

        public ConfigEntry<int> BloodmagicMakeSummonsTolerateLavaChanceAtMinSkill { get; } = cfg.BindEx(section, -1, $"""
            The chance (in percent) at skill level 0 for a summoned creature to tolerate lava.
            The actual chance scales linearly between this value and {nameof(BloodmagicMakeSummonsTolerateLavaChanceAtMaxSkill)} with skill level.
            Set both of these values to -1 to disable this feature.
            This will not affect summons that already tolerate lava by default.
            """);

        public ConfigEntry<int> BloodmagicMakeSummonsTolerateLavaChanceAtMaxSkill { get; } = cfg.BindEx(section, -1, $"""
            The chance (in percent) at skill level 0 for a summoned creature to tolerate lava.
            The actual chance scales linearly between this value and {nameof(BloodmagicMakeSummonsTolerateLavaChanceAtMinSkill)} with skill level.
            Set both of these values to -1 to disable this feature.
            This will not affect summons that already tolerate lava by default.
            """);

        public bool BloodmagicMakeSummonsTolerateLavaEnabled => Math.Max(BloodmagicMakeSummonsTolerateLavaChanceAtMinSkill.Value, BloodmagicMakeSummonsTolerateLavaChanceAtMaxSkill.Value) > 0;

        public ConfigEntry<float> BloodmagicSummonsHPRegenMultiplierAtMinSkill { get; } = cfg.BindEx(section, 1f, $"""
            The time it takes for a summoned creature to fully regenerate its health at skill level 0 is multiplied by this factor.
            The actual chance scales linearly between this value and {nameof(BloodmagicSummonsHPRegenMultiplierAtMaxSkill)} with skill level.
            Set both of these values to 1 to disable this feature.
            """);

        public ConfigEntry<float> BloodmagicSummonsHPRegenMultiplierAtMaxSkill { get; } = cfg.BindEx(section, 1f, $"""
            The time it takes for a summoned creature to fully regenerate its health at skill level 100 is multiplied by this factor.
            The actual chance scales linearly between this value and {nameof(BloodmagicSummonsHPRegenMultiplierAtMinSkill)} with skill level.
            Set both of these values to 1 to disable this feature.
            """);

        public bool BloodmagicSummonsHPRegenMultiplierEnabled =>
            (BloodmagicSummonsHPRegenMultiplierAtMinSkill.Value, BloodmagicSummonsHPRegenMultiplierAtMaxSkill.Value)
            is { Item1: > 0, Item2: > 0 } and not { Item1: 1f, Item2: 1f };

        public ConfigEntry<float> BloodmagicSummonsSpeedMultiplierAtMinSkill { get; } = cfg.BindEx(section, 1f, $"""
            The movement speed of a summoned creature at skill level 0 is multiplied by this factor.
            The actual chance scales linearly between this value and {nameof(BloodmagicSummonsSpeedMultiplierAtMaxSkill)} with skill level.
            Set both of these values to 1 to disable this feature.
            """);

        public ConfigEntry<float> BloodmagicSummonsSpeedMultiplierAtMaxSkill { get; } = cfg.BindEx(section, 1f, $"""
            The movement speed of a summoned creature at skill level 0 is multiplied by this factor.
            The actual chance scales linearly between this value and {nameof(BloodmagicSummonsSpeedMultiplierAtMinSkill)} with skill level.
            Set both of these values to 1 to disable this feature.
            """);

        public bool BloodmagicSummonsSpeedMultiplierEnabled =>
            (BloodmagicSummonsSpeedMultiplierAtMinSkill.Value, BloodmagicSummonsSpeedMultiplierAtMaxSkill.Value)
            is { Item1: > 0, Item2: > 0 } and not { Item1: 1f, Item2: 1f };

        public bool AnyEnbaled =>
            PickaxeRockCollapseEnabled ||
            BloodmagicSummonsLevelUpEnabled ||
            BloodmagicMakeSummonsFriendlyEnabled ||
            BloodmagicMakeSummonsTolerateLavaEnabled ||
            BloodmagicSummonsHPRegenMultiplierEnabled ||
            BloodmagicSummonsSpeedMultiplierEnabled;

    }
}