using BepInEx.Configuration;

namespace ServersideQoL.AutoProcess;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "AutoProcess";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> FeedFromContainers { get; } = BindEx(cfg, Section, true,
    "True to automatically feed smelters from nearby containers");
  public ConfigEntry<float> FeedFromContainersRange { get; } = BindEx(cfg, Section, 4f, $"""
    Required proximity of a container to a smelter to be used as feeding source.
    Can be overridden per chest with the {nameof(ServersideQoL)}.ContainerSigns mod.
    """);
  public int? FeedFromContainersMaxRange => Shared.FeedFromContainersMaxRange?.Value;
  public ConfigEntry<float> FeedFromContainersMinPlayerDistance { get; } = BindEx(cfg, Section, 4f,
    "Min distance all players must have to a processing station");
  public ConfigEntry<int> FeedFromContainersLeaveAtLeastFuel { get; } = BindEx(cfg, Section, 1,
    "Minimum amount of fuel to leave in a container");
  public ConfigEntry<int> FeedFromContainersLeaveAtLeastOre { get; } = BindEx(cfg, Section, 1,
    "Minimum amount of ore to leave in a container");
  public ConfigEntry<MessageTypes> OreOrFuelAddedMessageType { get; } = BindEx(cfg, Section, MessageTypes.None,
    "Type of message to show when ore or fuel is added to a smelter", AcceptableEnum<MessageTypes>.Default);
  public ConfigEntry<float> CapacityMultiplier { get; } = BindEx(cfg, Section, 1f,
    "Multiply a smelter's ore/fuel capacity by this factor");
  public ConfigEntry<float> TimePerProductMultiplier { get; } = BindEx(cfg, Section, 1f,
    "Multiply the time it takes to produce one product by this factor (will not go below 1 second per product).");

  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  public sealed class LocalizationConfig
  {
    string FuelAdded { get; init; } = "{0}: $msg_added {1} {2}x";
    public string FormatFuelAdded(string smelterName, string itemName, int stack) => string.Format(FuelAdded, smelterName, itemName, stack);
    string OreAdded { get; init; } = "{0}: $msg_added {1} {2}x";
    public string FormatOreAdded(string smelterName, string itemName, int stack) => string.Format(OreAdded, smelterName, itemName, stack);
  }
}
