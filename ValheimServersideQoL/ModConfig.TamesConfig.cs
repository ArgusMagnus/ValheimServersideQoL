using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class TamesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MakeCommandable { get; } = cfg.BindEx(section, false, "True to make all tames commandable (like wolves)");
        //public ConfigEntry<bool> FeedFromContainers { get; } = cfg.BindEx(section, false, "True to feed tames from containers");

        public ConfigEntry<MessageTypes> TamingProgressMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of taming progress messages to show", AcceptableEnum<MessageTypes>.Default);
        public ConfigEntry<MessageTypes> GrowingProgressMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of growing progress messages to show", AcceptableEnum<MessageTypes>.Default);
        public ConfigEntry<float> FedDurationMultiplier { get; } = cfg.BindEx(section, 1f, Invariant(
            $"Multiply the time tames stay fed after they have eaten by this factor. {float.PositiveInfinity} to keep them fed indefinitely"));
        public ConfigEntry<float> TamingTimeMultiplier { get; } = cfg.BindEx(section, 1f, """
             Multiply the time it takes to tame a tameable creature by this factor.
             E.g. a value of 0.5 means that the taming time is halved.
             """);
        public ConfigEntry<float> PotionTamingBoostMultiplier { get; } = cfg.BindEx(section, 1f, """
             Multiply the taming boost from the animal whispers potion by this factor.
             E.g. a value of 2 means that the effect of the potion is doubled and the resulting taming time is reduced by a factor of 4 per player.
             """);
        public ConfigEntry<bool> TeleportFollow { get; } = cfg.BindEx(section, false,
            "True to teleport following tames to the players location if the player gets too far away from them");
        public ConfigEntry<bool> TakeIntoDungeons { get; } = cfg.BindEx(section, false,
            $"True to take following tames into (and out of) dungeons with you");
    }
}