using BepInEx.Configuration;
using UnityEngine;

namespace TestMod;

sealed class ModConfig(ConfigFile cfg)
{
    public GeneralConfig General { get; } = new(cfg, "1. General");
    public SignsConfig Signs { get; } = new(cfg, "2. Signs");
    public MapTableConfig MapTables { get; } = new(cfg, "3. Map Tables");
    public TamesConfig Tames { get; } = new(cfg, "4. Tames");
    public FireplacesConfig Fireplaces { get; } = new(cfg, "5. Fireplaces");
    public ContainersConfig Containers { get; } = new(cfg, "6. Containers");
    public SmeltersConfig Smelters { get; } = new(cfg, "7. Smelters");

    //public bool NeedsSigns => Signs.TimeSigns.Value;
    //public bool NeedsMapTables => MapTables.AutoUpdatePortals.Value || MapTables.AutoUpdateShips.Value;
    //public bool NeedsTames => Tames.MakeCommandable.Value;
    //public bool NeedsFireplaces => Fireplaces.MakeToggleable.Value;
    //public bool NeedsContainers => Containers.AutoSort.Value || Containers.AutoPickup.Value;

    public sealed class GeneralConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enabled { get; } = cfg.Bind(section, nameof(Enabled), true, "Enables/disables the entire mode");
        public ConfigEntry<float> StartDelay { get; } = cfg.Bind(section, nameof(StartDelay), 0f, "Time (in seconds) before the mod starts processing the world");
        public ConfigEntry<float> Frequency { get; } = cfg.Bind(section, nameof(Frequency), 2f, "How many times per second the mod processes the world");
        public ConfigEntry<int> MaxProcessingTime { get; } = cfg.Bind(section, nameof(MaxProcessingTime), 50, "Max processing time (in ms) per update");
        public ConfigEntry<int> ZonesAroundPlayers { get; } = cfg.Bind(section, nameof(ZonesAroundPlayers), 1, "Zones to process around each player");
        public ConfigEntry<float> MinPlayerDistance { get; } = cfg.Bind(section, nameof(MinPlayerDistance), 4f, "Min distance all players must have to a ZDO for it to be modified");
    }

    [RequiredPrefabs<Sign>]
    public sealed class SignsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> TimeSigns { get; }= cfg.Bind(section, nameof(TimeSigns), true, $"True to update sign texts which contain time emojis (any of {string.Concat(Main.ClockEmojis)}) with the in-game time");
    }

    [RequiredPrefabs<MapTable>]
    public sealed class MapTableConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> AutoUpdatePortals { get; } = cfg.Bind(section, nameof(AutoUpdatePortals), true, "True to update map tables with portal pins");

        public ConfigEntry<string> AutoUpdatePortalsExclude { get; } = cfg.Bind(section, nameof(AutoUpdatePortalsExclude), "", "Portals with a tag that matches this filter are not added to map tables");
        public ConfigEntry<string> AutoUpdatePortalsInclude { get; } = cfg.Bind(section, nameof(AutoUpdatePortalsInclude), "", "Only portals with a tag that matches this filter are added to map tables");

        [RequiredPrefabs<Ship>]
        public ConfigEntry<bool> AutoUpdateShips { get; } = cfg.Bind(section, nameof(AutoUpdateShips), true, "True to update map tables with ship pins");
    }

    [RequiredPrefabs<Tameable>]
    public sealed class TamesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MakeCommandable { get; } = cfg.Bind(section, nameof(MakeCommandable), true, "True to make all tames commandable (like wolves)");
        //public ConfigEntry<bool> FeedFromContainers { get; } = cfg.Bind(section, nameof(FeedFromContainers), true, "True to feed tames from containers");
    }

    [RequiredPrefabs<Fireplace>]
    public sealed class FireplacesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MakeToggleable { get; } = cfg.Bind(section, nameof(MakeToggleable), true, "True to make all fireplaces (including torches, braziers, etc.) toggleable");
    }

    [RequiredPrefabs<Container, Piece> /* Require Container and Piece */]
    public sealed class ContainersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> AutoSort { get; } = cfg.Bind(section, nameof(AutoSort), true, "True to auto sort container inventories");
        [RequiredPrefabs<ItemDrop>]
        public ConfigEntry<bool> AutoPickup { get; } = cfg.Bind(section, nameof(AutoPickup), true, "True to automatically put dropped items into containers if they already contain said item");
        public ConfigEntry<float> AutoPickupRange { get; } = cfg.Bind(section, nameof(AutoPickupRange), ZoneSystem.c_ZoneSize, "Required proximity of a container to a dropped item to be considered as auto pickup target");
        public ConfigEntry<float> AutoPickupMinPlayerDistance { get; } = cfg.Bind(section, nameof(AutoPickupMinPlayerDistance), 8f, "Min distance all player must have to a dropped item for it to be picked up");
    }

    [RequiredPrefabs<Smelter>]
    public sealed class SmeltersConfig(ConfigFile cfg, string section)
    {
        [RequiredPrefabs<Container, Piece>]
        public ConfigEntry<bool> FeedFromContainers { get; } = cfg.Bind(section, nameof(FeedFromContainers), true, "True to automatically feed smelters from nearby containers");
        public ConfigEntry<float> FeedFromContainersRange { get; } = cfg.Bind(section, nameof(FeedFromContainersRange), 4f, "Required proxmity of a container to a smelter to be used as feeding source");
    }
}

abstract class RequiredPrefabsAttribute(params Type[] prefabs) : Attribute
{
    public Type[] Prefabs { get; } = prefabs;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
sealed class RequiredPrefabsAttribute<T> : RequiredPrefabsAttribute where T : Component
{
    public RequiredPrefabsAttribute() : base(typeof(T)) { }
}


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
sealed class RequiredPrefabsAttribute<T1, T2> : RequiredPrefabsAttribute where T1 : Component where T2 : Component
{
    public RequiredPrefabsAttribute() : base(typeof(T1), typeof(T2)) { }
}