using BepInEx.Configuration;

namespace ServersideQoL.TeleportFollowers;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "TeleportFollowers";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<float> MinDistance { get; } = BindEx(cfg, Section, ZoneSystem.c_ZoneSize,
    "Minimum distance from the player at which followers will be teleported to the player's location");
  public ConfigEntry<bool> TakeIntoDungeons { get; } = BindEx(cfg, Section, true,
    $"True to take followers into (and out of) dungeons with the player");

  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class AdvancedConfig
  {
    public TeleportFollowPositioningConfig TeleportFollowPositioning { get; init; } = new(2, 4, 0, 1, 45);
    public sealed record TeleportFollowPositioningConfig(
    float MinDistXZ, float MaxDistXZ, float MinOffsetY, float MaxOffsetY, float HalfArcXZ)
    { TeleportFollowPositioningConfig() : this(default, default, default, default, default) { } }
  }
}
