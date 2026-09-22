extern alias ContainerSigns;
using BepInEx.Configuration;
using ContainerSignsPlugin = ContainerSigns::ServersideQoL.ContainerSigns.ContainerSignsPlugin;

namespace ServersideQoL.AutoProcess;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> FeedFromContainers { get; } = Shared.FeedFromContainers = BindEx(cfg, true,
    "True to automatically feed smelters from nearby containers");

  const string DefaultRangeEmoji = "↔️";
  public ConfigEntry<float> FeedFromContainersRange { get; } = BindEx(cfg, 4f, $"""
    Required proximity of a container to a smelter to be used as feeding source.
    Can be overridden per chest by putting '<{nameof(FeedFromContainersRangeSignPrefix)}><Range>' on a chest sign, e.g. '{DefaultRangeEmoji}64'.
      For example, '{DefaultRangeEmoji}64' increase the range of that chest to 64m.
      Only works with automatic chest signs added by the {ContainerSignsPlugin.PluginName} mod.
    """);
  public ConfigEntry<string> FeedFromContainersRangeSignPrefix { get; } = Shared.FeedFromContainersRangeSignPrefix = BindEx(cfg, DefaultRangeEmoji, $"""
    Requires the {ContainerSignsPlugin.PluginName} mod.
    The prefix used to identify the container specifc feed range value in chest sign text.
    """);
  public ConfigEntry<int> FeedFromContainersMaxRange { get; } = Shared.FeedFromContainersMaxRange = BindEx(cfg, (int)ZoneSystem.c_ZoneSize, $"""
    Requires the {ContainerSignsPlugin.PluginName} mod.
    Max feeding range players can set per chest (by putting '<{nameof(FeedFromContainersRangeSignPrefix)}><Range>' on a chest sign)
    """);
  public ConfigEntry<float> FeedFromContainersMinPlayerDistance { get; } = BindEx(cfg, 4f,
    "Min distance all players must have to a processing station");
  public ConfigEntry<int> FeedFromContainersLeaveAtLeastFuel { get; } = BindEx(cfg, 1,
    "Minimum amount of fuel to leave in a container");
  public ConfigEntry<int> FeedFromContainersLeaveAtLeastOre { get; } = BindEx(cfg, 1,
    "Minimum amount of ore to leave in a container");
  public ConfigEntry<MessageTypes> OreOrFuelAddedMessageType { get; } = BindEx(cfg, MessageTypes.None,
    "Type of message to show when ore or fuel is added to a smelter", AcceptableEnum<MessageTypes>.Default);
  public ConfigEntry<float> CapacityMultiplier { get; } = BindEx(cfg, 1f,
    "Multiply a smelter's ore/fuel capacity by this factor");
  public ConfigEntry<float> TimePerProductMultiplier { get; } = BindEx(cfg, 1f,
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
