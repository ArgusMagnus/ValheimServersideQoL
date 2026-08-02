using BepInEx.Configuration;

namespace ServersideQoL.JustSleep;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "JustSleep";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<int> MinPlayersInBed { get; } = BindEx(cfg, Section, 1,
      "Minimum number of players in bed to show the sleep prompt to the other players", new AcceptableValueRange<int>(1, 10));
  public ConfigEntry<int> RequiredPlayerPercentage { get; } = BindEx(cfg, Section, 100,
      "Percentage of players that must be in bed or sitting to skip the night", new AcceptableValueRange<int>(0, 100));
  public ConfigEntry<MessageHud.MessageType> SleepPromptMessageType { get; } = BindEx(cfg, Section, MessageHud.MessageType.Center,
      "Type of message to show for the sleep prompt", AcceptableEnum<MessageHud.MessageType>.Default);

  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  public sealed class LocalizationConfig
  {
    string Prompt { get; init; } = "{0} of {1} players want to sleep.<br>Sit down if you want to sleep as well";
    public string FormatPrompt(int sleepingPlayers, int totalPlayers) => string.Format(Prompt, sleepingPlayers, totalPlayers);
  }
}
