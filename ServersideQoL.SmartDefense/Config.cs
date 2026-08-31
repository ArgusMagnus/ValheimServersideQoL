using BepInEx.Configuration;

namespace ServersideQoL.SmartDefense;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "SmartDefense";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");

  public ConfigEntry<bool> DontTargetPlayers { get; } = BindEx(cfg, Section, true,
    "True to stop ballistas from targeting players");
  public ConfigEntry<bool> DontTargetTames { get; } = BindEx(cfg, Section, true,
    "True to stop ballistas from targeting tames");
  public ConfigEntry<bool> LoadFromContainers { get; } = BindEx(cfg, Section, true,
    "True to automatically load ballistas from containers");
  public ConfigEntry<float> LoadFromContainersRange { get; } = BindEx(cfg, Section, 4f,
    "Required proximity of a container to a ballista to be used as ammo source");
  public int? FeedFromContainersMaxRange => Shared.FeedFromContainersMaxRange?.Value;
  public ConfigEntry<float> LoadFromContainersMinPlayerDistance { get; } = BindEx(cfg, Section, 4f,
    "Min distance all players must have to a ballista");
  public ConfigEntry<MessageTypes> AmmoAddedMessageType { get; } = BindEx(cfg, Section, MessageTypes.None,
    "Type of message to show when ammo is added to a turret", AcceptableEnum<MessageTypes>.Default);
  public ConfigEntry<MessageTypes> NoAmmoMessageType { get; } = BindEx(cfg, Section, MessageTypes.None,
    "Type of message to show when there is no ammo to add to a turret", AcceptableEnum<MessageTypes>.Default);

  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  public sealed class LocalizationConfig
  {
    string AmmoAdded { get; init; } = "{0}: $msg_added {1} {2}x";
    public string FormatAmmoAdded(string turretName, string itemName, int count) => string.Format(AmmoAdded, turretName, itemName, count);
    public string NoAmmoFound { get; init; } = "<color=red>$msg_noturretammo";
  }
}
