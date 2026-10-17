using BepInEx.Configuration;

namespace ServersideQoL.JustSleep;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");
  public ConfigEntry<int> MinPlayersInBed { get; } = BindEx(cfg, 1,
    "Minimum number of players in bed to show the sleep prompt to the other players", new AcceptableValueRange<int>(1, 10));
  public ConfigEntry<int> RequiredPlayerPercentage { get; } = BindEx(cfg, 100,
    "Percentage of players that must be in bed or sitting to skip the night", new AcceptableValueRange<int>(0, 100));
  public ConfigEntry<bool> RequireCampfireForSitting { get; } = BindEx(cfg, false,
    "When enabled, sitting players only count toward the sleep vote when near a lit campfire");
  public ConfigEntry<float> CampfireRange { get; } = BindEx(cfg, 10f,
    "Maximum distance from a lit campfire for a sitting player to count toward the sleep vote", new AcceptableValueRange<float>(1f, 30f));
  public ConfigEntry<MessageHud.MessageType> SleepPromptMessageType { get; } = BindEx(cfg, MessageHud.MessageType.Center,
    "Type of message to show for the sleep prompt", AcceptableEnum<MessageHud.MessageType>.Default);

  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  public sealed class LocalizationConfig
  {
    string Prompt { get; init; } = "{0} of {1} players want to sleep.<br>Sit down if you want to sleep as well";
    string CampfirePrompt { get; init; } = "<br>Sitting counts only within {0}m of a lit campfire.";
    public string FormatPrompt(int sleepingPlayers, int totalPlayers, bool requireCampfire, float campfireRange)
      => string.Format(Prompt, sleepingPlayers, totalPlayers)
        + (requireCampfire ? string.Format(CampfirePrompt, campfireRange) : "");
  }
}
