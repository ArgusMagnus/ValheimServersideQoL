using BepInEx.Configuration;

namespace ServersideQoL.Player;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "Player";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");

  static string GetInfiniteXDescription(string action) => Invariant($"""
    True to give players infinite stamina when {action}.
    Player stamina will still be drained, but when nearly depleted, just enough stamina will be restored to continue indefinitely.
    If you want infinite stamina in general, set the global key '{nameof(GlobalKeys.StaminaRate)}' to 0.
    """);

  public ConfigEntry<bool> InfiniteBuildingStamina { get; } = BindEx(cfg, Section, false, GetInfiniteXDescription("building"));
  public ConfigEntry<bool> InfiniteFarmingStamina { get; } = BindEx(cfg, Section, false, GetInfiniteXDescription("farming"));
  public ConfigEntry<bool> InfiniteMiningStamina { get; } = BindEx(cfg, Section, false, GetInfiniteXDescription("mining"));
  public ConfigEntry<bool> InfiniteWoodCuttingStamina { get; } = BindEx(cfg, Section, false, GetInfiniteXDescription("cutting wood"));
  public ConfigEntry<bool> InfiniteEncumberedStamina { get; } = BindEx(cfg, Section, false, GetInfiniteXDescription("encumbered"));
  public ConfigEntry<bool> InfiniteSneakingStamina { get; } = BindEx(cfg, Section, false, GetInfiniteXDescription("sneaking"));
  public ConfigEntry<bool> InfiniteSwimmingStamina { get; } = BindEx(cfg, Section, false, GetInfiniteXDescription("swimming"));

  public ConfigEntry<Emotes> OpenCartEmote { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote to open the inventory of an attached cart.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    You can bind emotes to buttons with chat commands.
    For example, on xbox you can bind the Y-Button to the wave-emote by entering "/bind JoystickButton3 {Emotes.Wave}" in the in-game chat.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<bool> CanSacrificeMegingjord { get; } = BindEx(cfg, Section, false,
      "If true, players can permanently unlock increased carrying weight by sacrificing a megingjord in an obliterator");
  public ConfigEntry<bool> CanSacrificeCryptKey { get; } = BindEx(cfg, Section, false,
      "If true, players can permanently unlock the ability to open sunken crypt doors by sacrificing a crypt key in an obliterator");
  public ConfigEntry<bool> CanSacrificeWishbone { get; } = BindEx(cfg, Section, false,
      "If true, players can permanently unlock the ability to sense hidden objects by sacrificing a wishbone in an obliterator");
  public ConfigEntry<bool> CanSacrificeTornSpirit { get; } = BindEx(cfg, Section, false,
      "If true, players can permanently unlock a wisp companion by sacrificing a torn spirit in an obliterator. WARNING: Wisp companion cannot be unsummoned and will stay as long as this setting is enabled.");

  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class AdvancedConfig
  {
    //public ProcessingDelaysConfig ProcessingDelays { get; init; } = new();

    //public sealed class ProcessingDelaysConfig
    //{
    //  public float TimeSigns { get; init; } = 0.5f;
    //}
  }
}
