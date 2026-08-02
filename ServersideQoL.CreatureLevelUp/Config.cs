using BepInEx.Configuration;
using static System.Collections.Specialized.BitVector32;

namespace ServersideQoL.CreatureLevelUp;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "CreatureLevelUp";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> ShowHigherLevelStars { get; } = BindEx(cfg, Section, true,
    "True to show stars for higher level creatures (> 2 stars)");
  public ConfigEntry<float> SizeIncreasePerStar { get; } = BindEx(cfg, Section, 0.1f,
    "The relative size increase a starred creature will have");
  public ConfigEntry<int> MaxLevelIncrease { get; } = BindEx(cfg, Section, 0, """
    Amount the max level of creatures is incremented throughout the world.
    The level up chance increases with the max level.
    Example: if this value is set to 2, a creature will spawn with 4 stars with the same probability as it would spawn with 2 stars without this setting.
    """);

  public ConfigEntry<int> MaxLevelIncreasePerDefeatedBoss { get; } = BindEx(cfg, Section, 1, """
    Amount the max level of creatures is incremented per defeated boss.
    The respective boss's biome and previous biomes are affected and the level up chance increases with the max level.
    Example: If this value is set to 1 and Eikthyr and the Elder is defeated, the max creature level in the Black Forest will be raised by 1 and in the Meadows by 2.
    """);

  public ConfigEntry<Heightmap.Biome> TreatOceanAs { get; } = BindEx(cfg, Section, Heightmap.Biome.BlackForest,
    "Biome to treat the ocean as for the purpose of leveling up creatures",
    new AcceptableEnum<Heightmap.Biome>(AcceptableEnum<Heightmap.Biome>.Default.AcceptableValues.Where(static x => x is not Heightmap.Biome.Ocean)));

  public ConfigEntry<bool> LevelUpBosses { get; } = BindEx(cfg, Section, false, "True to also level up bosses");

  public ConfigEntry<RespawnOneTimeSpawnsConditions> RespawnOneTimeSpawnsCondition { get; } = BindEx(cfg, Section, RespawnOneTimeSpawnsConditions.AfterBossDefeated,
    "Condition for one-time spawns to respawn");

  public ConfigEntry<float> RespawnOneTimeSpawnsAfterMinutes { get; } = BindEx(cfg, Section, 240f,
    "Time after one-time spawns are respawned in minutes");

  public enum RespawnOneTimeSpawnsConditions
  {
    Never,
    Always,
    AfterBossDefeated
  }

  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class AdvancedConfig
  {
    public bool ScaleSizeExponentially { get; init; } = true;
    string Star { get; init; } = "⭐";
    string HigherLevelStarNameFormat { get; init; } = "<line-height=150%><voffset=-2em>{0}<size=70%><br><color=yellow>{1}</color></size></voffset></line-height>";
    public string HigherLevelStarName(Character character, int level)
      => string.Format(HigherLevelStarNameFormat, character.m_name, string.Concat(Enumerable.Repeat(Star, level - 1)));
    public float SizeCheckDelaySeconds { get; init; } = 0.5f;
  }
}
