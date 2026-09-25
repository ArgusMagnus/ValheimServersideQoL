using BepInEx.Configuration;

namespace ServersideQoL.AutoPortalHub;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true, """
    Enables/disables the entire mod.
    True to automatically generate a portal hub.
    Placed portals which don't have a paired portal in the world will be connected to the portal hub.
    """);

  public ConfigEntry<string> Exclude { get; } = BindEx(cfg, "",
    "Portals with a tag that matches this filter are not connected to the portal hub");
  public ConfigEntry<string> Include { get; } = BindEx(cfg, "*",
    "Only portals with a tag that matches this filter are connected to the portal hub");
  public ConfigEntry<bool> AutoNameNewPortals { get; } = BindEx(cfg, false,
        "True to automatically name new portals");
  public ConfigEntry<string> AutoNameNewPortalsFormat { get; } = BindEx(cfg, "{0} {1:D2}",
    "Format string for auto-naming portals, the first argument is the biome name, the second is an automatically incremented integer",
    new AcceptableFormatString(["Test", 0]));

  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class AdvancedConfig
  {
    public float SignVerticalOffset { get; init; } = 2;
  }
}
