using BepInEx.Configuration;

namespace ServersideQoL.Signs;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  internal const string NotSetDefaultColor = "black";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");
  public ConfigEntry<string> DefaultColor { get; } = BindEx(cfg, NotSetDefaultColor,
    "Default color for signs", new AcceptableValueList<string>([
      // https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichTextColor.html (maybe obsolete)
      // https://docs.unity3d.com/Manual/UIE-supported-tags.html (doesn't mention supported color names)
      "black", "blue", "green", "orange", "purple", "red", "white", "yellow"]));
  public ConfigEntry<bool> TimeSigns { get; } = BindEx(cfg, true,
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
