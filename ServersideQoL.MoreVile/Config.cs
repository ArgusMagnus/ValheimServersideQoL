using BepInEx.Configuration;

namespace ServersideQoL.MoreVile;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");

  public ConfigEntry<float> SpawnChanceMultiplier { get; } = BindEx(cfg, 2f, """
    Multiplier for the chance of Viles to spawn naturally.
    Example: 2 doubles the chance, 1 leaves it unchanged.
    Spawn conditions (biome, time of day, required boss progress, etc.) are the same as in vanilla.
    """, new AcceptableValueRange<float>(1f, 20f));

  public ConfigEntry<float> MaxSpawnedMultiplier { get; } = BindEx(cfg, 1f, """
    Multiplier for the maximum number of Viles which can be around a player before no more spawn naturally.
    With a higher spawn chance, this limit is reached sooner, so you might want to increase it as well.
    """, new AcceptableValueRange<float>(1f, 10f));
}
