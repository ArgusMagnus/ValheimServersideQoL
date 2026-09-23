using BepInEx.Configuration;

namespace ServersideQoL.Treesurrection;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");
  public ConfigEntry<int> SaplingDropChance { get; } = BindEx(cfg, 100,
    "The probability a destroyed tree stump will drop a sapling", new AcceptableValueRange<int>(0, 100));
  public ConfigEntry<float> SaplingInvulnerabilitySeconds { get; } = BindEx(cfg, 10f,
    "How long saplings will be invulnerable after being dropped");
  public ConfigEntry<float> GrowingTimeMultiplier { get; } = BindEx(cfg, 1f,
    "How long new saplings take to regrow as a factor of the time player planted trees take to grow");

  //public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);
  //public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  //public sealed class AdvancedConfig
  //{
  //}

  //public sealed class LocalizationConfig
  //{
  //}
}
