using BepInEx.Configuration;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Valheim.ServersideQoL.Processors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace Valheim.ServersideQoL;

sealed partial record ModConfig(ConfigFile ConfigFile)
{
    public GeneralConfig General { get; } = new(ConfigFile, "A - General");
    public SignsConfig Signs { get; } = new(ConfigFile, "B - Signs");
    public MapTableConfig MapTables { get; } = new(ConfigFile, "B - Map Tables");
    public TamesConfig Tames { get; } = new(ConfigFile, "B - Tames");
    public CreaturesConfig Creatures { get; } = new(ConfigFile, "B - Creatures");
    public SummonsConfig Summons { get; } = new(ConfigFile, "B - Summons");
    public HostileSummonsConfig HostileSummons { get; } = new(ConfigFile, "B - Hostile Summons");
    public FireplacesConfig Fireplaces { get; } = new(ConfigFile, "B - Fireplaces");
    public ContainersConfig Containers { get; } = new(ConfigFile, "B - Containers");
    public SmeltersConfig Smelters { get; } = new(ConfigFile, "B - Smelters");
    public WindmillsConfig Windmills { get; } = new(ConfigFile, "B - Windmills");
    public CartsConfig Carts { get; } = new(ConfigFile, "B - Carts");
    public DoorsConfig Doors { get; } = new(ConfigFile, "B - Doors");
    public PlayersConfig Players { get; } = new(ConfigFile, "B - Players");
    public TurretsConfig Turrets { get; } = new(ConfigFile, "B - Turrets");
    public WearNTearConfig WearNTear { get; } = new(ConfigFile, "B - Build Pieces");
    public CraftingStationsConfig CraftingStations { get; } = new(ConfigFile, "B - Crafting Stations");
    public TradersConfig Traders { get; } = new(ConfigFile, "B - Traders");
    public PlantsConfig Plants { get; } = new(ConfigFile, "B - Plants");
    public TrapsConfig Traps { get; } = new(ConfigFile, "B - Traps");
    public PortalHubConfig PortalHub { get; } = new(ConfigFile, "B - Portal Hub");
    public WorldConfig World { get; } = new(ConfigFile, "B - World");
    public TrophySpawnerConfig TrophySpawner { get; } = new(ConfigFile, "B - Trophy Spawner");
    public NonTeleportableItemsConfig NonTeleportableItems { get; } = new(ConfigFile, "B - Non-teleportable Items");
    public SleepingConfig Sleeping { get; } = new(ConfigFile, "B - Sleeping");
    public FermentersConfig Fermenters { get; } = new(ConfigFile, "B - Fermenters");
    public NetworkingConfig Networking { get; } = new(ConfigFile, "B - Networking");
    public WishboneConfig Wishbone { get; } = new(ConfigFile, "B - Wishbone");
    public SkillsConfig Skills { get; } = new(ConfigFile, "B - Skills");
    public WorldModifiersConfig WorldModifiers { get; } = new(ConfigFile, "C - World Modifiers");
    public GlobalsKeysConfig GlobalsKeys { get; } = new(ConfigFile, "D - Global Keys");

    public AdvancedConfig Advanced { get; } = InitializeAdvancedConfig<AdvancedConfig>(ConfigFile, "Advanced.yml");
    public LocalizationConfig Localization { get; } = InitializeAdvancedConfig<LocalizationConfig>(ConfigFile, "Localization.yml");
}
