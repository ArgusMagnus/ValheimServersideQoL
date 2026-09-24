using BepInEx.Configuration;

namespace ServersideQoL.MultiplayerTweaks;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> ForcePlayerMapPin { get; } = BindEx(cfg, false,
    "True to force player map pins to be visible for all players");
  public ConfigEntry<bool> AssignInteractablesToClosestPlayer { get; } = BindEx(cfg, false, """
    True to assign ownership of some interactable objects (such as smelters or cooking stations) to the closest player.
    This should help avoiding the loss of ore, etc. due to networking issues.
    """);
  public ConfigEntry<bool> AssignMobsToClosestPlayer { get; } = BindEx(cfg, false, """
    True to assign ownership of hostile mobs to the closest player.
    This should help reduce issues with dodging/parrying due to networking issues.
    """);
  public ConfigEntry<bool> AssignShipsToCaptain { get; } = BindEx(cfg, false, """
    True to assign ownership of ships to the player controlling the ship.
    This should help reduce issues with ship control due to networking issues.
    """);
  public ConfigEntry<float> MinOwnershipDurationSeconds { get; } = BindEx(cfg, 2f,
    "The minimum time (seconds) after ownership was reassigned on an object before it can be reassigned again",
    new AcceptableValueRange<float>(2, 60));
  public ConfigEntry<float> MaxTimeSinceLastPingSeconds { get; } = BindEx(cfg, 1.5f, """
    The maximum time (seconds) since the last ping from a player was received for that player to be considered as potential new owner.
    This is not the max ping. Even on a perfect connection this value can be up to one second, because vanilla clients only send pings once every second.
    """, new AcceptableValueRange<float>(1, 5));

  //public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  //public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  //public sealed class LocalizationConfig
  //{
  //}

  //public sealed class AdvancedConfig
  //{
  //}
}
