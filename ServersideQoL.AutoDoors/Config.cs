using BepInEx.Configuration;

namespace ServersideQoL.AutoDoors;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "AutoDoors";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<float> AutoCloseMinPlayerDistance { get; } = BindEx(cfg, Section, 4f,
    Invariant($"Minimum distance all players must have to the door before it is closed."));
  public ConfigEntry<float> AutoCloseMinOpenSeconds { get; } = BindEx(cfg, Section, 2f,
    Invariant($"Minimum time the door must have been open before it is closed."));

  //public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);
  //public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  //public sealed class LocalizationConfig
  //{
  //}

  //public sealed class AdvancedConfig
  //{
  //  public float ResetTerrainRadius { get; init; } = 3;
  //}
}
