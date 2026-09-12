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

  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);
  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class LocalizationConfig
  { 
    public string BackpackName { get; init; } = "Backpack";
    public string ForbiddenItems { get; init; } = "Backpack cannot contain non-teleportable items";
    string WeightLimitExceeded { get; init; } = "Backpack weight limit ({0}) exceeded";
    public string FormatWeightLimitExceeded(int maxWeight) => string.Format(WeightLimitExceeded, maxWeight);
  }

  public sealed class AdvancedConfig
  {
    public float OpenBackpackDelay { get; init; } = 0.2f;
    public sealed record BackpackOnDeathDropTombStoneConfig(float VerticalOffset, float AutoCollectDistance) { BackpackOnDeathDropTombStoneConfig() : this(default, default) { } }
    public sealed record BackpackOnDeathDropItemsConfig(float ScatterRadius, float VerticalOffset, bool PreventAutoDestroy, bool PreventAutoPickup)
    {
      BackpackOnDeathDropItemsConfig() : this(default, default, default, default) { }
    }

    public BackpackOnDeathDropTombStoneConfig BackpackOnDeathDropTombStone { get; init; } = new(2, 2);
    public BackpackOnDeathDropItemsConfig BackpackOnDeathDropItems { get; init; } = new(2, 1, true, false);
  }
}
