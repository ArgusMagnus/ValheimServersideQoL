using BepInEx.Configuration;

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


  //public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  //public sealed class AdvancedConfig
  //{
  //  public ProcessingDelaysConfig ProcessingDelays { get; init; } = new();

  //  public sealed class ProcessingDelaysConfig
  //  {
  //    public float TimeSigns { get; init; } = 0.5f;
  //  }
  //}
}
