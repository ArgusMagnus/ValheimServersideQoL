extern alias ContainerSigns;
using BepInEx.Configuration;
using ContainerAndSignProcessor = ContainerSigns::ServersideQoL.ContainerSigns.ContainerAndSignProcessor;
using ContainerSignsPlugin = ContainerSigns::ServersideQoL.ContainerSigns.ContainerSignsPlugin;

namespace ServersideQoL.AutoStore;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");

  public ConfigEntry<bool> AutoSort { get; } = BindEx(cfg, true, "True to auto sort container inventories");
  public ConfigEntry<MessageTypes> SortedMessageType { get; } = BindEx(cfg, MessageTypes.None,
    "Type of message to show when a container was sorted", AcceptableEnum<MessageTypes>.Default);

  public ConfigEntry<bool> AutoPickup { get; } = Shared.AutoPickup = BindEx(cfg, true,
    "True to automatically put dropped items into containers if they already contain said item");
  public ConfigEntry<float> AutoPickupRange { get; } = BindEx(cfg, ZoneSystem.c_ZoneSize, $"""
    Required proximity of a container to a dropped item to be considered as auto pickup target.
    Can be overridden per chest by putting '{ContainerAndSignProcessor.PickupRangeEmoji}<Range>' on a chest sign, e.g. '{ContainerAndSignProcessor.PickupRangeEmoji}16'.
      For example, '{ContainerAndSignProcessor.PickupRangeEmoji}0' will disable auto pickup/stacking from player inventory into that chest.
      Only works with automatic chest signs added by the {ContainerSignsPlugin.PluginName} mod.
    """);    
  public ConfigEntry<int> AutoPickupMaxRange { get; } = Shared.AutoPickupMaxRange = BindEx(cfg, (int)ZoneSystem.c_ZoneSize, $"""
    Requires the {ContainerSignsPlugin.PluginName} mod. 
    Max auto pickup range players can set per chest (by putting '{ContainerAndSignProcessor.PickupRangeEmoji}<Range>' on a chest sign).
    """);
  public ConfigEntry<float> AutoPickupMinPlayerDistance { get; } = BindEx(cfg, 4f,
    "Min distance all players must have to a dropped item for it to be picked up");
  public ConfigEntry<ConfigArray<ItemDrop.ItemData.ItemType>> AutoPickupExcludeItemTypes { get; } = BindEx(cfg, ConfigArray<ItemDrop.ItemData.ItemType>.Empty,
    "Item types which will be excluded from auto pickup", __acceptableExcludeItemTypes);
  public ConfigEntry<bool> AutoPickupExcludeFodder { get; } = BindEx(cfg, true,
    "True to exclude food items for tames when tames are within search range");
  public ConfigEntry<bool> AutoPickupRequestOwnership { get; } = BindEx(cfg, true,
    "True to make the server request (and receive) ownership of dropped items from the clients before they are picked up. This will reduce the risk of data conflicts (e.g. item duplication) but will drastically decrease performance");
  public ConfigEntry<MessageTypes> PickedUpMessageType { get; } = BindEx(cfg, MessageTypes.InWorld,
    "Type of message to show when a dropped item is added to a container", AcceptableEnum<MessageTypes>.Default);
  public ConfigEntry<bool> ShowContainerModifiedEffect { get; } = BindEx(cfg, true,
    "True to show an effect when a container is modified due to auto pickup or player inventory stacking");

  public ConfigEntry<Emotes> StackInventoryIntoContainersEmote { get; } = BindEx(cfg, Emotes.Sit, $"""
    Emote to stack inventory into containers.
    If a player uses this emote, their inventory will be automatically stacked into nearby containers.
    The rules for which containers are used are the same as for auto pickup.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    For example, on xbox you can use D-Pad down to execute the {Emotes.Sit} emote.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));
  public ConfigEntry<ConfigArray<ItemDrop.ItemData.ItemType>> StackInventoryIntoContainersExcludeItemTypes { get; } = BindEx(cfg, ConfigArray<ItemDrop.ItemData.ItemType>.Empty,
    "Item types which will be excluded from stacking into containers via emote", __acceptableExcludeItemTypes);
  public ConfigEntry<float> StackInventoryIntoContainersReturnDelay { get; } = BindEx(cfg, 1f, """
    Time in seconds after which items which could not be stacked into containers are returned to the player.
    Increasing this value can help with bad connections.
    """, new AcceptableValueRange<float>(1f, 10f));

  static readonly AcceptableArrayValues<ItemDrop.ItemData.ItemType> __acceptableExcludeItemTypes = new([.. ObjectDB.instance.m_items
    .Select(static x => x.GetComponent<ItemDrop>()?.m_itemData.m_shared)
    .Where(static x => x is { m_icons.Length: > 0 })
    .Select(static x => x!.m_itemType)
    .Distinct()
    .OrderBy(static x => x.ToString())]);

  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);
  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class LocalizationConfig
  {
    string ContainerSorted { get; init; } = "{0} sorted";
    public string FormatContainerSorted(string containerName) => string.Format(ContainerSorted, containerName);
    string AutoPickup { get; init; } = "{0}: $msg_added {1} {2}x";
    public string FormatAutoPickup(string containerName, string itemName, int stack) => string.Format(AutoPickup, containerName, itemName, stack);
    string Stacked { get; init; } = "{0}: $msg_added {1} {2}x";
    public string FormatStacked(string containerName, string itemName, int stack) => string.Format(Stacked, containerName, itemName, stack);
  }

  public sealed class AdvancedConfig
  {
    public string ContainerModifiedEffectPrefabName { get; init; } = "fx_Potion_frostresist";
    public ProcessingDelaysConfig ProcessingDelays { get; init; } = new();

    public sealed class ProcessingDelaysConfig
    {
      public float AfterItemDropOwnershipRequest { get; init; } = 0.1f;
      public float StackContainerWhenMovingItems { get; init; } = 0.1f;
    }
  }
}
