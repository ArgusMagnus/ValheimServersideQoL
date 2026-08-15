using BepInEx.Configuration;

namespace ServersideQoL.AdminOptions;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "AdminBuildOptions";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");

  public ConfigEntry<Emotes> ToggleDisableRainDamageEmote { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote admins can use to toggle disabling rain damage for newly built pieces.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<Emotes> ToggleDisableSupportRequirements { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote admins can use to toggle disabling support requirements for newly built pieces.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<Emotes> ToggleMakeIndestructible { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote admins can use to toggle making newly built pieces indestructible.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<Emotes> ToggleNoWorkbench { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote admins can use to toggle the workbench requirement for building.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<Emotes> ToggleDungeonBuild { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote admins can use to toggle building in dungeons.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<Emotes> ToggleNoBuildCost { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote admins can use to toggle no build cost.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<Emotes> ToggleAllPiecesUnlocked { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote admins can use to toggle unlocking all building pieces.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<Emotes> CycleLevelGroundMode { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote admins can use to cycle between different modes when using the Hoe's "Level Ground" option.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<Emotes> DemigodMode { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote admins can use to toggle receiving no damage from enemies (environmental hazards can still hurt the player though).
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  public ConfigEntry<Emotes> InfiniteStamina { get; } = BindEx(cfg, Section, DisabledEmote, $"""
    Emote admins can use to toggle having infinite stamina.
    {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
    If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
    """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

  //public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);
  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  //public sealed class LocalizationConfig
  //{
  //}

  public sealed class AdvancedConfig
  {
    public float ResetTerrainRadius { get; init; } = 3;
  }
}
