using BepInEx.Configuration;
using System.Runtime.CompilerServices;

namespace ServersideQoL.SmartDefense;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, "SmartDefense", true,
    "Enables/disables the entire mod");

  public TurretsConfig Turrets { get; } = new(cfg);
  public TrapsConfig Traps { get; } = new(cfg);

  public sealed class TurretsConfig(ConfigFile cfg, [CallerMemberName] string section = default!)
  {
    public ConfigEntry<bool> DontTargetPlayers { get; } = BindEx(cfg, section, true,
      "True to stop ballistas from targeting players");
    public ConfigEntry<bool> DontTargetTames { get; } = BindEx(cfg, section, true,
      "True to stop ballistas from targeting tames");
    public ConfigEntry<bool> LoadFromContainers { get; } = BindEx(cfg, section, true,
      "True to automatically load ballistas from containers");
    public ConfigEntry<float> LoadFromContainersRange { get; } = BindEx(cfg, section, 4f,
      "Required proximity of a container to a ballista to be used as ammo source");
    public int? FeedFromContainersMaxRange => Shared.FeedFromContainersMaxRange?.Value;
    public ConfigEntry<float> LoadFromContainersMinPlayerDistance { get; } = BindEx(cfg, section, 4f,
      "Min distance all players must have to a ballista");
    public ConfigEntry<MessageTypes> AmmoAddedMessageType { get; } = BindEx(cfg, section, MessageTypes.None,
      "Type of message to show when ammo is added to a turret", AcceptableEnum<MessageTypes>.Default);
    public ConfigEntry<MessageTypes> NoAmmoMessageType { get; } = BindEx(cfg, section, MessageTypes.None,
      "Type of message to show when there is no ammo to add to a turret", AcceptableEnum<MessageTypes>.Default);
  }

  public sealed class TrapsConfig(ConfigFile cfg, [CallerMemberName] string section = default!)
  {
    public ConfigEntry<bool> DisableTriggeredByPlayers { get; } = BindEx(cfg, section, true,
      "True to stop traps from being triggered by players");
    public ConfigEntry<bool> DisableFriendlyFire { get; } = BindEx(cfg, section, true,
      "True to stop traps from damaging players and tames. Does not work reliably (yet).");
    public ConfigEntry<float> SelfDamageMultiplier { get; } = BindEx(cfg, section, 1f,
      "Multiply the damage the trap takes when it is triggered by this factor. 0 to make the trap take no damage",
      new AcceptableValueRange<float>(0, float.PositiveInfinity));
    public ConfigEntry<bool> AutoRearm { get; } = BindEx(cfg, section, true,
      "True to automatically rearm traps when they are triggered");
  }

  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  public sealed class LocalizationConfig
  {
    public TurretsConfig Turrets { get; init; } = new();

    public sealed class TurretsConfig
    {
      string AmmoAdded { get; init; } = "{0}: $msg_added {1} {2}x";
      public string FormatAmmoAdded(string turretName, string itemName, int count) => string.Format(AmmoAdded, turretName, itemName, count);
      public string NoAmmoFound { get; init; } = "<color=red>$msg_noturretammo";
    }
  }
}
