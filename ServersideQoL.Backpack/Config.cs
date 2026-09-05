using BepInEx.Configuration;

namespace ServersideQoL.Backpack;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "Backpack";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");

  public ConfigEntry<Emotes> OpenBackpackEmote { get; } = BindEx(cfg, Section, Emotes.Wave, $"""
    Emote to open the backpack.
    If a player uses this emote, a virtual container acting as their backpack will open.
    {AnyEmote} to use any emote as trigger.
    You can bind emotes to buttons with chat commands.
    For example, on xbox you can bind the Y-Button to the wave-emote by entering "/bind JoystickButton3 {Emotes.Wave}" in the in-game chat.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<int> InitialBackpackSlots { get; } = BindEx(cfg, Section, 4, "Initial available slots in the backpack");
  public ConfigEntry<int> AdditionalBackpackSlotsPerDefeatedBoss { get; } = BindEx(cfg, Section, 4, "Additional backpack slots per defeated boss");
  public ConfigEntry<int> MaxBackpackWeight { get; } = BindEx(cfg, Section, 0, "Maximum backpack weight. 0 for no limit.");
  public ConfigEntry<BackPackOnDeathOptions> BackpackOnDeath { get; } = BindEx(cfg, Section, BackPackOnDeathOptions.SameAsInventory, "What happens to backpack contents on player death");

  public enum BackPackOnDeathOptions
  {
    SameAsInventory,
    Keep,
    Destroy,
    DropTombStone,
    DropItems
  }

  //public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);
  //public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  //public sealed class LocalizationConfig
  //{
  //  string ContainerSorted { get; init; } = "{0} sorted";
  //  public string FormatContainerSorted(string containerName) => string.Format(ContainerSorted, containerName);
  //  string AutoPickup { get; init; } = "{0}: $msg_added {1} {2}x";
  //  public string FormatAutoPickup(string containerName, string itemName, int stack) => string.Format(AutoPickup, containerName, itemName, stack);
  //}

  //public sealed class AdvancedConfig
  //{
  //  public ProcessingDelaysConfig ProcessingDelays { get; init; } = new();

  //  public sealed class ProcessingDelaysConfig
  //  {
  //    public float AfterItemDropOwnershipRequest { get; init; } = 0.1f;
  //    public float StackContainerWhenMovingItems { get; init; } = 0.1f;
  //  }
  //}
}
