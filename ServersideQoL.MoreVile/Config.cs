using BepInEx.Configuration;

namespace ServersideQoL.MoreVile;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");

  public ConfigEntry<float> SpawnMultiplier { get; } = BindEx(cfg, 2f, """
    Multiplier for the spawn rate of Viles.
    Every time a Vile spawns naturally, (SpawnMultiplier - 1) additional Viles are spawned next to it on average.
    Example: 2.5 spawns one additional Vile and a second one with a 50% chance.
    """, new AcceptableValueRange<float>(1f, 10f));

  public ConfigEntry<int> MaxNearby { get; } = BindEx(cfg, 8, """
    No additional Viles are spawned if at least this many Viles are already in the surrounding area.
    0 means no limit.
    """, new AcceptableValueRange<int>(0, 100));

  public ConfigEntry<float> SpawnRadius { get; } = BindEx(cfg, 3f,
    "Maximum distance from the original Vile additional Viles are spawned at",
    new AcceptableValueRange<float>(0f, 20f));

  public ConfigEntry<bool> CopyLevel { get; } = BindEx(cfg, true,
    "True to spawn additional Viles with the same level (stars) as the original Vile, false to spawn them with level 1");
}
