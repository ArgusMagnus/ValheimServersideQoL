using BepInEx.Configuration;

namespace ServersideQoL.Recycler;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");

  public ConfigEntry<int> ReturnPercentage { get; } = BindEx(cfg, 50, """

    """, new AcceptableValueRange<int>(1, 100));
}
