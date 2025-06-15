using BepInEx.Configuration;
using System.Reflection;
using Valheim.ServersideQoL.Processors;
using YamlDotNet.Serialization;

namespace Valheim.ServersideQoL;

sealed class ModConfig(ConfigFile cfg)
{
    public ConfigFile ConfigFile { get; } = cfg;

    public GeneralConfig General { get; } = new(cfg, "A - General");
    public SignsConfig Signs { get; } = new(cfg, "B - Signs");
    public MapTableConfig MapTables { get; } = new(cfg, "B - Map Tables");
    public TamesConfig Tames { get; } = new(cfg, "B - Tames");
    public CreaturesConfig Creatures { get; } = new(cfg, "B - Creatures");
    public SummonsConfig Summons { get; } = new(cfg, "B - Summons");
    public FireplacesConfig Fireplaces { get; } = new(cfg, "B - Fireplaces");
    public ContainersConfig Containers { get; } = new(cfg, "B - Containers");
    public SmeltersConfig Smelters { get; } = new(cfg, "B - Smelters");
    public WindmillsConfig Windmills { get; } = new(cfg, "B - Windmills");
    public CartsConfig Carts { get; } = new(cfg, "B - Carts");
    public DoorsConfig Doors { get; } = new(cfg, "B - Doors");
    public PlayersConfig Players { get; } = new(cfg, "B - Players");
    public TurretsConfig Turrets { get; } = new(cfg, "B - Turrets");
    public WearNTearConfig WearNTear { get; } = new(cfg, "B - Build Pieces");
    public CraftingStationsConfig CraftingStations { get; } = new(cfg, "B - Crafting Stations");
    public TradersConfig Traders { get; } = new(cfg, "B - Traders");
    public PlantsConfig Plants { get; } = new(cfg, "B - Plants");
    public TrapsConfig Traps { get; } = new(cfg, "B - Traps");
    public PortalHubConfig PortalHub { get; } = new(cfg, "B - Portal Hub");
    public WorldConfig World { get; } = new(cfg, "B - World");
    public TrophySpawnerConfig TrophySpawner { get; } = new(cfg, "B - Trophy Spawner");

    public WorldModifiersConfig WorldModifiers { get; } = new(cfg, "C - World Modifiers");
    public GlobalsKeysConfig GlobalsKeys { get; } = new(cfg, "D - Global Keys");

    public sealed class GeneralConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enabled { get; } = cfg.Bind(section, nameof(Enabled), true, "Enables/disables the entire mode");
        public ConfigEntry<bool> ConfigPerWorld { get; } = cfg.Bind(section, nameof(ConfigPerWorld), false, "Use one config file per world. The file is saved next to the world file");
        public ConfigEntry<bool> InWorldConfigRoom { get; } = cfg.Bind(section, nameof(InWorldConfigRoom), false,
            "True to generate an in-world room which admins can enter to configure this mod by editing signs. A portal is placed at the start location");
        public ConfigEntry<float> FarMessageRange { get; } = cfg.Bind(section, nameof(FarMessageRange), ZoneSystem.c_ZoneSize,
            $"Max distance a player can have to a modified object to receive messages of type {MessageTypes.TopLeftFar} or {MessageTypes.CenterFar}");

        public ConfigEntry<bool> DiagnosticLogs { get; } = cfg.Bind(section, nameof(DiagnosticLogs), false, "Enables/disables diagnostic logs");
        public ConfigEntry<float> Frequency { get; } = cfg.Bind(section, nameof(Frequency), 5f,
            new ConfigDescription("How many times per second the mod processes the world", new AcceptableValueRange<float>(0, float.PositiveInfinity)));
        public ConfigEntry<int> MaxProcessingTime { get; } = cfg.Bind(section, nameof(MaxProcessingTime), 20, "Max processing time (in ms) per update");
        public ConfigEntry<int> ZonesAroundPlayers { get; } = cfg.Bind(section, nameof(ZonesAroundPlayers), 1, "Zones to process around each player");
        public ConfigEntry<float> MinPlayerDistance { get; } = cfg.Bind(section, nameof(MinPlayerDistance), 4f, "Min distance all players must have to a ZDO for it to be modified");
        public ConfigEntry<bool> IgnoreGameVersionCheck { get; } = cfg.Bind(section, nameof(IgnoreGameVersionCheck), true, "True to ignore the game version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreNetworkVersionCheck { get; } = cfg.Bind(section, nameof(IgnoreNetworkVersionCheck), false, "True to ignore the network version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreItemDataVersionCheck { get; } = cfg.Bind(section, nameof(IgnoreItemDataVersionCheck), false, "True to ignore the item data version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreWorldVersionCheck { get; } = cfg.Bind(section, nameof(IgnoreWorldVersionCheck), false, "True to ignore the world version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
    }

    public sealed class SignsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<string> DefaultColor { get; } = cfg.Bind(section, nameof(DefaultColor), "", "Default color for signs. Can be a color name or hex code (e.g. #FF0000 for red)");
        public ConfigEntry<bool> TimeSigns { get; }= cfg.Bind(section, nameof(TimeSigns), false,
            Invariant($"True to update sign texts which contain time emojis (any of {string.Concat(SignProcessor.ClockEmojis)}) with the in-game time"));
    }

    public sealed class MapTableConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> AutoUpdatePortals { get; } = cfg.Bind(section, nameof(AutoUpdatePortals), false, "True to update map tables with portal pins");
        public ConfigEntry<string> AutoUpdatePortalsExclude { get; } = cfg.Bind(section, nameof(AutoUpdatePortalsExclude), "", "Portals with a tag that matches this filter are not added to map tables");
        public ConfigEntry<string> AutoUpdatePortalsInclude { get; } = cfg.Bind(section, nameof(AutoUpdatePortalsInclude), "*", "Only portals with a tag that matches this filter are added to map tables");

        public ConfigEntry<bool> AutoUpdateShips { get; } = cfg.Bind(section, nameof(AutoUpdateShips), false, "True to update map tables with ship pins");
        public ConfigEntry<MessageTypes> UpdatedMessageType { get; } = cfg.Bind(section, nameof(UpdatedMessageType), MessageTypes.None,
            new ConfigDescription("Type of message to show when a map table is updated", AcceptableEnum<MessageTypes>.Default));
    }

    public sealed class TamesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MakeCommandable { get; } = cfg.Bind(section, nameof(MakeCommandable), false, "True to make all tames commandable (like wolves)");
        //public ConfigEntry<bool> FeedFromContainers { get; } = cfg.Bind(section, nameof(FeedFromContainers), false, "True to feed tames from containers");

        public ConfigEntry<MessageTypes> TamingProgressMessageType { get; } = cfg.Bind(section, nameof(TamingProgressMessageType), MessageTypes.None,
            new ConfigDescription("Type of taming progress messages to show", AcceptableEnum<MessageTypes>.Default));
        public ConfigEntry<MessageTypes> GrowingProgressMessageType { get; } = cfg.Bind(section, nameof(GrowingProgressMessageType), MessageTypes.None,
            new ConfigDescription("Type of growing progress messages to show", AcceptableEnum<MessageTypes>.Default));
        public ConfigEntry<bool> AlwaysFed { get; } = cfg.Bind(section, nameof(AlwaysFed), false, "True to make tames always fed (not hungry)");

        public ConfigEntry<bool> TeleportFollow { get; } = cfg.Bind(section, nameof(TeleportFollow), false, "True to teleport following tames to the players location if the player gets too far away from them");
    }

    public sealed class CreaturesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> ShowHigherLevelStars { get; } = cfg.Bind(section, nameof(ShowHigherLevelStars), false,
            "True to show stars for higher level creatures (> 2 stars). The intended use is with other mods, which spawn higher level creatures");

        public ConfigEntry<ShowHigherLevelAuraOptions> ShowHigherLevelAura { get; } = cfg.Bind(section, nameof(ShowHigherLevelAura), ShowHigherLevelAuraOptions.Never,
            new ConfigDescription("Show an aura for higher level creatures (> 2 stars)", AcceptableEnum<ShowHigherLevelAuraOptions>.Default));

        [Flags]
        public enum ShowHigherLevelAuraOptions
        {
            Never = 0,
            Wild = (1 << 0),
            Tamed = (1 << 1)
        }
    }

    public sealed class FireplacesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MakeToggleable { get; } = cfg.Bind(section, nameof(MakeToggleable), false, "True to make all fireplaces (including torches, braziers, etc.) toggleable");
        public ConfigEntry<bool> InfiniteFuel { get; } = cfg.Bind(section, nameof(InfiniteFuel), false, "True to make all fireplaces have infinite fuel");
        public ConfigEntry<IgnoreRainOptions> IgnoreRain { get; } = cfg.Bind(section, nameof(IgnoreRain), IgnoreRainOptions.Never,
            new ConfigDescription("Options to make all fireplaces ignore rain", AcceptableEnum<IgnoreRainOptions>.Default));

        public enum IgnoreRainOptions
        {
            Never,
            Always,
            InsideShield
        }
    }

    public sealed class ContainersConfig(ConfigFile cfg, string section)
    {
        const string ChestSignItemNamesFileName = "ChestSignItemNames.yml";

        public ConfigEntry<bool> AutoSort { get; } = cfg.Bind(section, nameof(AutoSort), false, "True to auto sort container inventories");
        public ConfigEntry<MessageTypes> SortedMessageType { get; } = cfg.Bind(section, nameof(SortedMessageType), MessageTypes.None,
            new ConfigDescription("Type of message to show when a container was sorted", AcceptableEnum<MessageTypes>.Default));

        public ConfigEntry<bool> AutoPickup { get; } = cfg.Bind(section, nameof(AutoPickup), false, "True to automatically put dropped items into containers if they already contain said item");
        public ConfigEntry<float> AutoPickupRange { get; } = cfg.Bind(section, nameof(AutoPickupRange), ZoneSystem.c_ZoneSize,
            $"Required proximity of a container to a dropped item to be considered as auto pickup target. Can be overriden per chest by putting '{SignProcessor.MagnetEmoji}<Range>' on a chest sign");
        public ConfigEntry<float> AutoPickupMinPlayerDistance { get; } = cfg.Bind(section, nameof(AutoPickupMinPlayerDistance), 4f, "Min distance all player must have to a dropped item for it to be picked up");
        public ConfigEntry<bool> AutoPickupExcludeFodder { get; } = cfg.Bind(section, nameof(AutoPickupExcludeFodder), true, "True to exclude food items for tames when tames are within search range");
        public ConfigEntry<bool> AutoPickupRequestOwnership { get; } = cfg.Bind(section, nameof(AutoPickupRequestOwnership), true,
            "True to make the server request (and receive) ownership of dropped items from the clients before they are picked up. This will reduce the risk of data conflicts (e.g. item duplication) but will drastically decrease performance");
        public ConfigEntry<MessageTypes> PickedUpMessageType { get; } = cfg.Bind(section, nameof(PickedUpMessageType), MessageTypes.None,
            new ConfigDescription("Type of message to show when a dropped item is added to a container", AcceptableEnum<MessageTypes>.Default));

        const string DefaultPlaceholderString = "•";
        public ConfigEntry<string> ChestSignsDefaultText { get; } = cfg.Bind(section, nameof(ChestSignsDefaultText), DefaultPlaceholderString, "Default text for chest signs");
        public ConfigEntry<int> ChestSignsContentListMaxCount { get; } = cfg.Bind(section, nameof(ChestSignsContentListMaxCount), 3, "Max number of entries to show in the content list on chest signs.");
        public ConfigEntry<string> ChestSignsContentListPlaceholder { get; } = cfg.Bind(section, nameof(ChestSignsContentListPlaceholder), DefaultPlaceholderString, "Bullet to use for content lists on chest signs");
        public ConfigEntry<string> ChestSignsContentListSeparator { get; } = cfg.Bind(section, nameof(ChestSignsContentListSeparator), "<br>", "Separator to use for content lists on chest signs");
        public ConfigEntry<string> ChestSignsContentListNameRest { get; } = cfg.Bind(section, nameof(ChestSignsContentListNameRest), "Other", "Text to show for the entry summarizing the rest of the items");
        
        public ConfigEntry<string> ChestSignsContentListEntryFormat { get; } = cfg.Bind(section, nameof(ChestSignsContentListEntryFormat), "{0} {1}",
            new ConfigDescription($"Format string for entries in the content list, the first argument is the name of the item, the second is the total number of per item. The item names can be configured further by editing {ChestSignItemNamesFileName}", new AcceptableFormatString(["Test", 0])));

        public ConfigEntry<SignOptions> WoodChestSigns { get; } = cfg.Bind(section, nameof(WoodChestSigns), SignOptions.None,
            new ConfigDescription("Options to automatically put signs on wood chests", AcceptableEnum<SignOptions>.Default));
        public ConfigEntry<SignOptions> ReinforcedChestSigns { get; } = cfg.Bind(section, nameof(ReinforcedChestSigns), SignOptions.None,
            new ConfigDescription("Options to automatically put signs on reinforced chests", AcceptableEnum<SignOptions>.Default));
        public ConfigEntry<SignOptions> BlackmetalChestSigns { get; } = cfg.Bind(section, nameof(BlackmetalChestSigns), SignOptions.None,
            new ConfigDescription("Options to automatically put signs on blackmetal chests", AcceptableEnum<SignOptions>.Default));
        public ConfigEntry<SignOptions> ObliteratorSigns { get; } = cfg.Bind(section, nameof(ObliteratorSigns), SignOptions.None,
            new ConfigDescription("Options to automatically put signs on obliterators", new AcceptableEnum<SignOptions>([SignOptions.Front])));
        public ConfigEntry<ObliteratorItemTeleporterOptions> ObliteratorItemTeleporter { get; } = cfg.Bind(section, nameof(ObliteratorItemTeleporter), ObliteratorItemTeleporterOptions.Disabled,
            new ConfigDescription(
                $"Options to enable obliterators to teleport items instead of obliterating them when the lever is pulled. Requires '{nameof(ObliteratorSigns)}' and two obliterators with matching tags. The tag is set by putting '{SignProcessor.LinkEmoji}<Tag>' on the sign",
                AcceptableEnum<ObliteratorItemTeleporterOptions>.Default));
        public ConfigEntry<MessageTypes> ObliteratorItemTeleporterMessageType { get; } = cfg.Bind(section, nameof(ObliteratorItemTeleporterMessageType), MessageTypes.InWorld,
            new ConfigDescription("Type of message to show for obliterator item teleporters", AcceptableEnum<MessageTypes>.Default));

        public IReadOnlyDictionary<int, ConfigEntry<string>> ContainerSizes { get; } = ZNetScene.instance.m_prefabs
            .Where(x => SharedProcessorState.PieceTablesByPiece.ContainsKey(x.name))
            .Select(x => (Name: x.name, Container: x.GetComponent<Container>() ?? x.GetComponentInChildren<Container>(), Piece: x.GetComponent<Piece>()))
            .Where(x => x is { Container: not null, Piece: not null })
            .ToDictionary(x => x.Name.GetStableHashCode(), x => cfg
                .Bind(section, Invariant($"InventorySize_{x.Name}"), Invariant($"{x.Container.m_width}x{x.Container.m_height}"), Invariant($"Inventory size for '{Localization.instance.Localize(x.Piece.m_name)}'")));
        
        public enum ObliteratorItemTeleporterOptions
        {
            Disabled,
            Enabled,
            EnabledAllItems,

            [Obsolete]
            False = Disabled,
            [Obsolete]
            True = Enabled
        }

        [Flags]
        public enum SignOptions
        {
            None,
            Left = (1 << 0),
            Right = (1 << 1),
            Front = (1 << 2),
            Back = (1 << 3)
        }

        public IReadOnlyDictionary<string, string> ItemNames { get; } = new Func<IReadOnlyDictionary<string, string>>(() =>
        {
            var configDir = Main.Instance.ConfigDirectory;
            var itemNamesCfg = Path.Combine(configDir, ChestSignItemNamesFileName);
            Dictionary<string, string> items;
            if (!File.Exists(itemNamesCfg))
                items = new(ObjectDB.instance.m_items.Count);
            else
            {
                try
                {
                    using var stream = new StreamReader(File.OpenRead(itemNamesCfg));
                    items = new DeserializerBuilder().Build().Deserialize<Dictionary<string, string>>(stream);
                }
                catch (Exception ex)
                {
                    Main.Instance.Logger.LogWarning($"{ChestSignItemNamesFileName}: {ex.GetType().Name}: {ex.Message}");
                    items = new(ObjectDB.instance.m_items.Count);
                }
            }

            foreach (var entry in ObjectDB.instance.m_items)
            {
                if (entry.GetComponent<ItemDrop>() is not { m_itemData.m_shared.m_icons.Length: > 0 } itemDrop)
                    continue;
                if (!items.ContainsKey(itemDrop.name))
                    items.Add(itemDrop.name, Localization.instance.Localize(itemDrop.m_itemData.m_shared.m_name));
            }

            if (!File.Exists(itemNamesCfg))
            {
                Directory.CreateDirectory(configDir);
                using var stream = new StreamWriter(File.OpenWrite(itemNamesCfg));
                new SerializerBuilder().Build().Serialize(stream, items);
            }

            return items;
        }).Invoke();
    }

    public sealed class SmeltersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> FeedFromContainers { get; } = cfg.Bind(section, nameof(FeedFromContainers), false, "True to automatically feed smelters from nearby containers");
        public ConfigEntry<float> FeedFromContainersRange { get; } = cfg.Bind(section, nameof(FeedFromContainersRange), 4f,
            $"Required proxmity of a container to a smelter to be used as feeding source. Can be overriden per chest by putting '{SignProcessor.LeftRightArrowEmoji}<Range>' on a chest sign");
        public ConfigEntry<int> FeedFromContainersLeaveAtLeastFuel { get; } = cfg.Bind(section, nameof(FeedFromContainersLeaveAtLeastFuel), 1, "Minimum amout of fuel to leave in a container");
        public ConfigEntry<int> FeedFromContainersLeaveAtLeastOre { get; } = cfg.Bind(section, nameof(FeedFromContainersLeaveAtLeastOre), 1, "Minimum amout of ore to leave in a container");
        public ConfigEntry<MessageTypes> OreOrFuelAddedMessageType { get; } = cfg.Bind(section, nameof(OreOrFuelAddedMessageType), MessageTypes.None,
            new ConfigDescription("Type of message to show when ore or fuel is added to a smelter", AcceptableEnum<MessageTypes>.Default));
        public ConfigEntry<float> CapacityMultiplier { get; } = cfg.Bind(section, nameof(CapacityMultiplier), 1f, "Multiply a smelter's ore/fuel capacity by this factor");
    }

    public sealed class WindmillsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> IgnoreWind { get; } = cfg.Bind(section, nameof(IgnoreWind), false, "True to make windmills ignore wind (Cover still decreases operating efficiency though)");
    }

    public sealed class CartsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> ContentMassMultiplier { get; } = cfg.Bind(section, nameof(ContentMassMultiplier), 1f,
            new ConfigDescription("Multiplier for a carts content weight. E.g. set to 0 to ignore a cart's content weight", new AcceptableValueRange<float>(0, float.PositiveInfinity)));
    }

    public sealed class DoorsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> AutoCloseMinPlayerDistance { get; } = cfg.Bind(section, nameof(AutoCloseMinPlayerDistance), float.NaN,
            Invariant($"Min distance all players must have to the door before it is closed. {float.NaN} to disable this feature"));
    }

    public sealed class PlayersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> InfiniteBuildingStamina { get; } = cfg.Bind(section, nameof(InfiniteBuildingStamina), false,
            Invariant($"True to give players infinite stamina when building. If you want infinite stamina in general, set the global key '{nameof(GlobalKeys.StaminaRate)}' to 0"));
        public ConfigEntry<bool> InfiniteFarmingStamina { get; } = cfg.Bind(section, nameof(InfiniteFarmingStamina), false,
            Invariant($"True to give players infinite stamina when farming. If you want infinite stamina in general, set the global key '{nameof(GlobalKeys.StaminaRate)}' to 0"));
        public ConfigEntry<bool> InfiniteMiningStamina { get; } = cfg.Bind(section, nameof(InfiniteMiningStamina), false,
            Invariant($"True to give players infinite stamina when mining. If you want infinite stamina in general, set the global key '{nameof(GlobalKeys.StaminaRate)}' to 0"));
        public ConfigEntry<bool> InfiniteWoodCuttingStamina { get; } = cfg.Bind(section, nameof(InfiniteWoodCuttingStamina), false,
            Invariant($"True to give players infinite stamina when cutting wood. If you want infinite stamina in general, set the global key '{nameof(GlobalKeys.StaminaRate)}' to 0"));
        public ConfigEntry<bool> InfiniteEncumberedStamina { get; } = cfg.Bind(section, nameof(InfiniteEncumberedStamina), false,
            Invariant($"True to give players infinite stamina when encumbered. If you want infinite stamina in general, set the global key '{nameof(GlobalKeys.StaminaRate)}' to 0"));

        public const Emotes DisabledEmote = (Emotes)(-1);
        public const Emotes AnyEmote = (Emotes)(-2);
        public ConfigEntry<Emotes> StackInventoryIntoContainersEmote { get; } = cfg.Bind(section, nameof(StackInventoryIntoContainersEmote), DisabledEmote,
            new ConfigDescription($"Emote to stack inventory into containers. {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger", new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()])));

        public ConfigEntry<bool> CanSacrificeMegingjord { get; } = cfg.Bind(section, nameof(CanSacrificeMegingjord), false,
            "If true, players can permanently unlock increased carrying weight by sacrificing a megingjord in an obliterator");
        public ConfigEntry<bool> CanSacrificeCryptKey { get; } = cfg.Bind(section, nameof(CanSacrificeCryptKey), false,
            "If true, players can permanently unlock the ability to open sunken crypt doors by sacrificing a crypt key in an obliterator");
        public ConfigEntry<bool> CanSacrificeWishbone { get; } = cfg.Bind(section, nameof(CanSacrificeWishbone), false,
            "If true, players can permanently unlock the ability to sense hidden objects by sacrificing a wishbone in an obliterator");
        public ConfigEntry<bool> CanSacrificeTornSpirit { get; } = cfg.Bind(section, nameof(CanSacrificeTornSpirit), false,
            "If true, players can permanently unlock a wisp companion by sacrificing a torn spirit in an obliterator. WARNING: Wisp companion cannot be unsummoned and will stay as long as this setting is enabled.");
    }

    public sealed class TurretsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DontTargetPlayers { get; } = cfg.Bind(section, nameof(DontTargetPlayers), false, "True to stop ballistas from targeting players");
        public ConfigEntry<bool> DontTargetTames { get; } = cfg.Bind(section, nameof(DontTargetTames), false, "True to stop ballistas from targeting tames");
        public ConfigEntry<bool> LoadFromContainers { get; } = cfg.Bind(section, nameof(LoadFromContainers), false, "True to automatically load ballistas from containers");
        public ConfigEntry<float> LoadFromContainersRange { get; } = cfg.Bind(section, nameof(LoadFromContainersRange), 4f, "Required proxmity of a container to a ballista to be used as ammo source");
        public ConfigEntry<MessageTypes> AmmoAddedMessageType { get; } = cfg.Bind(section, nameof(AmmoAddedMessageType), MessageTypes.None,
            new ConfigDescription("Type of message to show when ammo is added to a turret", AcceptableEnum<MessageTypes>.Default));
        public ConfigEntry<MessageTypes> NoAmmoMessageType { get; } = cfg.Bind(section, nameof(NoAmmoMessageType), MessageTypes.None,
            new ConfigDescription("Type of message to show when there is no ammo to add to a turret", AcceptableEnum<MessageTypes>.Default));
    }

    public sealed class WearNTearConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DisableRainDamage { get; } = cfg.Bind(section, nameof(DisableRainDamage), false, "True to prevent rain from damaging build pieces");

        public ConfigEntry<DisableSupportRequirementsOptions> DisableSupportRequirements { get; } = cfg.Bind(section, nameof(DisableSupportRequirements), DisableSupportRequirementsOptions.None,
            new ConfigDescription("Ignore support requirements on build pieces", AcceptableEnum<DisableSupportRequirementsOptions>.Default));

        public ConfigEntry<bool> MakeIndestructible { get; } = cfg.Bind(section, nameof(MakeIndestructible), false, "True to make player-built pieces indestructible");

        [Flags]
        public enum DisableSupportRequirementsOptions
        {
            None,
            PlayerBuilt = (1 << 0),
            World = (1 << 1)
        }
    }

    public sealed class CraftingStationsConfig(ConfigFile cfg, string section)
    {
        public IReadOnlyDictionary<CraftingStation, StationCfg> StationConfig { get; } = new Func<IReadOnlyDictionary<CraftingStation, StationCfg>>(() =>
        {
            Dictionary<CraftingStation, bool> dict = [];
            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab.GetComponent<CraftingStation>() is { } station)
                {
                    if (station.m_areaMarker is not null && !dict.ContainsKey(station))
                        dict.Add(station, false);
                }
                else if (prefab.GetComponent<StationExtension>() is { } extension)
                {
                    station = extension.m_craftingStation;
                    dict[station] = true;
                }
            }
            return dict.ToDictionary(x => x.Key, x => new StationCfg(cfg, section, NormalizeName(x.Key.name), x.Key, x.Value));
        }).Invoke();
    
        public sealed class StationCfg(ConfigFile cfg, string section, string prefix, CraftingStation station, bool hasExtensions)
        {
            public ConfigEntry<float>? BuildRange { get; } = station.m_areaMarker is null ? null :
                cfg.Bind(section, $"{prefix}{nameof(BuildRange)}", station.m_rangeBuild, $"Build range of {Localization.instance.Localize(station.m_name)}");
            public ConfigEntry<float>? ExtraBuildRangePerLevel { get; } = station.m_areaMarker is null || !hasExtensions ? null :
                cfg.Bind(section, $"{prefix}{nameof(ExtraBuildRangePerLevel)}", station.m_extraRangePerLevel, $"Additional build range per level of {Localization.instance.Localize(station.m_name)}");
            public ConfigEntry<float>? MaxExtensionDistance { get; } = !hasExtensions ? null :
                cfg.Bind(section, $"{prefix}{nameof(MaxExtensionDistance)}", float.NaN,
                Invariant($"Max distance an extension can have to the corresponding {Localization.instance.Localize(station.m_name)} to increase its level. {float.NaN} to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional {Localization.instance.Localize(station.m_name)} to be able to place the extension."));
        }

        static string NormalizeName(string name)
        {
            if (char.IsUpper(name[0]))
                return name;
            name = name.Replace("piece_", "");
            return $"{char.ToUpperInvariant(name[0])}{name[1..]}";
        }
    }

    public sealed class PortalHubConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enable { get; } = cfg.Bind(section, nameof(Enable), false, "True to automatically generate a portal hub");
        public ConfigEntry<string> Exclude { get; } = cfg.Bind(section, nameof(Exclude), "", "Portals with a tag that matches this filter are not added to the portal hub");
        public ConfigEntry<string> Include { get; } = cfg.Bind(section, nameof(Include), "*", "Only portals with a tag that matches this filter are added to the portal hub");
        public ConfigEntry<bool> AutoNameNewPortals { get; } = cfg.Bind(section, nameof(AutoNameNewPortals), false, $"True to automatically name new portals. Has no effect if '{nameof(Enable)}' is false");
        public ConfigEntry<string> AutoNameNewPortalsFormat { get; } = cfg.Bind(section, nameof(AutoNameNewPortalsFormat), "{0} {1:D2}",
            new ConfigDescription("Format string for autonaming portals, the first argument is the biome name, the second is an automatically incremented integer",
                new AcceptableFormatString(["Test", 0])));
    }

    public sealed class WorldConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> AssignInteractableOwnershipToClosestPeer { get; } = cfg.Bind(section, nameof(AssignInteractableOwnershipToClosestPeer), false, "True to assign ownership of some interactable objects (such as smelters or cooking stations) to the closest peer. This should help avoiding the loss of ore, etc. due to networking issues.");
        public ConfigEntry<RemoveMistlandsMistOptions> RemoveMistlandsMist { get; } = cfg.Bind(section, nameof(RemoveMistlandsMist), RemoveMistlandsMistOptions.Never,
            new ConfigDescription("Condition to remove the mist from the mistlands", AcceptableEnum<RemoveMistlandsMistOptions>.Default));

        //public ConfigEntry<bool> UnlockSunkenCryptsAfterElder { get; } = cfg.Bind(section, nameof(UnlockSunkenCryptsAfterElder), false, "True to unlock sunken crypts after the Elder has been defeated");

        public enum RemoveMistlandsMistOptions
        {
            Never,
            Always,
            AfterQueenKilled,
            InsideShield
        }
    }

    public sealed class TrophySpawnerConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enable { get; } = cfg.Bind(section, nameof(Enable), false, "True to make dropped trophies attract mobs");
        public ConfigEntry<int> ActivationDelay { get; } = cfg.Bind(section, nameof(ActivationDelay), 3600, "Time in seconds before trophies start attracting mobs");
        public ConfigEntry<int> RespawnDelay { get; } = cfg.Bind(section, nameof(RespawnDelay), 12, "Respawn delay in seconds");
        public ConfigEntry<int> MaxLevel { get; } = cfg.Bind(section, nameof(MaxLevel), 3,
            new ConfigDescription("Maximum level of spawned mobs",
                new AcceptableValueRange<int>(1, 9)));
        public ConfigEntry<int> LevelUpChanceOverride { get; } = cfg.Bind(section, nameof(LevelUpChanceOverride), -1,
            new ConfigDescription("Level up chance override for spawned mobs. If < 0, world default is used", new AcceptableValueRange<int>(-1, 100)));
        public ConfigEntry<int> SpawnLimit { get; } = cfg.Bind(section, nameof(SpawnLimit), 20,
            new ConfigDescription("Maximum number of mobs of the trophy's type in the active area", new AcceptableValueRange<int>(1, 10000)));
        public ConfigEntry<bool> SuppressDrops { get; } = cfg.Bind(section, nameof(SuppressDrops), true,
            "True to suppress drops from mobs spawned by trophies. Does not work reliably (yet)");
        public ConfigEntry<MessageTypes> MessageType { get; } = cfg.Bind(section, nameof(MessageType), MessageTypes.InWorld,
            new ConfigDescription("Type of message to show when a trophy is attracting mobs", AcceptableEnum<MessageTypes>.Default));
    }

    public sealed class WorldModifiersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> SetPresetFromConfig { get; } = cfg.Bind(section, nameof(SetPresetFromConfig), false,
            Invariant($"True to set the world preset according to the '{nameof(Preset)}' config entry"));
        public ConfigEntry<WorldPresets> Preset { get; } = GetPreset(cfg, section);

        public ConfigEntry<bool> SetModifiersFromConfig { get; } = cfg.Bind(section, nameof(SetModifiersFromConfig), false,
            "True to set world modifiers according to the following configuration entries");
        public IReadOnlyDictionary<WorldModifiers, ConfigEntry<WorldModifierOption>> Modifiers { get; } = GetModifiers(cfg, section);

        static ConfigEntry<WorldPresets> GetPreset(ConfigFile cfg, string section)
        {
            /// <see cref="ServerOptionsGUI.SetPreset(World, WorldPresets)"/>
            var presets = PrivateAccessor.GetServerOptionsGUIPresets();
            return cfg.Bind(section, nameof(Preset), WorldPresets.Default, new ConfigDescription(Invariant($"World preset. Enable '{nameof(SetPresetFromConfig)}' for this to have an effect"),
                new AcceptableEnum<WorldPresets>(presets.Select(x => x.m_preset))));
        }

        static IReadOnlyDictionary<WorldModifiers, ConfigEntry<WorldModifierOption>> GetModifiers(ConfigFile cfg, string section)
        {
            /// <see cref="ServerOptionsGUI.SetPreset(World, WorldModifiers, WorldModifierOption)"/>
            var modifiers = PrivateAccessor.GetServerOptionsGUIModifiers()
                .OfType<KeySlider>()
                .Select(keySlider => (Key: keySlider.m_modifier, Cfg: cfg.Bind(section, Invariant($"{keySlider.m_modifier}"), WorldModifierOption.Default,
                    new ConfigDescription(Invariant($"World modifier '{keySlider.m_modifier}'. Enable '{nameof(SetModifiersFromConfig)}' for this to have an effect"),
                        new AcceptableEnum<WorldModifierOption>(keySlider.m_settings.Select(x => x.m_modifierValue))))))
                .ToDictionary(x => x.Key, x => x.Cfg);
            return modifiers;
        }
    }

    public sealed class GlobalsKeysConfig(ConfigFile cfg, string section, object? tmp = null)
    {
        public ConfigEntry<bool> SetGlobalKeysFromConfig { get; } = cfg.Bind(section, nameof(SetGlobalKeysFromConfig), false,
            "True to set global keys according to the following configuration entries");
        public IReadOnlyDictionary<GlobalKeys, ConfigEntryBase> KeyConfigs { get; } = ((GlobalKeyConfigFinder)(tmp ??= new GlobalKeyConfigFinder()))
            .Get<GlobalKeys>(GlobalKeys.Preset, cfg, section, Invariant($"Sets the value for the '{{0}}' global key. Enable '{nameof(SetGlobalKeysFromConfig)}' for this to have an effect"));

        public ConfigEntry<bool> NoPortalsPreventsContruction { get; } = cfg.Bind(section, nameof(NoPortalsPreventsContruction), true,
            Invariant($"True to change the effect of the '{GlobalKeys.NoPortals}' global key, to prevent the construction of new portals but leave existing portals functional"));

        //public IReadOnlyDictionary<PlayerKeys, ConfigEntryBase> PlayerKeys { get; } = ((GlobalKeyConfigFinder)(tmp ??= new GlobalKeyConfigFinder()))
        //    .Get<PlayerKeys>(null, cfg, section, Invariant($"Sets the value for the '{{0}}' player key. Enable '{nameof(SetGlobalKeysFromConfig)}' for this to have an effect"));
    }

    sealed class GlobalKeyConfigFinder
    {
        /// <see cref="ZoneSystem.GetGlobalKey(GlobalKeys, out string)"/>
        /// <see cref="Game.UpdateWorldRates(HashSet{string}, Dictionary{string, string})"/>
        
        sealed record FieldInfoEx(FieldInfo Field, object? RestoreValueObject, double RestoreValue)
        {
            public double ComparisonValue { get; set; } = double.NaN;
        }

        public IReadOnlyDictionary<TKey, ConfigEntryBase> Get<TKey>(TKey? maxEclusive, ConfigFile cfg, string section, string descriptionFormat)
            where TKey : unmanaged, Enum
        {
            List<(double TestValue, double Value)> testResults = [];
            IEnumerable<double> testValues = [float.MinValue, int.MinValue, .. Enumerable.Range(-100, 100).Select(x => (double)x), int.MaxValue, float.MaxValue];
            Dictionary<string, string> keyTestValues = [];

            List<FieldInfoEx> fields = [.. typeof(Game).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(x => !x.IsLiteral && !x.IsInitOnly)
                .Select(x => new FieldInfoEx(x, x.GetValue(null), TryGetAsDouble(x)))
                .Where(x => !double.IsNaN(x.RestoreValue))];

            MethodInfo? bindDefinition = null;

            // set all fields to default values (in case they were changed before this method is called)
            try { Game.UpdateWorldRates([], keyTestValues); }
            catch (NullReferenceException) { } /// expect in <see cref="Game.UpdateNoMap"/>

            foreach (var field in fields)
                field.ComparisonValue = TryGetAsDouble(field.Field);

            var result = new Dictionary<TKey, ConfigEntryBase>();
            foreach (TKey key in Enum.GetValues(typeof(TKey)))
            {
                if (maxEclusive is not null && key.ToInt64() >= maxEclusive.Value.ToInt64())
                    continue;

                var name = key.ToString();
                var nameLower = name.ToLower();

                FieldInfo? field = null;
                object? restoreValueObject = null;
                double comparisonValue = double.NaN;
                testResults.Clear();
                foreach (var testValue in testValues)
                {
                    keyTestValues.Clear();
                    keyTestValues.Add(nameLower, Invariant($"{testValue}"));
                    try { Game.UpdateWorldRates([], keyTestValues); }
                    catch (NullReferenceException) { } /// expect in <see cref="Game.UpdateNoMap"/>
                    double value = double.NaN;
                    if (field is null)
                    {
                        (field, restoreValueObject, comparisonValue, value, var idx) = fields
                            .Select((x, i) => (x.Field, x.RestoreValueObject, x.ComparisonValue, Value: TryGetAsDouble(x.Field), i))
                            .FirstOrDefault(x => x.ComparisonValue != x.Value);
                        if (field is not null)
                            fields.RemoveAt(idx);
                    }
                    else
                    {
                        value = TryGetAsDouble(field);
                        if (value == comparisonValue)
                            value = double.NaN;
                    }

                    if (!double.IsNaN(value))
                        testResults.Add((testValue, value));
                }

                if (testResults is { Count: > 0 } && field is not null)
                {
                    var min = testResults.Min(x => x.Value);
                    var max = testResults.Max(x => x.Value);
                    var inRange = testResults.Where(x => x.Value is not 0 && x.Value > min && x.Value < max);
                    var multiplier = inRange.Any() ? inRange.Average(x => x.TestValue / x.Value) : 1;
                    min *= multiplier;
                    max *= multiplier;
                    comparisonValue *= multiplier;

                    AcceptableValueBase? range = null;
                    if (min > float.MinValue && max < float.MaxValue && min < max)
                        range = (AcceptableValueBase)Activator.CreateInstance(typeof(AcceptableValueRange<>).MakeGenericType(field.FieldType), Convert.ChangeType(min, field.FieldType), Convert.ChangeType(max, field.FieldType));
                    var desc = new ConfigDescription(string.Format(descriptionFormat, name), range);
                    bindDefinition ??= new Func<string, string, bool, ConfigDescription, ConfigEntry<bool>>(cfg.Bind).Method.GetGenericMethodDefinition();
                    var entry = (ConfigEntryBase)bindDefinition.MakeGenericMethod(field.FieldType).Invoke(cfg, [section, name, Convert.ChangeType(comparisonValue, field.FieldType), desc]);
                    result.Add(key, entry);
                }
                else
                {
                    result.Add(key, cfg.Bind(section, name, false, string.Format(descriptionFormat, name)));
                }

                field?.SetValue(null, restoreValueObject);
            }

            foreach (var field in fields)
                field.Field.SetValue(null, field.RestoreValueObject);

            return result;
        }

        static double TryGetAsDouble(FieldInfo field)
        {
            var obj = field.GetValue(null);
            try { return (double)Convert.ChangeType(obj, typeof(double)); }
            catch { return double.NaN; }
        }
    }

    public sealed class TradersConfig(ConfigFile cfg, string section)
    {
        public IReadOnlyDictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>> AlwaysUnlock { get; } = GetAlwaysUnlock(cfg, section);

        static IReadOnlyDictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>> GetAlwaysUnlock(ConfigFile cfg, string section)
        {
            if (!ZNet.instance.IsServer() || !ZNet.instance.IsDedicated())
                return new Dictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>>();

            return ZNetScene.instance.m_prefabs.Select(x => x.GetComponent<Trader>()).Where(x => x is not null)
                .Select(trader => (Trader: trader, Entries: (IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>)trader.m_items
                    .Where(x => !string.IsNullOrEmpty(x.m_requiredGlobalKey))
                    .Select(item => (item.m_requiredGlobalKey, cfg.Bind(section, Invariant($"{nameof(AlwaysUnlock)}{trader.name}{item.m_prefab.name}"), false,
                        Invariant($"Remove the progression requirements for buying {Localization.instance.Localize(item.m_prefab.m_itemData.m_shared.m_name)} from {Localization.instance.Localize(trader.m_name)}"))))
                    .ToList()))
                .Where(x => x.Entries.Any())
                .ToDictionary(x => x.Trader, x => x.Entries);
        }
    }

    public sealed class PlantsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> GrowTimeMultiplier { get; } = cfg.Bind(section, nameof(GrowTimeMultiplier), 1f,
            new ConfigDescription("Multiply plant grow time by this factor. 0 to make them grow almost instantly.", new AcceptableValueRange<float>(0, float.PositiveInfinity)));
        public ConfigEntry<float> SpaceRequirementMultiplier { get; } = cfg.Bind(section, nameof(SpaceRequirementMultiplier), 1f,
            new ConfigDescription("Multiply plant space requirement by this factor. 0 to disable space requirements.", new AcceptableValueRange<float>(0, float.PositiveInfinity)));
        public ConfigEntry<bool> DontDestroyIfCantGrow { get; } = cfg.Bind(section, nameof(DontDestroyIfCantGrow), false, "True to keep plants which can't grow alive");
    }

    public sealed class SummonsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> UnsummonDistanceMultiplier { get; } = cfg.Bind(section, nameof(UnsummonDistanceMultiplier), 1f,
            new ConfigDescription("Multiply unsummon distance by this factor. 0 to disable distance-based unsummoning", new AcceptableValueRange<float>(0, float.PositiveInfinity)));
        public ConfigEntry<float> UnsummonLogoutTimeMultiplier { get; } = cfg.Bind(section, nameof(UnsummonLogoutTimeMultiplier), 1f,
            new ConfigDescription("Multiply the time after which summons are unsummoned when the player logs out. 0 to disable logout-based unsummoning", new AcceptableValueRange<float>(0, float.PositiveInfinity)));
    }

    public sealed class TrapsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DisableTriggeredByPlayers { get; } = cfg.Bind(section, nameof(DisableTriggeredByPlayers), false, "True to stop traps from being triggered by players");
        public ConfigEntry<bool> DisableFriendlyFire { get; } = cfg.Bind(section, nameof(DisableFriendlyFire), false, "True to stop traps from damaging players and tames");
        public ConfigEntry<float> SelfDamageMultiplier { get; } = cfg.Bind(section, nameof(SelfDamageMultiplier), 1f,
            new ConfigDescription("Multiply the damage the trap takes when it is triggered by this factor. 0 to make the trap take no damage", new AcceptableValueRange<float>(0, float.PositiveInfinity)));
        public ConfigEntry<bool> AutoRearm { get; } = cfg.Bind(section, nameof(AutoRearm), false, "True to automatically rearm traps when they are triggered");
    }

    internal sealed class AcceptableEnum<T> : AcceptableValueBase
        where T : unmanaged, Enum
    {
        public static AcceptableEnum<T> Default { get; } = new(GetDefaultValues());

        public IReadOnlyList<T> AcceptableValues { get; }
        readonly T _default;

        static IEnumerable<T> GetDefaultValues()
        {
            var added = new HashSet<T>();
            foreach (var value in (T[])Enum.GetValues(typeof(T)))
            {
                // Filter out duplicate (obsolete) values
                if (added.Add(value))
                    yield return value;
            }
        }

        public AcceptableEnum(IEnumerable<T> values)
            : base (typeof(T))
        {
            if (EnumUtils.IsBitSet<T>())
            {
                AcceptableValues = [.. values.Where(x => !x.Equals(default(T)))];
                _default = default;
            }
            else
            {
                AcceptableValues = values as IReadOnlyList<T> ?? [.. values];
                _default = AcceptableValues.FirstOrDefault();
            }
        }

        public override object Clamp(object value)
        {
            if (value is not T e)
                return _default;

            if (EnumUtils.IsBitSet<T>())
            {
                var val = e.ToUInt64();
                ulong result = 0;
                foreach (var flag in AcceptableValues.Select(x => x.ToUInt64()).Where(x => (val & x) == x))
                    result |= flag;
                return EnumUtils.ToEnum<T>(result);
            }
            else if (!AcceptableValues.Any(x => x.Equals(e)))
            {
                return _default;
            }
            return e;
        }

        public override bool IsValid(object value)
        {
            return Equals(value, Clamp(value));
        }

        public override string ToDescriptionString()
        {
            if (EnumUtils.IsBitSet<T>())
                return Invariant($"# Acceptable values: {_default} or combination of {string.Join(", ", AcceptableValues.Where(x => !x.Equals(_default)))}");
            else
                return Invariant($"# Acceptable values: {string.Join(", ", AcceptableValues)}");
        }
    }

    sealed class AcceptableFormatString(object[] testArgs) : AcceptableValueBase(typeof(string))
    {
        public override bool IsValid(object value)
        {
            if (value is not string format)
                return false;

            try { string.Format(format, testArgs); }
            catch (FormatException) { return false; }
            return true;
        }

        public override object Clamp(object value) => value;

        public override string ToDescriptionString()
            => $"# Acceptable values: .NET Format strings for two arguments ({string.Join(", ", testArgs.Select(x => x.GetType().Name))}): https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-string-format#get-started-with-the-stringformat-method";
    }
}
