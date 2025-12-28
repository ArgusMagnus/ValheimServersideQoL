using BepInEx.Configuration;
using System.Runtime.CompilerServices;

namespace Valheim.ServersideQoL;

sealed record ModConfig : ModConfigBase
{
    public static event Action<ConfigFile, ModConfig>? Initialized;

    public ModConfig(ConfigFile configFile)
        : base(configFile)
        => Initialized?.Invoke(configFile, this);
}

partial record ModConfigBase(ConfigFile ConfigFile)
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
    public ShipsConfig Ships { get; } = new(ConfigFile, "B - Ships");
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

    public sealed record Deprecated(string Reason, Action<ModConfig> AdjustConfig);
    static readonly HashSet<ConfigEntryBase> __deprecatedEntries = [];

    public static bool IsDeprecated(ConfigEntryBase entry)
        => __deprecatedEntries.Contains(entry);

    public static ConfigEntry<T> BindEx<T>(ConfigFile config, string section, T defaultValue, string description,
        AcceptableValueBase? acceptableValues,
        Deprecated? deprecated,
        string key)
    {
        if (deprecated is not null)
            description = string.Join(Environment.NewLine, [$"DEPRECATED: {deprecated.Reason}", description]);
        var cfg = config.Bind(section, key, defaultValue, new ConfigDescription(description, acceptableValues));
        if (deprecated is not null)
        {
            __deprecatedEntries.Add(cfg);
            ModConfig.Initialized += OnInitialized;
        }
        return cfg;

        void OnInitialized(ConfigFile cfgFile, ModConfig modConfig)
        {
            if (!ReferenceEquals(cfgFile, cfg.ConfigFile))
                return;
            ModConfig.Initialized -= OnInitialized;
            cfg.SettingChanged += (_, _) => OnSettingChanged(deprecated, cfg, modConfig);
            OnSettingChanged(deprecated, cfg, modConfig);
        }

        static void OnSettingChanged(Deprecated deprecated, ConfigEntry<T> cfg, ModConfig modCfg)
        {
            if (EqualityComparer<T>.Default.Equals(cfg.Value, (T)cfg.DefaultValue))
                return;
            deprecated.AdjustConfig(modCfg);
            Main.Instance.Logger.LogWarning($"[{cfg.Definition.Section}].[{cfg.Definition.Key}] is deprecated: {deprecated.Reason}");
        }
    }
}
