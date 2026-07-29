using BepInEx.Configuration;

namespace ServersideQoL.AutoMapTables;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "AutoMapTables";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
}
