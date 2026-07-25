using BepInEx.Configuration;
using static System.Collections.Specialized.BitVector32;

namespace ServersideQoL.AutoStore;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "AutoStore";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");

  public ConfigEntry<bool> AutoSort { get; } = BindEx(cfg, Section, false, "True to auto sort container inventories");
  public ConfigEntry<MessageTypes> SortedMessageType { get; } = BindEx(cfg, Section, MessageTypes.None,
    "Type of message to show when a container was sorted", AcceptableEnum<MessageTypes>.Default);

  public ConfigEntry<bool> AutoPickup { get; } = Shared.AutoPickup = BindEx(cfg, Section, false,
    "True to automatically put dropped items into containers if they already contain said item");
  public ConfigEntry<float> AutoPickupRange { get; } = BindEx(cfg, Section, ZoneSystem.c_ZoneSize,
    $"Required proximity of a container to a dropped item to be considered as auto pickup target. Can be overridden per chest with the {nameof(ServersideQoL)}.ContainerChests mod.");
  public int? AutoPickupMaxRange => Shared.AutoPickupMaxRange?.Value;
  public ConfigEntry<float> AutoPickupMinPlayerDistance { get; } = BindEx(cfg, Section, 4f,
    "Min distance all player must have to a dropped item for it to be picked up");
  public ConfigEntry<bool> AutoPickupExcludeFodder { get; } = BindEx(cfg, Section, true,
    "True to exclude food items for tames when tames are within search range");
  public ConfigEntry<bool> AutoPickupRequestOwnership { get; } = BindEx(cfg, Section, true,
    "True to make the server request (and receive) ownership of dropped items from the clients before they are picked up. This will reduce the risk of data conflicts (e.g. item duplication) but will drastically decrease performance");
  public ConfigEntry<MessageTypes> PickedUpMessageType { get; } = BindEx(cfg, Section, MessageTypes.None,
    "Type of message to show when a dropped item is added to a container", AcceptableEnum<MessageTypes>.Default);

  public ConfigEntry<Emotes> StackInventoryIntoContainersEmote { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote to stack inventory into containers.
    If a player uses this emote, their inventory will be automatically stacked into nearby containers.
    The rules for which containers are used are the same as for auto pickup.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    For example, on xbox you can use D-Pad down to execute the {Emotes.Sit} emote.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<float> StackInventoryIntoContainersReturnDelay { get; } = BindEx(cfg, Section, 1f, """
    Time in seconds after which items which could not be stacked into containers are returned to the player.
    Increasing this value can help with bad connections.
    """, new AcceptableValueRange<float>(1f, 10f));

  public YamlConfigEntry<Types.Localization> Localization { get; } = BindYaml<Types.Localization>(cfg);

  public static class Types
  {
    public sealed class Localization
    {
      string ContainerSorted { get; init; } = "{0} sorted";
      public string FormatContainerSorted(string containerName) => string.Format(ContainerSorted, containerName);
      string AutoPickup { get; init; } = "{0}: $msg_added {1} {2}x";
      public string FormatAutoPickup(string containerName, string itemName, int stack) => string.Format(AutoPickup, containerName, itemName, stack);
    }
  }
}
