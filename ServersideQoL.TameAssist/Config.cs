extern alias ContainerSigns;
using BepInEx.Configuration;
using ContainerSignsPlugin = ContainerSigns::ServersideQoL.ContainerSigns.ContainerSignsPlugin;

namespace ServersideQoL.TameAssist;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> MakeCommandable { get; } = BindEx(cfg, false, "True to make all tames commandable (like wolves)");
  public ConfigEntry<MessageTypes> TamingProgressMessageType { get; } = BindEx(cfg, MessageTypes.InWorld,
    "Type of taming progress messages to show", AcceptableEnum<MessageTypes>.Default);
  public ConfigEntry<MessageTypes> GrowingProgressMessageType { get; } = BindEx(cfg, MessageTypes.InWorld,
    "Type of growing progress messages to show", AcceptableEnum<MessageTypes>.Default);
  public ConfigEntry<float> FedDurationMultiplier { get; } = BindEx(cfg, 1f, Invariant(
    $"Multiply the time tames stay fed after they have eaten by this factor. {float.PositiveInfinity} to keep them fed indefinitely"));
  public ConfigEntry<float> TamingTimeMultiplier { get; } = BindEx(cfg, 1f, """
    Multiply the time it takes to tame a tameable creature by this factor.
    E.g. a value of 0.5 means that the taming time is halved.
    """);
  public ConfigEntry<float> PotionTamingBoostMultiplier { get; } = BindEx(cfg, 1f, """
    Multiply the taming boost from the animal whispers potion by this factor.
    E.g. a value of 2 means that the effect of the potion is doubled and the resulting taming time is reduced by a factor of 4 per player.
    """);
  public ConfigEntry<bool> TeleportFollowers { get; } = BindEx(cfg, true,
      "True to teleport following tames and summons to the players location if the player gets too far away from them");
  public ConfigEntry<float> TeleportFollowersMinDistance { get; } = BindEx(cfg, ZoneSystem.c_ZoneSize,
    "Minimum distance from the player at which followers will be teleported to the player's location");
  public ConfigEntry<bool> TakeIntoDungeons { get; } = BindEx(cfg, true,
    $"True to take followers into (and out of) dungeons with the player");
  public ConfigEntry<bool> FeedFromContainers { get; } = Shared.TameAssistFeedFromContainers = BindEx(cfg, true,
    "True to automatically feed tames from nearby containers");
  const string DefaultRangeEmoji = "🐗";
  public ConfigEntry<float> FeedFromContainersRange { get; } = BindEx(cfg, 0f, $"""
    Required proximity of a container to a tame to be used as feeding source.
    Can be overridden per chest by putting '<{nameof(FeedFromContainersRangeSignPrefix)}><Range>' on a chest sign, e.g. '{DefaultRangeEmoji}64'.
      For example, '{DefaultRangeEmoji}64' increase the range of that chest to 64m.
      Only works with automatic chest signs added by the {ContainerSignsPlugin.PluginName} mod.
    """);
  public ConfigEntry<string> FeedFromContainersRangeSignPrefix { get; } = Shared.TameAssistFeedFromContainersRangeSignPrefix = BindEx(cfg, DefaultRangeEmoji, $"""
    Requires the {ContainerSignsPlugin.PluginName} mod.
    The prefix used to identify the container specifc feed range value in chest sign text.
    """);
  public ConfigEntry<int> FeedFromContainersMaxRange { get; } = Shared.TameAssistFeedFromContainersMaxRange = BindEx(cfg, (int)ZoneSystem.c_ZoneSize, $"""
    Requires the {ContainerSignsPlugin.PluginName} mod.
    Max feeding range players can set per chest (by putting '<{nameof(FeedFromContainersRangeSignPrefix)}><Range>' on a chest sign)
    """);
  public ConfigEntry<int> FeedFromContainersLeaveAtLeast { get; } = BindEx(cfg, 1,
    "Minimum amount of any given food item type to leave in a container");
  public ConfigEntry<MessageTypes> TameFedMessageType { get; } = BindEx(cfg, MessageTypes.None,
    "Type of message to show when a time was fed from a container", AcceptableEnum<MessageTypes>.Default);

  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  public sealed class AdvancedConfig
  {
    public TeleportFollowPositioningConfig TeleportFollowPositioning { get; init; } = new(2, 4, 0, 1, 45);
    public sealed record TeleportFollowPositioningConfig(
    float MinDistXZ, float MaxDistXZ, float MinOffsetY, float MaxOffsetY, float HalfArcXZ)
    { TeleportFollowPositioningConfig() : this(default, default, default, default, default) { } }
  }

  public sealed class LocalizationConfig
  {
    string Growing { get; init; } = "$caption_growing {0}%";
    public string FormatGrowing(int percent) => string.Format(Growing, percent);
    string Taming { get; init; } = "$hud_tameness {0:P0}";
    string TamingHungry { get; init; } = "$hud_tameness {0:P0}, $hud_tamehungry";
    public string FormatTaming(float tameness, bool isHungry) => isHungry ? string.Format(TamingHungry, tameness) : string.Format(Taming, tameness);
    string TameFed { get; init; } = "{0}: $msg_added {1}";
    public string FormatTameFed(string tameName, string itemName) => string.Format(TameFed, tameName, itemName);
  }
}
