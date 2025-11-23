using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class SleepingConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<int> MinPlayersInBed { get; } = cfg.BindEx(section, 0,
            "Minimum number of players in bed to show the sleep prompt to the other players. 0 to require all players to be in bed (default behavior)");
        public ConfigEntry<int> RequiredPlayerPercentage { get; } = cfg.BindEx(section, 100,
            "Percentage of players that must be in bed or sitting to skip the night", new AcceptableValueRange<int>(0, 100));
        public ConfigEntry<MessageHud.MessageType> SleepPromptMessageType { get; } = cfg.BindEx(section, MessageHud.MessageType.Center,
            "Type of message to show for the sleep prompt", AcceptableEnum<MessageHud.MessageType>.Default);
    }
}