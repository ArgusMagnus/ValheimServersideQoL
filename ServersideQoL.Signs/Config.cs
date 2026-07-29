using BepInEx.Configuration;

namespace ServersideQoL.Signs;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "Signs";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<string> DefaultColor { get; } = BindEx(cfg, Section, "",
    "Default color for signs. Can be a color name or hex code (e.g. #FF0000 for red)");
  public ConfigEntry<bool> TimeSigns { get; } = BindEx(cfg, Section, true,
    Invariant($"True to update sign texts which contain time emojis (any of {string.Concat(SignProcessor.ClockEmojis)}) with the in-game time"));

  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class AdvancedConfig
  {
    public ProcessingDelaysConfig ProcessingDelays { get; init; } = new();

    public sealed class ProcessingDelaysConfig
    {
      public float TimeSigns { get; init; } = 0.5f;
    }
  }
}
