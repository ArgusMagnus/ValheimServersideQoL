using BepInEx.Configuration;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Valheim.ServersideQoL.Processors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace Valheim.ServersideQoL;

sealed record ModConfig(ConfigFile ConfigFile)
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

    public WorldModifiersConfig WorldModifiers { get; } = new(ConfigFile, "C - World Modifiers");
    public GlobalsKeysConfig GlobalsKeys { get; } = new(ConfigFile, "D - Global Keys");

    public AdvancedConfig Advanced { get; } = InitializeAdvancedConfig(ConfigFile);

    public sealed class GeneralConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enabled { get; } = cfg.BindEx(section, true, "Enables/disables the entire mod");
        public ConfigEntry<bool> ConfigPerWorld { get; } = cfg.BindEx(section, false, "Use one config file per world. The file is saved next to the world file");
        public ConfigEntry<bool> InWorldConfigRoom { get; } = cfg.BindEx(section, false,
            "True to generate an in-world room which admins can enter to configure this mod by editing signs. A portal is placed at the start location");
        public ConfigEntry<float> FarMessageRange { get; } = cfg.BindEx(section, ZoneSystem.c_ZoneSize,
            $"Max distance a player can have to a modified object to receive messages of type {MessageTypes.TopLeftFar} or {MessageTypes.CenterFar}");

        public ConfigEntry<bool> DiagnosticLogs { get; } = cfg.BindEx(section, false, "Enables/disables diagnostic logs");
        public ConfigEntry<int> ZonesAroundPlayers { get; } = cfg.BindEx(section, ZoneSystem.instance.GetActiveArea(), "Zones to process around each player");
        public ConfigEntry<float> MinPlayerDistance { get; } = cfg.BindEx(section, 4f, "Min distance all players must have to a ZDO for it to be modified");
        public ConfigEntry<bool> IgnoreGameVersionCheck { get; } = cfg.BindEx(section, true, "True to ignore the game version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreNetworkVersionCheck { get; } = cfg.BindEx(section, false, "True to ignore the network version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreItemDataVersionCheck { get; } = cfg.BindEx(section, false, "True to ignore the item data version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreWorldVersionCheck { get; } = cfg.BindEx(section, false, "True to ignore the world version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
    }

    public sealed class SignsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<string> DefaultColor { get; } = cfg.BindEx(section, "", "Default color for signs. Can be a color name or hex code (e.g. #FF0000 for red)");
        public ConfigEntry<bool> TimeSigns { get; }= cfg.BindEx(section, false,
            Invariant($"True to update sign texts which contain time emojis (any of {string.Concat(SignProcessor.ClockEmojis)}) with the in-game time"));
    }

    public sealed class MapTableConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> AutoUpdatePortals { get; } = cfg.BindEx(section, false, "True to update map tables with portal pins");
        public ConfigEntry<string> AutoUpdatePortalsExclude { get; } = cfg.BindEx(section, "", "Portals with a tag that matches this filter are not added to map tables");
        public ConfigEntry<string> AutoUpdatePortalsInclude { get; } = cfg.BindEx(section, "*", "Only portals with a tag that matches this filter are added to map tables");

        public ConfigEntry<bool> AutoUpdateShips { get; } = cfg.BindEx(section, false, "True to update map tables with ship pins");
        public ConfigEntry<MessageTypes> UpdatedMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when a map table is updated", AcceptableEnum<MessageTypes>.Default);
    }

    public sealed class TamesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MakeCommandable { get; } = cfg.BindEx(section, false, "True to make all tames commandable (like wolves)");
        //public ConfigEntry<bool> FeedFromContainers { get; } = cfg.BindEx(section, false, "True to feed tames from containers");

        public ConfigEntry<MessageTypes> TamingProgressMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of taming progress messages to show", AcceptableEnum<MessageTypes>.Default);
        public ConfigEntry<MessageTypes> GrowingProgressMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of growing progress messages to show", AcceptableEnum<MessageTypes>.Default);
        public ConfigEntry<float> FedDurationMultiplier { get; } = cfg.BindEx(section, 1f, Invariant(
            $"Multiply the time tames stay fed after they have eaten by this factor. {float.PositiveInfinity} to keep them fed indefinitely"));
        public ConfigEntry<float> TamingTimeMultiplier { get; } = cfg.BindEx(section, 1f, """
            Multiply the time it takes to tame a tameable creature by this factor.
            E.g. a value of 0.5 means that the taming time is halved.
            """);
        public ConfigEntry<float> PotionTamingBoostMultiplier { get; } = cfg.BindEx(section, 1f, """
            Multiply the taming boost from the animal whispers potion by this factor.
            E.g. a value of 2 means that the effect of the potion is doubled and the resulting taming time is reduced by a factor of 4 per player.
            """);            
        public ConfigEntry<bool> TeleportFollow { get; } = cfg.BindEx(section, false, "True to teleport following tames to the players location if the player gets too far away from them");
        public ConfigEntry<bool> TakeIntoDungeons { get; } = cfg.BindEx(section, false, $"True to take following tames into (and out of) dungeons with you");
    }

    public sealed class CreaturesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> ShowHigherLevelStars { get; } = cfg.BindEx(section, false,
            "True to show stars for higher level creatures (> 2 stars)");

        public ConfigEntry<ShowHigherLevelAuraOptions> ShowHigherLevelAura { get; } = cfg.BindEx(section, ShowHigherLevelAuraOptions.Never,
            "Show an aura for higher level creatures (> 2 stars)", AcceptableEnum<ShowHigherLevelAuraOptions>.Default);

        public ConfigEntry<int> MaxLevelIncrease { get; } = cfg.BindEx(section, 0, """
            Amount the max level of creatures is incremented throughout the world.
            The level up chance increases with the max level.
            Example: if this value is set to 2, a creature will spawn with 4 stars with the same probability as it would spawn with 2 stars without this setting.
            """);

        public ConfigEntry<int> MaxLevelIncreasePerDefeatedBoss { get; } = cfg.BindEx(section, 0, """
            Amount the max level of creatures is incremented per defeated boss.
            The respective boss's biome and previous biomes are affected and the level up chance increases with the max level.
            Example: If this value is set to 1 and Eikthyr and the Elder is defeated, the max creature level in the Black Forest will be raised by 1 and in the Meadows by 2.
            """);

        public ConfigEntry<Heightmap.Biome> TreatOceanAs { get; } = cfg.BindEx(section, Heightmap.Biome.BlackForest,
            "Biome to treat the ocean as for the purpose of leveling up creatures",
            new AcceptableEnum<Heightmap.Biome>(AcceptableEnum<Heightmap.Biome>.Default.AcceptableValues.Where(static x => x is not Heightmap.Biome.Ocean)));

        public ConfigEntry<bool> LevelUpBosses { get; } = cfg.BindEx(section, false, "True to also level up bosses");

        public ConfigEntry<RespawnOneTimeSpawnsConditions> RespawnOneTimeSpawnsCondition { get; } = cfg.BindEx(section, RespawnOneTimeSpawnsConditions.Never,
            "Condition for one-time spawns to respawn");

        public ConfigEntry<float> RespawnOneTimeSpawnsAfter { get; } = cfg.BindEx(section, 240f,
            "Time after one-time spawns are respawned in minutes");

        [Flags]
        public enum ShowHigherLevelAuraOptions
        {
            Never = 0,
            Wild = (1 << 0),
            Tamed = (1 << 1)
        }

        public enum RespawnOneTimeSpawnsConditions
        {
            Never,
            Always,
            AfterBossDefeated
        }
    }

    public sealed class FireplacesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MakeToggleable { get; } = cfg.BindEx(section, false, "True to make all fireplaces (including torches, braziers, etc.) toggleable");
        public ConfigEntry<bool> InfiniteFuel { get; } = cfg.BindEx(section, false, "True to make all fireplaces have infinite fuel");
        public ConfigEntry<IgnoreRainOptions> IgnoreRain { get; } = cfg.BindEx(section, IgnoreRainOptions.Never,
            "Options to make all fireplaces ignore rain", AcceptableEnum<IgnoreRainOptions>.Default);

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

        public ConfigEntry<bool> AutoSort { get; } = cfg.BindEx(section, false, "True to auto sort container inventories");
        public ConfigEntry<MessageTypes> SortedMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when a container was sorted", AcceptableEnum<MessageTypes>.Default);

        public ConfigEntry<bool> AutoPickup { get; } = cfg.BindEx(section, false, "True to automatically put dropped items into containers if they already contain said item");
        public ConfigEntry<float> AutoPickupRange { get; } = cfg.BindEx(section, ZoneSystem.c_ZoneSize,
            $"Required proximity of a container to a dropped item to be considered as auto pickup target. Can be overridden per chest by putting '{SignProcessor.MagnetEmoji}<Range>' on a chest sign");
        public ConfigEntry<int> AutoPickupMaxRange { get; } = cfg.BindEx(section, (int)ZoneSystem.c_ZoneSize,
            $"Max auto pickup range players can set per chest (by putting '{SignProcessor.MagnetEmoji}<Range>' on a chest sign)");
        public ConfigEntry<float> AutoPickupMinPlayerDistance { get; } = cfg.BindEx(section, 4f,
            "Min distance all player must have to a dropped item for it to be picked up");
        public ConfigEntry<bool> AutoPickupExcludeFodder { get; } = cfg.BindEx(section, true,
            "True to exclude food items for tames when tames are within search range");
        public ConfigEntry<bool> AutoPickupRequestOwnership { get; } = cfg.BindEx(section, true,
            "True to make the server request (and receive) ownership of dropped items from the clients before they are picked up. This will reduce the risk of data conflicts (e.g. item duplication) but will drastically decrease performance");
        public ConfigEntry<MessageTypes> PickedUpMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when a dropped item is added to a container", AcceptableEnum<MessageTypes>.Default);

        const string DefaultPlaceholderString = "•";
        public ConfigEntry<string> ChestSignsDefaultText { get; } = cfg.BindEx(section, DefaultPlaceholderString, "Default text for chest signs");
        public ConfigEntry<string> ChestSignsContentListPlaceholder { get; } = cfg.BindEx(section, DefaultPlaceholderString,
            "If this value is found in the text of a chest sign, it will be replaced by a list of contained items in that chest");
        public ConfigEntry<int> ChestSignsContentListMaxCount { get; } = cfg.BindEx(section, 3,
            "Max number of entries to show in the content list on chest signs.");
        public ConfigEntry<string> ChestSignsContentListSeparator { get; } = cfg.BindEx(section, "<br>",
            "Separator to use for content lists on chest signs");
        public ConfigEntry<string> ChestSignsContentListNameRest { get; } = cfg.BindEx(section, "Other",
            "Text to show for the entry summarizing the rest of the items");        
        public ConfigEntry<string> ChestSignsContentListEntryFormat { get; } = cfg.BindEx(section, "{0} {1}",
            $"Format string for entries in the content list, the first argument is the name of the item, the second is the total number of per item. The item names can be configured further by editing {ChestSignItemNamesFileName}",
            new AcceptableFormatString(["Test", 0]));

        public ConfigEntry<SignOptions> WoodChestSigns { get; } = cfg.BindEx(section, SignOptions.None,
            "Options to automatically put signs on wood chests", AcceptableEnum<SignOptions>.Default);
        public ConfigEntry<SignOptions> ReinforcedChestSigns { get; } = cfg.BindEx(section, SignOptions.None,
            "Options to automatically put signs on reinforced chests", AcceptableEnum<SignOptions>.Default);
        public ConfigEntry<SignOptions> BlackmetalChestSigns { get; } = cfg.BindEx(section, SignOptions.None,
            "Options to automatically put signs on blackmetal chests", AcceptableEnum<SignOptions>.Default);
        public ConfigEntry<SignOptions> BarrelSigns { get; } = cfg.BindEx(section, SignOptions.None,
            "Options to automatically put signs on barrels", AcceptableEnum<SignOptions>.Default);
        public ConfigEntry<SignOptions> ObliteratorSigns { get; } = cfg.BindEx(section, SignOptions.None,
            "Options to automatically put signs on obliterators", new AcceptableEnum<SignOptions>([SignOptions.Front]));
        public ConfigEntry<ObliteratorItemTeleporterOptions> ObliteratorItemTeleporter { get; } = cfg.BindEx(section, ObliteratorItemTeleporterOptions.Disabled,
            $"Options to enable obliterators to teleport items instead of obliterating them when the lever is pulled. Requires '{nameof(ObliteratorSigns)}' and two obliterators with matching tags. The tag is set by putting '{SignProcessor.LinkEmoji}<Tag>' on the sign",
            AcceptableEnum<ObliteratorItemTeleporterOptions>.Default);
        public ConfigEntry<MessageTypes> ObliteratorItemTeleporterMessageType { get; } = cfg.BindEx(section, MessageTypes.InWorld,
            "Type of message to show for obliterator item teleporters", AcceptableEnum<MessageTypes>.Default);

        public IReadOnlyDictionary<int, ConfigEntry<string>> ContainerSizes { get; } = ZNetScene.instance.m_prefabs
            .Where(static x => SharedProcessorState.PieceTablesByPiece.ContainsKey(x.name))
            .Select(static x => (Name: x.name, Container: x.GetComponentInChildren<Container>(), Piece: x.GetComponent<Piece>()))
            .Where(static x => x is { Container: not null, Piece: not null })
            .ToDictionary(static x => x.Name.GetStableHashCode(), x => cfg
                .Bind(section, Invariant($"InventorySize_{x.Name}"), Invariant($"{x.Container.m_width}x{x.Container.m_height}"), Invariant($"""
                    Inventory size for '{Localization.instance.Localize(x.Piece.m_name)}'.
                    If you append '+' to the end (e.g. '{x.Container.m_width}x{x.Container.m_height}+'),
                    the inventory size will keep expanding as long as only one type of item is stored inside.
                    """)));                    
        
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
            Back = (1 << 3),
            TopLongitudinal = (1 << 4),
            TopLateral = (1 << 5)
        }

        public IReadOnlyDictionary<string, string> ItemNames { get; } = new Func<IReadOnlyDictionary<string, string>>(() =>
        {
            var configDir = Path.Combine(Path.GetDirectoryName(cfg.ConfigFilePath), Path.GetFileNameWithoutExtension(cfg.ConfigFilePath));
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
                    Main.Instance.Logger.LogWarning($"{ChestSignItemNamesFileName}: {ex}");
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
                WriteYamlHeader(stream);
                new SerializerBuilder().Build().Serialize(stream, items);
            }

            return items;
        }).Invoke();
    }

    public sealed class SmeltersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> FeedFromContainers { get; } = cfg.BindEx(section, false,
            "True to automatically feed smelters from nearby containers");
        public ConfigEntry<float> FeedFromContainersRange { get; } = cfg.BindEx(section, 4f, $"""
            Required proximity of a container to a smelter to be used as feeding source.
            Can be overridden per chest by putting '{SignProcessor.LeftRightArrowEmoji}<Range>' on a chest sign.
            """);            
        public ConfigEntry<int> FeedFromContainersMaxRange { get; } = cfg.BindEx(section, (int)ZoneSystem.c_ZoneSize,
            $"Max feeding range players can set per chest (by putting '{SignProcessor.LeftRightArrowEmoji}<Range>' on a chest sign)");
        public ConfigEntry<int> FeedFromContainersLeaveAtLeastFuel { get; } = cfg.BindEx(section, 1,
            "Minimum amount of fuel to leave in a container");
        public ConfigEntry<int> FeedFromContainersLeaveAtLeastOre { get; } = cfg.BindEx(section, 1,
            "Minimum amount of ore to leave in a container");
        public ConfigEntry<MessageTypes> OreOrFuelAddedMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when ore or fuel is added to a smelter", AcceptableEnum<MessageTypes>.Default);
        public ConfigEntry<float> CapacityMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply a smelter's ore/fuel capacity by this factor");
        public ConfigEntry<float> TimePerProductMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply the time it takes to produce one product by this factor (will not go below 1 second per product).");
    }

    public sealed class WindmillsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> IgnoreWind { get; } = cfg.BindEx(section, false,
            "True to make windmills ignore wind (Cover still decreases operating efficiency though)");
    }

    public sealed class CartsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> ContentMassMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiplier for a carts content weight. E.g. set to 0 to ignore a cart's content weight", new AcceptableValueRange<float>(0, float.PositiveInfinity));
    }

    public sealed class DoorsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> AutoCloseMinPlayerDistance { get; } = cfg.BindEx(section, float.NaN,
            Invariant($"Min distance all players must have to the door before it is closed. {float.NaN} to disable this feature"));
    }

    public sealed class PlayersConfig(ConfigFile cfg, string section)
    {
        static string GetInfiniteXDescription(string action) => Invariant($"""
            True to give players infinite stamina when {action}.
            Player stamina will still be drained, but when nearly depleted, just enough stamina will be restored to continue indefinitely.
            If you want infinite stamina in general, set the global key '{nameof(GlobalKeys.StaminaRate)}' to 0.
            """);
            
        public ConfigEntry<bool> InfiniteBuildingStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("building"));                
        public ConfigEntry<bool> InfiniteFarmingStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("farming"));
        public ConfigEntry<bool> InfiniteMiningStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("mining"));
        public ConfigEntry<bool> InfiniteWoodCuttingStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("cutting wood"));
        public ConfigEntry<bool> InfiniteEncumberedStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("encumbered"));
        public ConfigEntry<bool> InfiniteSneakingStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("sneaking"));
        public ConfigEntry<bool> InfiniteSwimmingStamina { get; } = cfg.BindEx(section, false, GetInfiniteXDescription("swimming"));

        public const Emotes DisabledEmote = (Emotes)(-1);
        public const Emotes AnyEmote = (Emotes)(-2);
        public ConfigEntry<Emotes> StackInventoryIntoContainersEmote { get; } = cfg.BindEx(section, DisabledEmote, $"""
            Emote to stack inventory into containers.
            If a player uses this emote, their inventory will be automatically stacked into nearby containers.
            The rules for which containers are used are the same as for auto pickup.
            {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
            If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
            """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

        public ConfigEntry<float> StackInventoryIntoContainersReturnDelay { get; } = cfg.BindEx(section, 1f, """
            Time in seconds after which items which could not be stacked into containers are returned to the player.
            Increasing this value can help with bad connections.
            """, new AcceptableValueRange<float>(1f, 10f));

        public ConfigEntry<Emotes> OpenBackpackEmote { get; } = cfg.BindEx(section, DisabledEmote, $"""
            Emote to open the backpack.
            If a player uses this emote, a virtual container acting as their backpack will open.
            {DisabledEmote} to disable this feature, {AnyEmote} to use any emote as trigger.
            You can bind emotes to buttons with chat commands.
            For example, on xbox you can bind the Y-Button to the wave-emote by entering "/bind JoystickButton3 {Emotes.Wave}" in the in-game chat.
            If you use emotes exclusively for this feature, it is recommended to set the value to {AnyEmote} as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.
            """, new AcceptableEnum<Emotes>([DisabledEmote, AnyEmote, .. Enum.GetValues(typeof(Emotes)).Cast<Emotes>()]));

        //public ConfigEntry<int> InitialBackpackSlots { get; } = cfg.BindEx(section, 0, "Initial available slots in the backpack");
        ////public ConfigEntry<int>

        public ConfigEntry<bool> CanSacrificeMegingjord { get; } = cfg.BindEx(section, false,
            "If true, players can permanently unlock increased carrying weight by sacrificing a megingjord in an obliterator");
        public ConfigEntry<bool> CanSacrificeCryptKey { get; } = cfg.BindEx(section, false,
            "If true, players can permanently unlock the ability to open sunken crypt doors by sacrificing a crypt key in an obliterator");
        public ConfigEntry<bool> CanSacrificeWishbone { get; } = cfg.BindEx(section, false,
            "If true, players can permanently unlock the ability to sense hidden objects by sacrificing a wishbone in an obliterator");
        public ConfigEntry<bool> CanSacrificeTornSpirit { get; } = cfg.BindEx(section, false,
            "If true, players can permanently unlock a wisp companion by sacrificing a torn spirit in an obliterator. WARNING: Wisp companion cannot be unsummoned and will stay as long as this setting is enabled.");
    }

    public sealed class TurretsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DontTargetPlayers { get; } = cfg.BindEx(section, false, "True to stop ballistas from targeting players");
        public ConfigEntry<bool> DontTargetTames { get; } = cfg.BindEx(section, false, "True to stop ballistas from targeting tames");
        public ConfigEntry<bool> LoadFromContainers { get; } = cfg.BindEx(section, false, "True to automatically load ballistas from containers");
        public ConfigEntry<float> LoadFromContainersRange { get; } = cfg.BindEx(section, 4f, "Required proximity of a container to a ballista to be used as ammo source");
        public ConfigEntry<MessageTypes> AmmoAddedMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when ammo is added to a turret", AcceptableEnum<MessageTypes>.Default);
        public ConfigEntry<MessageTypes> NoAmmoMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when there is no ammo to add to a turret", AcceptableEnum<MessageTypes>.Default);
    }

    public sealed class WearNTearConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DisableRainDamage { get; } = cfg.BindEx(section, false, "True to prevent rain from damaging build pieces");

        public ConfigEntry<DisableSupportRequirementsOptions> DisableSupportRequirements { get; } = cfg.BindEx(section, DisableSupportRequirementsOptions.None,
            "Ignore support requirements on build pieces", AcceptableEnum<DisableSupportRequirementsOptions>.Default);

        public ConfigEntry<bool> MakeIndestructible { get; } = cfg.BindEx(section, false, "True to make player-built pieces indestructible");

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
            return dict.ToDictionary(static x => x.Key, x => new StationCfg(cfg, section, NormalizeName(x.Key.name), x.Key, x.Value));
        }).Invoke();
    
        public sealed class StationCfg(ConfigFile cfg, string section, string prefix, CraftingStation station, bool hasExtensions)
        {
            public ConfigEntry<float>? BuildRange { get; } = station.m_areaMarker is null ? null :
                cfg.Bind(section, $"{prefix}{nameof(BuildRange)}", station.m_rangeBuild, $"Build range of {Localization.instance.Localize(station.m_name)}");
            public ConfigEntry<float>? ExtraBuildRangePerLevel { get; } = station.m_areaMarker is null || !hasExtensions ? null :
                cfg.Bind(section, $"{prefix}{nameof(ExtraBuildRangePerLevel)}", station.m_extraRangePerLevel, $"Additional build range per level of {Localization.instance.Localize(station.m_name)}");
            public ConfigEntry<float>? MaxExtensionDistance { get; } = !hasExtensions ? null :
                cfg.Bind(section, $"{prefix}{nameof(MaxExtensionDistance)}", float.NaN, Invariant($"""
                    Max distance an extension can have to the corresponding {Localization.instance.Localize(station.m_name)} to increase its level.
                    Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional {Localization.instance.Localize(station.m_name)} to be able to place the extension.
                    {float.NaN} to use the game's default range. 
                    """));
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
        public ConfigEntry<bool> Enable { get; } = cfg.BindEx(section, false, """
            True to automatically generate a portal hub.
            Placed portals which don't have a paired portal in the world will be connected to the portal hub.
            """);
            
        public ConfigEntry<string> Exclude { get; } = cfg.BindEx(section, "", "Portals with a tag that matches this filter are not connected to the portal hub");
        public ConfigEntry<string> Include { get; } = cfg.BindEx(section, "*", "Only portals with a tag that matches this filter are connected to the portal hub");
        public ConfigEntry<bool> AutoNameNewPortals { get; } = cfg.BindEx(section, false, $"True to automatically name new portals. Has no effect if '{nameof(Enable)}' is false");
        public ConfigEntry<string> AutoNameNewPortalsFormat { get; } = cfg.BindEx(section, "{0} {1:D2}",
            "Format string for auto-naming portals, the first argument is the biome name, the second is an automatically incremented integer",
            new AcceptableFormatString(["Test", 0]));
    }

    public sealed class WorldConfig(ConfigFile cfg, string section)
    {
        //public ConfigEntry<bool> AssignInteractableOwnershipToClosestPeer { get; } = cfg.BindEx(section, false, "True to assign ownership of some interactable objects (such as smelters or cooking stations) to the closest peer. This should help avoiding the loss of ore, etc. due to networking issues.");
        public ConfigEntry<RemoveMistlandsMistOptions> RemoveMistlandsMist { get; } = cfg.BindEx(section, RemoveMistlandsMistOptions.Never, """
            Condition to remove the mist from the mistlands.
            Beware that there are a few cases of mist (namely mist around POIs like ancient bones/skulls)
            that cannot be removed by this mod and will remain regardless of this setting.
            """, AcceptableEnum<RemoveMistlandsMistOptions>.Default);
            

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
        public ConfigEntry<bool> Enable { get; } = cfg.BindEx(section, false, "True to make dropped trophies attract mobs.");
            
        public ConfigEntry<int> ActivationDelay { get; } = cfg.BindEx(section, 3600, "Time in seconds before trophies start attracting mobs");
        public ConfigEntry<int> RespawnDelay { get; } = cfg.BindEx(section, 12, "Respawn delay in seconds");
        static float MaxDistance => Mathf.Round(Mathf.Sqrt(2) * ZoneSystem.instance.m_activeArea * ZoneSystem.c_ZoneSize);
        public ConfigEntry<float> MinSpawnDistance { get; } = cfg.BindEx(section, MaxDistance,
            "Min distance from the trophy mobs can spawn", new AcceptableValueRange<float>(0, MaxDistance));
        public ConfigEntry<float> MaxSpawnDistance { get; } = cfg.BindEx(section, MaxDistance,
            "Max distance from the trophy mobs can spawn", new AcceptableValueRange<float>(0, MaxDistance));
        public ConfigEntry<int> MaxLevel { get; } = cfg.BindEx(section, 3, "Maximum level of spawned mobs", new AcceptableValueRange<int>(1, 9));
        public ConfigEntry<int> LevelUpChanceOverride { get; } = cfg.BindEx(section, -1,
            "Level up chance override for spawned mobs. If < 0, world default is used", new AcceptableValueRange<int>(-1, 100));
        public ConfigEntry<int> SpawnLimit { get; } = cfg.BindEx(section, 20,
            "Maximum number of mobs of the trophy's type in the active area", new AcceptableValueRange<int>(1, 10000));
        public ConfigEntry<bool> SuppressDrops { get; } = cfg.BindEx(section, true,
            "True to suppress drops from mobs spawned by trophies. Does not work reliably (yet)");
        public ConfigEntry<MessageTypes> MessageType { get; } = cfg.BindEx(section, MessageTypes.InWorld,
            "Type of message to show when a trophy is attracting mobs", AcceptableEnum<MessageTypes>.Default);
    }

    public sealed class SleepingConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<int> MinPlayersInBed { get; } = cfg.BindEx(section, 0,
            "Minimum number of players in bed to show the sleep prompt to the other players. 0 to require all players to be in bed (default behavior)");
        public ConfigEntry<int> RequiredPlayerPercentage { get; } = cfg.BindEx(section, 100,
            "Percentage of players that must be in bed or sitting to skip the night", new AcceptableValueRange<int>(0, 100));
        public ConfigEntry<MessageHud.MessageType> SleepPromptMessageType { get; } = cfg.BindEx(section, MessageHud.MessageType.Center,
            "Type of message to show for the sleep prompt", AcceptableEnum<MessageHud.MessageType>.Default);
    }

    public sealed class TradersConfig(ConfigFile cfg, string section)
    {
        public IReadOnlyDictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>> AlwaysUnlock { get; } = GetAlwaysUnlock(cfg, section);

        static IReadOnlyDictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>> GetAlwaysUnlock(ConfigFile cfg, string section)
        {
            if (!ZNet.instance.IsServer() || !ZNet.instance.IsDedicated())
                return new Dictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>>();

            return ZNetScene.instance.m_prefabs.Select(static x => x.GetComponent<Trader>()).Where(static x => x is not null)
                .Select(trader => (Trader: trader, Entries: (IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>)trader.m_items
                    .Where(static x => !string.IsNullOrEmpty(x.m_requiredGlobalKey))
                    .Select(item => (item.m_requiredGlobalKey, cfg.Bind(section, Invariant($"{nameof(AlwaysUnlock)}{trader.name}{item.m_prefab.name}"), false,
                        Invariant($"Remove the progression requirements for buying {Localization.instance.Localize(item.m_prefab.m_itemData.m_shared.m_name)} from {Localization.instance.Localize(trader.m_name)}"))))
                    .ToList()))
                .Where(static x => x.Entries.Any())
                .ToDictionary(static x => x.Trader, static x => x.Entries);
        }
    }

    public sealed class PlantsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> GrowTimeMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply plant grow time by this factor. 0 to make them grow almost instantly.", new AcceptableValueRange<float>(0, float.PositiveInfinity));
        public ConfigEntry<float> SpaceRequirementMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply plant space requirement by this factor. 0 to disable space requirements.", new AcceptableValueRange<float>(0, float.PositiveInfinity));
        public ConfigEntry<bool> DontDestroyIfCantGrow { get; } = cfg.BindEx(section, false, "True to keep plants that can't grow alive");
        //public ConfigEntry<bool> MakeHarvestableWithScythe { get; } = cfg.BindEx(section, false, "True to make all crops harvestable with the scythe");
    }

    public sealed class SummonsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> UnsummonDistanceMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply unsummon distance by this factor. 0 to disable distance-based unsummoning", new AcceptableValueRange<float>(0, float.PositiveInfinity));
        public ConfigEntry<float> UnsummonLogoutTimeMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply the time after which summons are unsummoned when the player logs out. 0 to disable logout-based unsummoning", new AcceptableValueRange<float>(0, float.PositiveInfinity));
    }

    public sealed class HostileSummonsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> AllowReplacementSummon { get; } = cfg.BindEx(section, false, "True to allow the summoning of new hostile summons (such as summoned trolls) to replace older ones when the limit exceeded");
        public ConfigEntry<bool> MakeFriendly { get; } = cfg.BindEx(section, false, "True to make all hostile summons (such as summoned trolls) friendly");
        public ConfigEntry<bool> FollowSummoner { get; } = cfg.BindEx(section, false, "True to make summoned creatures follow the summoner");
    }

    public sealed class TrapsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DisableTriggeredByPlayers { get; } = cfg.BindEx(section, false, "True to stop traps from being triggered by players");
        public ConfigEntry<bool> DisableFriendlyFire { get; } = cfg.BindEx(section, false, "True to stop traps from damaging players and tames. Does not work reliably (yet).");
        public ConfigEntry<float> SelfDamageMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply the damage the trap takes when it is triggered by this factor. 0 to make the trap take no damage", new AcceptableValueRange<float>(0, float.PositiveInfinity));
        public ConfigEntry<bool> AutoRearm { get; } = cfg.BindEx(section, false, "True to automatically rearm traps when they are triggered");
    }

    public sealed class NonTeleportableItemsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enable { get; } = cfg.BindEx(section, false, """
            True to enable the non-teleportable items feature.
            Items which are not teleportable by default (e.g. ores, metals, etc.) will be temporarily taken from a player's inventory when they enter a certain range around a portal so that they can travel through, according to the settings below.
            When the player leaves the range (e.g. by travelling through the portal), the items will be returned to their inventory.
            """);
            
        public ConfigEntry<float> PortalRange { get; } = cfg.BindEx(section, 4f, """
            The range around a portal in which items will be taken from a player's inventory.
            Decreasing this value will lead to a longer delay before players with non-teleportable items in their inventory can use the portal.
            Increasing this value will leave players unable to have certain items in their inventory in a larger range around portals.
            """);
            
        public ConfigEntry<MessageTypes> MessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when a non-teleportable item is taken from/returned to a player's inventory", AcceptableEnum<MessageTypes>.Default);

        public sealed record Entry(ItemDrop ItemDrop, ConfigEntry<string> Config);

        public IReadOnlyList<Entry> Entries { get; } = new Func<IReadOnlyList<Entry>>(() =>
        {
            var acceptableValues = new AcceptableValueList<string>([.. SharedProcessorState.BossesByBiome.Values
                .OrderBy(static x => x.m_health)
                .Select(static x => x.m_defeatSetGlobalKey)]);

            List<Entry> result = [];
            foreach (var item in ObjectDB.instance.m_items)
            {
                if (item.GetComponent<ItemDrop>() is not { m_itemData.m_shared.m_teleportable: false } itemDrop)
                    continue;

                var defaultValue = "";
                if (Regex.IsMatch(item.name, @"copper|tin|bronze", RegexOptions.IgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.BlackForest].m_defeatSetGlobalKey;
                else if (item.name.Contains("iron", StringComparison.OrdinalIgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.Swamp].m_defeatSetGlobalKey;
                else if (Regex.IsMatch(item.name, @"silver|DragonEgg", RegexOptions.IgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.Mountain].m_defeatSetGlobalKey;
                else if (item.name.Contains("blackmetal", StringComparison.OrdinalIgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.Plains].m_defeatSetGlobalKey;
                else if (Regex.IsMatch(item.name, @"DvergrNeedle|MechanicalSpring", RegexOptions.IgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.Mistlands].m_defeatSetGlobalKey;
                else if (Regex.IsMatch(item.name, @"flametal|CharredCogwheel", RegexOptions.IgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.AshLands].m_defeatSetGlobalKey;

                result.Add(new(itemDrop, cfg.Bind(section, item.name, defaultValue, new ConfigDescription(
                    $"Key of the boss that will allow '{Localization.instance.Localize(itemDrop.m_itemData.m_shared.m_name)}' to be teleported when defeated",
                    acceptableValues))));
            }
            return result;
        }).Invoke();
    }

    public sealed class FermentersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> FermentationDurationMultiplier { get; } = cfg.BindEx(section, 1f, "Multiply the time fermentation takes by this factor.");
    }

    public sealed class NetworkingConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MeasurePing { get; } = cfg.BindEx(section, false, "True to measure player ping");
        public ConfigEntry<int> PingStatisticsWindow { get; } = cfg.BindEx(section, 60, "Number of measurements to include for statistic calculations like mean and standard deviation",
            new AcceptableValueRange<int>(1, 100000));
        public ConfigEntry<int> LogPingThreshold { get; } = cfg.BindEx(section, 0, "A player's ping value to the server is logged if it exceeds this threshold");
        public ConfigEntry<int> ShowPingThreshold { get; } = cfg.BindEx(section, 0, "A player's ping value to the server is shown to the player if it exceeds this threshold");
        public ConfigEntry<int> LogZoneOwnerPingThreshold { get; } = cfg.BindEx(section, 0, "A player's ping value to the zone owner is logged if it exceeds this threshold");
        public ConfigEntry<int> ShowZoneOwnerPingThreshold { get; } = cfg.BindEx(section, 0, "A player's ping value to the zone owner is shown to the player if it exceeds this threshold");
        public ConfigEntry<string> LogPingFormat { get; } = cfg.BindEx(section, "Ping ({0}): {1:F0} ms (av: {2:F0} ± {3:F0} ms, jitter: {4:F0} ms)", """
            Format string for logging player ping.
            Arguments:
              0: Player name
              1: Ping value in milliseconds
              2: Mean ping of value in milliseconds
              3: Standard deviation of ping value in milliseconds
              4: Jitter in milliseconds
              5: Connection quality
            """, new AcceptableFormatString(["", 0d, 0d, 0d, 0d, 0f]));
        public ConfigEntry<string> ShowPingFormat { get; } = cfg.BindEx(section, "Ping: <color=yellow>{0:F0} ms</color> (av: {1:F0} ± {2:F0} ms, jitter: {3:F0} ms)", """
            Format string for player ping messages.
            Arguments:
              0: Ping value in milliseconds
              1: Mean ping of value in milliseconds
              2: Standard deviation of ping value in milliseconds
              3: Jitter in milliseconds
              4: Connection quality
            """, new AcceptableFormatString([0d, 0d, 0d, 0d, 0f]));
        public ConfigEntry<string> LogZoneOwnerPingFormat { get; } = cfg.BindEx(section, "Ping ({0}): {1:F0} ms (av: {2:F0} ± {3:F0} ms, jitter: {4:F0} ms) + ZoneOwner ({6}): {7:F0} ms (av: {8:F0} ± {9:F0} ms, jitter: {10:F0} ms)", """
            Format string for logging player ping.
            Arguments:
              0: Player name
              1: Ping value in milliseconds
              2: Mean ping of value in milliseconds
              3: Standard deviation of ping value in milliseconds
              4: Jitter in milliseconds
              5: Connection quality
              6: Zone owner player name
              7: Zone owner ping value in milliseconds
              8: Mean ping of zone owner ping in milliseconds
              9: Standard deviation of zone owner ping value in milliseconds
             10: Zone owner jitter in milliseconds
             11: Zone owner connection quality
            """, new AcceptableFormatString(["", 0d, 0d, 0d, 0d, 0f, "", 0d, 0d, 0d, 0d, 0f]));
        public ConfigEntry<string> ShowZoneOwnerPingFormat { get; } = cfg.BindEx(section, "Ping: <color=yellow>{0:F0} ms</color> (av: {1:F0} ± {2:F0} ms, jitter: {3:F0} ms) + <color=yellow>{5}: {6:F0} ms</color> (av: {7:F0} ± {8:F0} ms, jitter: {9:F0} ms)", """
            Format string for player ping messages.
            Arguments:
              0: Ping value in milliseconds
              1: Mean ping of value in milliseconds
              2: Standard deviation of ping value in milliseconds
              3: Jitter in milliseconds
              4: Connection quality
              5: Zone owner player name
              6: Zone owner ping value in milliseconds
              7: Mean ping of zone owner ping in milliseconds
              8: Standard deviation of zone owner ping value in milliseconds
              9: Zone owner jitter in milliseconds
             10: Zone owner connection quality
            """, new AcceptableFormatString([0d, 0d, 0d, 0d, 0f, "", 0d, 0d, 0d, 0d, 0f]));
        public ConfigEntry<bool> ReassignOwnershipBasedOnConnectionQuality { get; } = cfg.BindEx(section, false, $"""
            True to (re)assign zone ownership to the player with the best connection.
            Requires '{nameof(MeasurePing)}' to be enabled.
            The connection with the lowest connection quality value is chosen as the best connection,
            where connection quality = ping mean * {nameof(ConnectionQualityPingMeanWeight)} + ping stddev * {nameof(ConnectionQualityPingStdDevWeight)} + ping jitter * {nameof(ConnectionQualityPingJitterWeight)}
            WARNING: This feature is highly experimental and is likely to cause issues/interfere with other features
            """);
        public ConfigEntry<float> ConnectionQualityPingMeanWeight { get; } = cfg.BindEx(section, 1f,
            "Weight of ping mean when calculating connection quality");
        public ConfigEntry<float> ConnectionQualityPingStdDevWeight { get; } = cfg.BindEx(section, 1f,
            "Weight of ping standard deviation when calculating connection quality");
        public ConfigEntry<float> ConnectionQualityPingJitterWeight { get; } = cfg.BindEx(section, 0f,
            "Weight of ping jitter when calculating connection quality");
        public ConfigEntry<bool> AssignInteractablesToClosestPlayer { get; } = cfg.BindEx(section, false, """
            True to assign ownership of some interactable objects (such as smelters or cooking stations) to the closest player.
            This should help avoiding the loss of ore, etc. due to networking issues.
            """);
        public ConfigEntry<bool> AssignMobsToClosestPlayer { get; } = cfg.BindEx(section, false, """
            True to assign ownership of hostile mobs to the closest player.
            This should help reduce issues with dodging/parrying due to networking issues.
            """);
    }

    public sealed class WishboneConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> FindDungeons { get; } = cfg.BindEx(section, false, "True to make the wishbone find dungeons");
        public ConfigEntry<bool> FindVegvisir { get; } = cfg.BindEx(section, false, "True to make the wishbone find vegvisirs");
        public ConfigEntry<string> FindLocationObjectRegex { get; } = cfg.BindEx(section, "", $"""
            The wishbone will find locations which contain an object whose (prefab) name matches this regular expression.
            Example: Beehive|goblin_totempole|giant_brain|dvergrprops_crate\w*
            """);
        public ConfigEntry<float> Range { get; } = cfg.BindEx(section, Mathf.Max(Minimap.instance.m_exploreRadius, ZoneSystem.c_ZoneSize),
            "Radius in which the wishbone will react to dungeons/locations", new AcceptableValueRange<float>(0, ZoneSystem.c_ZoneSize * 2 * Mathf.Sqrt(2)));
    }

    public sealed class WorldModifiersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> SetPresetFromConfig { get; } = cfg.BindEx(section, false,
            Invariant($"True to set the world preset according to the '{nameof(Preset)}' config entry"));
        public ConfigEntry<WorldPresets> Preset { get; } = GetPreset(cfg, section);

        public ConfigEntry<bool> SetModifiersFromConfig { get; } = cfg.BindEx(section, false,
            "True to set world modifiers according to the following configuration entries");
        public IReadOnlyDictionary<WorldModifiers, ConfigEntry<WorldModifierOption>> Modifiers { get; } = GetModifiers(cfg, section);

        static ConfigEntry<WorldPresets> GetPreset(ConfigFile cfg, string section)
        {
            /// <see cref="ServerOptionsGUI.SetPreset(World, WorldPresets)"/>
            var presets = PrivateAccessor.GetServerOptionsGUIPresets();
            return cfg.Bind(section, nameof(Preset), WorldPresets.Default, new ConfigDescription(
                Invariant($"World preset. Enable '{nameof(SetPresetFromConfig)}' for this to have an effect"),
                new AcceptableEnum<WorldPresets>(presets.Select(static x => x.m_preset))));
        }

        static IReadOnlyDictionary<WorldModifiers, ConfigEntry<WorldModifierOption>> GetModifiers(ConfigFile cfg, string section)
        {
            /// <see cref="ServerOptionsGUI.SetPreset(World, WorldModifiers, WorldModifierOption)"/>
            var modifiers = PrivateAccessor.GetServerOptionsGUIModifiers()
                .OfType<KeySlider>()
                .Select(keySlider => (Key: keySlider.m_modifier, Cfg: cfg.Bind(section, Invariant($"{keySlider.m_modifier}"), WorldModifierOption.Default,
                    new ConfigDescription(Invariant($"World modifier '{keySlider.m_modifier}'. Enable '{nameof(SetModifiersFromConfig)}' for this to have an effect"),
                        new AcceptableEnum<WorldModifierOption>(keySlider.m_settings.Select(static x => x.m_modifierValue))))))
                .ToDictionary(static x => x.Key, static x => x.Cfg);
            return modifiers;
        }
    }

    public sealed class GlobalsKeysConfig(ConfigFile cfg, string section, object? tmp = null)
    {
        public ConfigEntry<bool> SetGlobalKeysFromConfig { get; } = cfg.BindEx(section, false,
            "True to set global keys according to the following configuration entries");
        public IReadOnlyDictionary<GlobalKeys, ConfigEntryBase> KeyConfigs { get; } = ((GlobalKeyConfigFinder)(tmp ??= new GlobalKeyConfigFinder()))
            .Get<GlobalKeys>(GlobalKeys.Preset, cfg, section, Invariant($"Sets the value for the '{{0}}' global key. Enable '{nameof(SetGlobalKeysFromConfig)}' for this to have an effect"));

        public ConfigEntry<bool> NoPortalsPreventsContruction { get; } = cfg.BindEx(section, true,
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
            IEnumerable<double> testValues = [float.MinValue, int.MinValue, .. Enumerable.Range(-100, 100).Select(static x => (double)x), int.MaxValue, float.MaxValue];
            Dictionary<string, string> keyTestValues = [];

            List<FieldInfoEx> fields = [.. typeof(Game).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(static x => !x.IsLiteral && !x.IsInitOnly)
                .Select(static x => new FieldInfoEx(x, x.GetValue(null), TryGetAsDouble(x)))
                .Where(static x => !double.IsNaN(x.RestoreValue))];

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
                            .FirstOrDefault(static x => x.ComparisonValue != x.Value);
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
                    var min = testResults.Min(static x => x.Value);
                    var max = testResults.Max(static x => x.Value);
                    var inRange = testResults.Where(x => x.Value is not 0 && x.Value > min && x.Value < max);
                    var multiplier = inRange.Any() ? inRange.Average(static x => x.TestValue / x.Value) : 1;
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
                AcceptableValues = [.. values.Where(static x => x.ExactlyOneBitSet())];
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
                foreach (var flag in AcceptableValues.Select(static x => x.ToUInt64()).Where(x => (val & x) == x))
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
            => $"# Acceptable values: .NET Format strings for two arguments ({string.Join(", ", testArgs.Select(static x => x.GetType().Name))}): https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-string-format#get-started-with-the-stringformat-method";
    }

    public sealed class AdvancedConfig
    {
        public TamesConfig Tames { get; init; } = new();
        public HostileSummonsConfig HostileSummons { get; init; } = new();
        public ContainerConfig Containers { get; init; } = new();

        public sealed class TamesConfig
        {
            public TeleportFollowPositioningConfig TeleportFollowPositioning { get; init; } = new(2, 4, 0, 1, 45);
            public sealed record TeleportFollowPositioningConfig(
                float MinDistXZ, float MaxDistXZ, float MinOffsetY, float MaxOffsetY, float HalfArcXZ)
            { TeleportFollowPositioningConfig() : this(default, default, default, default, default) { } }

            Dictionary<string, bool> TeleportFollow { get; init; } = [];
            IReadOnlyList<int>? _teleportFollowExcluded;
            [YamlIgnore]
            public IReadOnlyList<int> TeleportFollowExcluded => _teleportFollowExcluded ??= [.. TeleportFollow
                .Where(static x => !x.Value).Select(static x => x.Key.GetStableHashCode())];

            Dictionary<string, bool> TakeIntoDungeon { get; init; } = [];
            IReadOnlyList<int>? _takeIntoDungeonExcluded;
            [YamlIgnore]
            public IReadOnlyList<int> TakeIntoDungeonExcluded => _takeIntoDungeonExcluded ??= [.. TakeIntoDungeon
                .Where(static x => !x.Value).Select(static x => x.Key.GetStableHashCode())];

            public TamesConfig()
            {
                foreach (var prefab in ZNetScene.instance.m_prefabs)
                {
                    if (prefab.GetComponent<Tameable>() is not { } || prefab.GetComponent<BaseAI>() is not { } /*baseAI*/)
                        continue;

                    TeleportFollow.Add(prefab.name, true);
                    TakeIntoDungeon.Add(prefab.name, true);
                    //TakeIntoDungeon.Add(prefab.name, baseAI.m_pathAgentType is not Pathfinding.AgentType.TrollSize);
                }
            }
        }

        public sealed class HostileSummonsConfig
        {
            public sealed record FollowSummonerConfig(float MoveInterval, float MaxDistance) { FollowSummonerConfig() : this(default, default) { } }

            public FollowSummonerConfig FollowSummoners { get; init; } = new(4, 20);
        }

        public sealed class ContainerConfig
        {
            public sealed record ChestSignOffset(float Left, float Right, float Front, float Back, float Top) { ChestSignOffset() : this(float.NaN, float.NaN, float.NaN, float.NaN, float.NaN) { } }

            [YamlMember(Alias = nameof(ChestSignOffsets))]
            Dictionary<string, ChestSignOffset> ChestSignOffsetsYaml { get; init; } = new()
            {
                [Processor.PrefabNames.WoodChest] = new(0.8f, 0.8f, 0.4f, 0.4f, 0.8f),
                [Processor.PrefabNames.ReinforcedChest] = new(0.85f, 0.85f, 0.5f, 0.5f, 1.1f),
                [Processor.PrefabNames.BlackmetalChest] = new(0.95f, 0.95f, 0.7f, 0.7f, 0.95f),
                [Processor.PrefabNames.Barrel] = new(0.4f, 0.4f, 0.4f, 0.4f, 0.9f),
                [Processor.PrefabNames.Incinerator] = new(float.NaN, float.NaN, 0.1f, float.NaN, 3f)
            };

            IReadOnlyDictionary<int, ChestSignOffset>? _chestSignOffsets;

            [YamlIgnore]
            public IReadOnlyDictionary<int, ChestSignOffset> ChestSignOffsets => _chestSignOffsets ??= ChestSignOffsetsYaml.ToDictionary(static x => x.Key.GetStableHashCode(), static x => x.Value);
        }
    }

    static AdvancedConfig InitializeAdvancedConfig(ConfigFile cfg)
    {
        var configDir = Path.Combine(Path.GetDirectoryName(cfg.ConfigFilePath), Path.GetFileNameWithoutExtension(cfg.ConfigFilePath));
        var configPath = Path.Combine(configDir, "Advanced.yml");

        var result = new AdvancedConfig();

        var serializer = new SerializerBuilder()
            .IncludeNonPublicProperties()
            .WithTypeInspector(static x => new MyTypeInspector(x))
            .Build();

        {
            Directory.CreateDirectory(configDir);
            var defaultConfigPath = Path.ChangeExtension(configPath, "default.yml");
            using var file = new StreamWriter(defaultConfigPath, append: false);
            file.WriteLine($"# {Path.GetFileName(defaultConfigPath)} contains the default values and is overwritten regularly.");
            file.WriteLine($"# Rename it to {Path.GetFileName(configPath)} if you want to change values.");
            file.WriteLine();
            WriteYamlHeader(file);
            serializer.Serialize(file, result);
        }

        if (File.Exists(configPath))
        {
            try
            {
                using var stream = new StreamReader(configPath);
                result = new DeserializerBuilder()
                    .IncludeNonPublicProperties()
                    .EnablePrivateConstructors()
                    //.WithObjectFactory(new MyObjectFactory())
                    .WithTypeInspector(static x => new MyTypeInspector(x))
                    .Build().Deserialize<AdvancedConfig>(stream);
                Main.Instance.Logger.LogInfo($"Advanced config loaded from {Path.GetFileName(configPath)}:{Environment.NewLine}{serializer.Serialize(result)}");
            }
            catch (Exception ex)
            {
                Main.Instance.Logger.LogWarning($"{Path.GetFileName(configPath)}: {ex}");
            }
        }

        return result;
    }

    static void WriteYamlHeader(StreamWriter writer)
    {
        writer.WriteLine($"# IMPORTANT:");
        writer.WriteLine($"#   This file is for advanced tweaks. You are expected to be familiar with YAML and its pitfalls if you decide to edit it.");
        writer.WriteLine($"#   Check the log for warnings related to this file and DO NOT open issues asking for help on how to format this file.");
        writer.WriteLine();
    }

    //sealed class MyObjectFactory() : DefaultObjectFactory(new Dictionary<Type, Type>(), new() { AllowPrivateConstructors = true })
    //{
    //    public override object Create(Type type)
    //    {
    //        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
    //            type = typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments());
    //        return base.Create(type);
    //    }
    //}

    sealed class MyTypeInspector(ITypeInspector inner) : TypeInspectorSkeleton
    {
        readonly ITypeInspector _inner = inner;

        public override string GetEnumName(Type enumType, string name) => _inner.GetEnumName(enumType, name);
        public override string GetEnumValue(object enumValue) => _inner.GetEnumValue(enumValue);

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
        {
            foreach (var prop in _inner.GetProperties(type, container))
            {
                if (prop.Type == typeof(Type) && prop.Name is "EqualityContract")
                    continue;
                yield return prop;
            }
        }
    }
}
