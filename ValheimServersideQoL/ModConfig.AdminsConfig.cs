
using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class AdminsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<Emotes> ToggleDisableRainDamageEmote { get; } = cfg.BindEx(section, DisabledEmote, $"""
            Emote admins can use to toggle disabling rain damage for newly built pieces.
            {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
            If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
            """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

        public ConfigEntry<Emotes> ToggleDisableSupportRequirements { get; } = cfg.BindEx(section, DisabledEmote, $"""
            Emote admins can use to toggle disabling support requirements for newly built pieces.
            {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
            If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
            """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

        public ConfigEntry<Emotes> ToggleMakeIndestructible { get; } = cfg.BindEx(section, DisabledEmote, $"""
            Emote admins can use to toggle making newly built pieces indestructible.
            {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
            If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
            """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

        public ConfigEntry<Emotes> ToggleNoWorkbench { get; } = cfg.BindEx(section, DisabledEmote, $"""
            Emote admins can use to toggle the workbench requirement for building.
            {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
            If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
            """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

        public ConfigEntry<Emotes> ToggleDungeonBuild { get; } = cfg.BindEx(section, DisabledEmote, $"""
            Emote admins can use to toggle building in dungeons.
            {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
            If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
            """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));
        
        public ConfigEntry<Emotes> ToggleNoBuildCost { get; } = cfg.BindEx(section, DisabledEmote, $"""
            Emote admins can use to toggle no build cost.
            {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
            If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
            """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

        public ConfigEntry<Emotes> ToggleAllPiecesUnlocked { get; } = cfg.BindEx(section, DisabledEmote, $"""
            Emote admins can use to toggle unlocking all building pieces.
            {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
            If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
            """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

        public ConfigEntry<Emotes> CycleLevelGroundMode { get; } = cfg.BindEx(section, DisabledEmote, $"""
            Emote admins can use to cycle between different modes when using the Hoe's "Level Ground" option.
            {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
            If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
            """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));
    }
}