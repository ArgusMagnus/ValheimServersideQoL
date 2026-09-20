using BepInEx.Configuration;

namespace ServersideQoL.Treesurrection;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");

  public ConfigEntry<float> GrowingTimeMultiplier { get; } = BindEx(cfg, 1f,
    "How long stumps take to regrow as a factor of the time player planted trees take to grow");

  //public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);
  //public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  //public sealed class AdvancedConfig
  //{
  //}

  //public sealed class LocalizationConfig
  //{
  //}
}
