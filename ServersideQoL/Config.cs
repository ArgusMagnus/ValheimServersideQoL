using BepInEx.Configuration;

namespace ServersideQoL;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "General";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> DiagnosticLogs { get; } = BindEx(cfg, Section, false,
    "Enables/disables diagnostic logs");
  public ConfigEntry<bool> IgnoreGameVersionCheck { get; } = BindEx(cfg, Section, true,
    "True to ignore the game version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
  public ConfigEntry<bool> IgnoreNetworkVersionCheck { get; } = BindEx(cfg, Section, false,
    "True to ignore the network version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
  public ConfigEntry<bool> IgnoreItemDataVersionCheck { get; } = BindEx(cfg, Section, false,
    "True to ignore the item data version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
  public ConfigEntry<bool> IgnoreWorldVersionCheck { get; } = BindEx(cfg, Section, false,
    "True to ignore the world version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
  public ConfigEntry<float> FarMessageRange { get; } = BindEx(cfg, Section, ZoneSystem.c_ZoneSize,
    $"Max distance a player can have to a modified object to receive messages of type {MessageTypes.TopLeftFar} or {MessageTypes.CenterFar}");

  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class AdvancedConfig
  {
    public ProcessingDelaysConfig ProcessingDelays { get; init; } = new();
    public ContainersConfig Containers { get; init; } = new();
    public PlayersConfig Players { get; init; } = new();

    public sealed class ProcessingDelaysConfig
    {
      public float WhenNoNearbyPlayers { get; init; } = 2;
      public float AfterContainerOwnershipRequest { get; init; } = 0.1f;
    }

    public sealed class ContainersConfig
    {
      public float MinOwnershipRequestInterval { get; init; } = 1;
    }

    public sealed class PlayersConfig
    {
      public float UpdateStaminaInterval { get; init; } = 0.2f;
    }
  }
}
