using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class PlayersConfig(ConfigFile cfg, string section)
    {
        static string GetInfiniteXDescription(string action) => Invariant($"""
                True to give players infinite stamina when {action}.
                Player stamina will still be drained, but when nearly depleted, just enough stamina will be restored to continue indefinitely.
                If you want infinite stamina in general, set the global key '{nameof(GlobalKeys.StaminaRate)}' to 0.
                """);

        public ConfigEntry<bool> InfiniteBuildingStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("building"));
        public ConfigEntry<bool> InfiniteFarmingStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("farming"));
        public ConfigEntry<bool> InfiniteMiningStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("mining"));
        public ConfigEntry<bool> InfiniteWoodCuttingStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("cutting wood"));
        public ConfigEntry<bool> InfiniteEncumberedStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("encumbered"));
        public ConfigEntry<bool> InfiniteSneakingStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("sneaking"));
        public ConfigEntry<bool> InfiniteSwimmingStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("swimming"));

        public const Emotes DisabledEmote = (Emotes)(-1);
        public const Emotes AnyEmote = (Emotes)(-2);
        public ConfigEntry<Emotes> StackInventoryIntoContainersEmote { get; } = cfg.BindEx(section, DisabledEmote, $"""
                Emote to stack inventory into containers.
                If a player uses this emote, their inventory will be automatically stacked into nearby containers.
                The rules for which containers are used are the same as for auto pickup.
                {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
                For example, on xbox you can use D-Pad down to execute the {Emotes.Sit} emote.
                If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
                """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

        public ConfigEntry<float> StackInventoryIntoContainersReturnDelay { get; } = cfg.BindEx(section, 1f, """
                Time in seconds after which items which could not be stacked into containers are returned to the player.
                Increasing this value can help with bad connections.
                """, new AcceptableValueRange<float>(1f, 10f));

        public ConfigEntry<Emotes> OpenBackpackEmote { get; } = cfg.BindEx(section, DisabledEmote, $"""
                Emote to open the backpack.
                If a player uses this emote, a virtual container acting as their backpack will open.
                {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
                You can bind emotes to buttons with chat commands.
                For example, on xbox you can bind the Y-Button to the wave-emote by entering "/bind JoystickButton3 {Emotes.Wave}" in the in-game chat.
                If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
                """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

        public ConfigEntry<int> InitialBackpackSlots { get; } = cfg.BindEx(section, 4, "Initial available slots in the backpack");
        public ConfigEntry<int> AdditionalBackpackSlotsPerDefeatedBoss { get; } = cfg.BindEx(section, 4, "Additional backpack slots per defeated boss");
        public ConfigEntry<int> MaxBackpackWeight { get; } = cfg.BindEx(section, 0, "Maximum backpack weight. 0 for no limit.");

        public ConfigEntry<bool> CanSacrificeMegingjord { get; } = cfg.BindEx(section, false,
            "If true, players can permanently unlock increased carrying weight by sacrificing a megingjord in an obliterator");
        public ConfigEntry<bool> CanSacrificeCryptKey { get; } = cfg.BindEx(section, false,
            "If true, players can permanently unlock the ability to open sunken crypt doors by sacrificing a crypt key in an obliterator");
        public ConfigEntry<bool> CanSacrificeWishbone { get; } = cfg.BindEx(section, false,
            "If true, players can permanently unlock the ability to sense hidden objects by sacrificing a wishbone in an obliterator");
        public ConfigEntry<bool> CanSacrificeTornSpirit { get; } = cfg.BindEx(section, false,
            "If true, players can permanently unlock a wisp companion by sacrificing a torn spirit in an obliterator. WARNING: Wisp companion cannot be unsummoned and will stay as long as this setting is enabled.");
    }
}