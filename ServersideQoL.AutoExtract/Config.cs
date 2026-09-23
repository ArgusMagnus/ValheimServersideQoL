extern alias ContainerSigns;
using BepInEx.Configuration;
using ContainerSignsPlugin = ContainerSigns::ServersideQoL.ContainerSigns.ContainerSignsPlugin;

namespace ServersideQoL.AutoExtract;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> ExtractBeehives { get; } = BindEx(cfg, true,
    "True to automatically extract honey, feathers, etc. from beehives and bird nests into nearby containers");
  public ConfigEntry<bool> ExtractSapCollectors { get; } = BindEx(cfg, true,
    "True to automatically extract sap from sap collectors into nearby containers");
  const string DefaultPickupRangeEmoji = "🧲";
  public ConfigEntry<float> ExtractRange { get; } = BindEx(cfg, 4f, $"""
    Required proximity of a container to a beehive, bird nest or sap collector to be used as extraction target.
    Containers which already hold the extracted item are preferred over other containers.
    Can be overridden per chest by putting '<AutoPickupRangeSignPrefix><Range>' on a chest sign, e.g. '{DefaultPickupRangeEmoji}16'.
      For example, '{DefaultPickupRangeEmoji}0' will exclude that chest.
      Only works with automatic chest signs added by the {ContainerSignsPlugin.PluginName} mod (requires the AutoStore mod, which owns the AutoPickupRangeSignPrefix option).
    """);
  public int? AutoPickupMaxRange => Shared.AutoStorePickupMaxRange?.Value;
  public ConfigEntry<float> ExtractMinPlayerDistance { get; } = BindEx(cfg, 4f,
    "Min distance all players must have to a beehive, bird nest or sap collector");
  public ConfigEntry<MessageTypes> ExtractedMessageType { get; } = BindEx(cfg, MessageTypes.None,
    "Type of message to show when items are extracted from a beehive, bird nest or sap collector", AcceptableEnum<MessageTypes>.Default);

  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  public sealed class LocalizationConfig
  {
    string Extracted { get; init; } = "{0}: {1} {2}x";
    public string FormatExtracted(string stationName, string itemName, int stack) => string.Format(Extracted, stationName, itemName, stack);
  }
}
