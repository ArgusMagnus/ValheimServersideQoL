using BepInEx.Configuration;
using System.Runtime.CompilerServices;

namespace ServersideQoL.Skills;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, "Skills", true,
    "Enables/disables the entire mod");

  public PickaxeConfig Pickaxe { get; } = new(cfg);

  public sealed class PickaxeConfig(ConfigFile cfg, [CallerMemberName] string section = default!)
  {
    public ConfigEntry<int> RockCollapseThresholdAtMinSkill { get; } = BindEx(cfg, section, 100, $"""
      The percentage of destroyed parts required to collapse a rock or ore deposit at pickaxe skill level 0.
      The actual required percentage scales linearly between this value and {nameof(RockCollapseThresholdAtMaxSkill)} with skill level.
      Set both of these values to -1 to disable this feature.
      """);

    public ConfigEntry<int> RockCollapseThresholdAtMaxSkill { get; } = BindEx(cfg, section, 0, $"""
      The percentage of destroyed parts required to collapse a rock or ore deposit at pickaxe skill level 100.
      The actual required percentage scales linearly between this value and {nameof(RockCollapseThresholdAtMinSkill)} with skill level.
      Set both of these values to -1 to disable this feature.
      """);

    public bool RockCollapseEnabled => Math.Max(RockCollapseThresholdAtMinSkill.Value, RockCollapseThresholdAtMaxSkill.Value) > 0;
  }

  public BloodMagicConfig BloodMagic { get; } = new(cfg);

  public sealed class BloodMagicConfig(ConfigFile cfg, [CallerMemberName] string section = default!)
  {
    public ConfigEntry<int> SummonsLevelUpChanceAtMinSkill { get; } = BindEx(cfg, section, -1, $"""
      The chance (in percent) at skill level 0 to summon a creature with an increased level.
      The actual chance scales linearly between this value and {nameof(SummonsLevelUpChanceAtMaxSkill)} with skill level.
      Set both of these values to -1 to disable this feature.
      """);

    public ConfigEntry<int> SummonsLevelUpChanceAtMaxSkill { get; } = BindEx(cfg, section, -1, $"""
      The chance (in percent) at skill level 100 to summon a creature with an increased level.
      The actual chance scales linearly between this value and {nameof(SummonsLevelUpChanceAtMinSkill)} with skill level.
      Set both of these values to -1 to disable this feature.
      """);

    public bool SummonsLevelUpEnabled => Math.Max(SummonsLevelUpChanceAtMinSkill.Value, SummonsLevelUpChanceAtMaxSkill.Value) > 0;

    public ConfigEntry<int> SummonsMaxLevel { get; } = BindEx(cfg, section, 3, """
      The maximum level a summoned creature can reach.
      """, new AcceptableValueRange<int>(2, 9));

    public ConfigEntry<int> MakeSummonsFriendlyChanceAtMinSkill { get; } = BindEx(cfg, section, -1, $"""
      The chance (in percent) at skill level 0 for hostile summons to be made friendly.
      The actual chance scales linearly between this value and {nameof(MakeSummonsFriendlyChanceAtMaxSkill)} with skill level.
      Set both of these values to -1 to disable this feature.
      This will not affect summons that are already friendly by default.
      """);

    public ConfigEntry<int> MakeSummonsFriendlyChanceAtMaxSkill { get; } = BindEx(cfg, section, -1, $"""
      The chance (in percent) at skill level 100 for hostile summons to be made friendly.
      The actual chance scales linearly between this value and {nameof(MakeSummonsFriendlyChanceAtMinSkill)} with skill level.
      Set both of these values to -1 to disable this feature.
      This will not affect summons that are already friendly by default.
      """);

    public ConfigEntry<bool> MakeFriendlySummonsFollow { get; } = BindEx(cfg, section, true,
      "True to make friendly summoned creatures follow the summoner");

    public bool MakeSummonsFriendlyEnabled => Math.Max(MakeSummonsFriendlyChanceAtMinSkill.Value, MakeSummonsFriendlyChanceAtMaxSkill.Value) > 0;

    public ConfigEntry<int> MakeSummonsTolerateLavaChanceAtMinSkill { get; } = BindEx(cfg, section, -1, $"""
      The chance (in percent) at skill level 0 for a summoned creature to tolerate lava.
      The actual chance scales linearly between this value and {nameof(MakeSummonsTolerateLavaChanceAtMaxSkill)} with skill level.
      Set both of these values to -1 to disable this feature.
      This will not affect summons that already tolerate lava by default.
      """);

    public ConfigEntry<int> MakeSummonsTolerateLavaChanceAtMaxSkill { get; } = BindEx(cfg, section, -1, $"""
      The chance (in percent) at skill level 0 for a summoned creature to tolerate lava.
      The actual chance scales linearly between this value and {nameof(MakeSummonsTolerateLavaChanceAtMinSkill)} with skill level.
      Set both of these values to -1 to disable this feature.
      This will not affect summons that already tolerate lava by default.
      """);

    public bool MakeSummonsTolerateLavaEnabled => Math.Max(MakeSummonsTolerateLavaChanceAtMinSkill.Value, MakeSummonsTolerateLavaChanceAtMaxSkill.Value) > 0;

    public ConfigEntry<float> SummonsHPRegenMultiplierAtMinSkill { get; } = BindEx(cfg, section, 1f, $"""
      The time it takes for a summoned creature to fully regenerate its health at skill level 0 is multiplied by this factor.
      The actual chance scales linearly between this value and {nameof(SummonsHPRegenMultiplierAtMaxSkill)} with skill level.
      Set both of these values to 1 to disable this feature.
      """);

    public ConfigEntry<float> SummonsHPRegenMultiplierAtMaxSkill { get; } = BindEx(cfg, section, 1f, $"""
      The time it takes for a summoned creature to fully regenerate its health at skill level 100 is multiplied by this factor.
      The actual chance scales linearly between this value and {nameof(SummonsHPRegenMultiplierAtMinSkill)} with skill level.
      Set both of these values to 1 to disable this feature.
      """);

    public bool SummonsHPRegenMultiplierEnabled =>
        (SummonsHPRegenMultiplierAtMinSkill.Value, SummonsHPRegenMultiplierAtMaxSkill.Value)
        is { Item1: > 0, Item2: > 0 } and not { Item1: 1f, Item2: 1f };

    public ConfigEntry<float> SummonsSpeedMultiplierAtMinSkill { get; } = BindEx(cfg, section, 1f, $"""
      The movement speed of a summoned creature at skill level 0 is multiplied by this factor.
      The actual chance scales linearly between this value and {nameof(SummonsSpeedMultiplierAtMaxSkill)} with skill level.
      Set both of these values to 1 to disable this feature.
      """);

    public ConfigEntry<float> SummonsSpeedMultiplierAtMaxSkill { get; } = BindEx(cfg, section, 1f, $"""
      The movement speed of a summoned creature at skill level 0 is multiplied by this factor.
      The actual chance scales linearly between this value and {nameof(SummonsSpeedMultiplierAtMinSkill)} with skill level.
      Set both of these values to 1 to disable this feature.
      """);

    public bool SummonsSpeedMultiplierEnabled =>
        (SummonsSpeedMultiplierAtMinSkill.Value, SummonsSpeedMultiplierAtMaxSkill.Value)
        is { Item1: > 0, Item2: > 0 } and not { Item1: 1f, Item2: 1f };

    public ConfigEntry<float> AllowReplacementSummonMinSkill { get; } = BindEx(cfg, section, float.NaN,
      "Min skill level required to allow the summoning of new hostile summons (such as summoned trolls) to replace older ones when the limit exceeded");
  }

  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class AdvancedConfig
  {
    public BloodMagicConfig BloodMagic { get; init; } = new();

    public sealed class BloodMagicConfig
    {
      public sealed record FollowSummonerConfig(float MoveInterval, float MaxDistance) { FollowSummonerConfig() : this(default, default) { } }

      public FollowSummonerConfig FollowSummoners { get; init; } = new(4, 20);
    }
  }
}
