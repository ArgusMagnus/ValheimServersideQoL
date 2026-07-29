using BepInEx.Configuration;

namespace ServersideQoL.AutoMapTables;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "AutoMapTables";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> AutoUpdatePortals { get; } = BindEx(cfg, Section, true,
      "True to update map tables with portal pins");
  public ConfigEntry<string> AutoUpdatePortalsExclude { get; } = BindEx(cfg, Section, "",
      "Portals with a tag that matches this filter are not added to map tables");
  public ConfigEntry<string> AutoUpdatePortalsInclude { get; } = BindEx(cfg, Section, "*",
      "Only portals with a tag that matches this filter are added to map tables");
  public ConfigEntry<bool> AutoUpdateShips { get; } = BindEx(cfg, Section, true,
      "True to update map tables with ship pins");
  public ConfigEntry<MessageTypes> UpdatedMessageType { get; } = BindEx(cfg, Section, MessageTypes.None,
      "Type of message to show when a map table is updated", AcceptableEnum<MessageTypes>.Default);

  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);
  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class LocalizationConfig
  {
    public string Updated { get; init; } = "$msg_mapsaved";
  }

  public sealed class AdvancedConfig
  {
    public float MapTableUpdateInterval { get; init; } = 5;
  }
}
