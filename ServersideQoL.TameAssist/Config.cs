using BepInEx.Configuration;

namespace ServersideQoL.TameAssist;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "TameAssist";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> MakeCommandable { get; } = BindEx(cfg, Section, false, "True to make all tames commandable (like wolves)");
  public ConfigEntry<MessageTypes> TamingProgressMessageType { get; } = BindEx(cfg, Section, MessageTypes.None,
    "Type of taming progress messages to show", AcceptableEnum<MessageTypes>.Default);
  public ConfigEntry<MessageTypes> GrowingProgressMessageType { get; } = BindEx(cfg, Section, MessageTypes.None,
    "Type of growing progress messages to show", AcceptableEnum<MessageTypes>.Default);
  public ConfigEntry<float> FedDurationMultiplier { get; } = BindEx(cfg, Section, 1f, Invariant(
    $"Multiply the time tames stay fed after they have eaten by this factor. {float.PositiveInfinity} to keep them fed indefinitely"));
  public ConfigEntry<float> TamingTimeMultiplier { get; } = BindEx(cfg, Section, 1f, """
    Multiply the time it takes to tame a tameable creature by this factor.
    E.g. a value of 0.5 means that the taming time is halved.
    """);
  public ConfigEntry<float> PotionTamingBoostMultiplier { get; } = BindEx(cfg, Section, 1f, """
    Multiply the taming boost from the animal whispers potion by this factor.
    E.g. a value of 2 means that the effect of the potion is doubled and the resulting taming time is reduced by a factor of 4 per player.
    """);

  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  public sealed class LocalizationConfig
  {
    string Growing { get; init; } = "$caption_growing {0}%";
    public string FormatGrowing(int percent) => string.Format(Growing, percent);
    string Taming { get; init; } = "$hud_tameness {0:P0}";
    string TamingHungry { get; init; } = "$hud_tameness {0:P0}, $hud_tamehungry";
    public string FormatTaming(float tameness, bool isHungry) => isHungry ? string.Format(TamingHungry, tameness) : string.Format(Taming, tameness);
  }
}
