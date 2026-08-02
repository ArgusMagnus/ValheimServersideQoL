using BepInEx.Configuration;

namespace ServersideQoL.MultiplayerTweaks;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "MultiplayerTweaks";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> ForcePlayerMapPin { get; } = BindEx(cfg, Section, false,
    "True to force player map pins to be visible for all players");
  public ConfigEntry<bool> AssignInteractablesToClosestPlayer { get; } = BindEx(cfg, Section, false, """
    True to assign ownership of some interactable objects (such as smelters or cooking stations) to the closest player.
    This should help avoiding the loss of ore, etc. due to networking issues.
    """);
  public ConfigEntry<bool> AssignMobsToClosestPlayer { get; } = BindEx(cfg, Section, false, """
    True to assign ownership of hostile mobs to the closest player.
    This should help reduce issues with dodging/parrying due to networking issues.
    """);
  public ConfigEntry<bool> AssignShipsToCaptain { get; } = BindEx(cfg, Section, false, """
    True to assign ownership of ships to the player controlling the ship.
    This should help reduce issues with ship control due to networking issues.
    """);

  //public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  //public sealed class LocalizationConfig
  //{
  //  string Prompt { get; init; } = "{0} of {1} players want to sleep.<br>Sit down if you want to sleep as well";
  //  public string FormatPrompt(int sleepingPlayers, int totalPlayers) => string.Format(Prompt, sleepingPlayers, totalPlayers);
  //}
}
