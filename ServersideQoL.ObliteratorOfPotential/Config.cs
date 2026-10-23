using BepInEx.Configuration;

namespace ServersideQoL.ObliteratorOfPotential;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");

  public ConfigEntry<bool> PlaceNearForgeOfPotential { get; } = BindEx(cfg, true,
    "True to place an Obliterator of Potential near the Forge of Potential");

  public ConfigEntry<bool> EveryObliteratorCanUpgrade { get; } = BindEx(cfg, false,
    "True to give every obliterator the power to upgrade gear, not just the one placed near the Forge of Potential");
}
