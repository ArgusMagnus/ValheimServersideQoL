using BepInEx.Configuration;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

sealed class ModConfig(ConfigFile cfg)
{
    public GeneralConfig General { get; } = new(cfg, "A - General");
    public GlobalsKeysConfig GlobalsKeys { get; } = new(cfg, "B - Global Keys");
    public SignsConfig Signs { get; } = new(cfg, "B - Signs");
    public MapTableConfig MapTables { get; } = new(cfg, "B - Map Tables");
    public TamesConfig Tames { get; } = new(cfg, "B - Tames");
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
    public TradersConfig Traders { get; } = new(cfg, "B - Traders");
    public PlantsConfig Plants { get; } = new(cfg, "B - Plants");

    public sealed class GeneralConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enabled { get; } = cfg.Bind(section, nameof(Enabled), true, "Enables/disables the entire mode");
        public ConfigEntry<bool> DiagnosticLogs { get; } = cfg.Bind(section, nameof(DiagnosticLogs), false, "Enables/disables diagnostic logs");
        public ConfigEntry<float> StartDelay { get; } = cfg.Bind(section, nameof(StartDelay), 0f, "Time (in seconds) before the mod starts processing the world");
        public ConfigEntry<float> Frequency { get; } = cfg.Bind(section, nameof(Frequency), 5f, "How many times per second the mod processes the world");
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
        public ConfigEntry<bool> TimeSigns { get; }= cfg.Bind(section, nameof(TimeSigns), false, $"True to update sign texts which contain time emojis (any of {string.Concat(SignProcessor.ClockEmojis)}) with the in-game time");
    }

    public sealed class MapTableConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> AutoUpdatePortals { get; } = cfg.Bind(section, nameof(AutoUpdatePortals), false, "True to update map tables with portal pins");
        public ConfigEntry<string> AutoUpdatePortalsExclude { get; } = cfg.Bind(section, nameof(AutoUpdatePortalsExclude), "", "Portals with a tag that matches this filter are not added to map tables");
        public ConfigEntry<string> AutoUpdatePortalsInclude { get; } = cfg.Bind(section, nameof(AutoUpdatePortalsInclude), "*", "Only portals with a tag that matches this filter are added to map tables");

        public ConfigEntry<bool> AutoUpdateShips { get; } = cfg.Bind(section, nameof(AutoUpdateShips), false, "True to update map tables with ship pins");
    }

    public sealed class TamesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MakeCommandable { get; } = cfg.Bind(section, nameof(MakeCommandable), false, "True to make all tames commandable (like wolves)");
        //public ConfigEntry<bool> FeedFromContainers { get; } = cfg.Bind(section, nameof(FeedFromContainers), false, "True to feed tames from containers");

        public ConfigEntry<bool> SendTamingPogressMessages { get; } = cfg.Bind(section, nameof(SendTamingPogressMessages), false, "True to send taming progress messages to nearby players");
        public ConfigEntry<bool> AlwaysFed { get; } = cfg.Bind(section, nameof(AlwaysFed), false, "True to make tames always fed (not hungry)");

        public ConfigEntry<bool> TeleportFollow { get; } = cfg.Bind(section, nameof(TeleportFollow), false, "True to teleport following tames to the players location if the player gets too far away from them");
    }

    public sealed class FireplacesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MakeToggleable { get; } = cfg.Bind(section, nameof(MakeToggleable), false, "True to make all fireplaces (including torches, braziers, etc.) toggleable");
        public ConfigEntry<bool> InfiniteFuel { get; } = cfg.Bind(section, nameof(InfiniteFuel), false, "True to make all fireplaces have infinite fuel");
    }

    public sealed class ContainersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> AutoSort { get; } = cfg.Bind(section, nameof(AutoSort), false, "True to auto sort container inventories");

        public ConfigEntry<bool> AutoPickup { get; } = cfg.Bind(section, nameof(AutoPickup), false, "True to automatically put dropped items into containers if they already contain said item");
        public ConfigEntry<float> AutoPickupRange { get; } = cfg.Bind(section, nameof(AutoPickupRange), ZoneSystem.c_ZoneSize, "Required proximity of a container to a dropped item to be considered as auto pickup target");
        public ConfigEntry<float> AutoPickupMinPlayerDistance { get; } = cfg.Bind(section, nameof(AutoPickupMinPlayerDistance), 8f, "Min distance all player must have to a dropped item for it to be picked up");

        IReadOnlyDictionary<int, ConfigEntry<string>>? _containerSizes;
        public IReadOnlyDictionary<int, ConfigEntry<string>> ContainerSizes => _containerSizes ??= ZNetScene.instance.m_prefabs
            .Where(x => SharedProcessorState.PieceTablesByPiece.ContainsKey(x.name))
            .Select(x => (Name: x.name, Container: x.GetComponent<Container>() ?? x.GetComponentInChildren<Container>(), Piece: x.GetComponent<Piece>()))
            .Where(x => x is { Container: not null, Piece: not null })
            .ToDictionary(x => x.Name.GetStableHashCode(), x => cfg
                .Bind(section, $"InventorySize_{x.Name}", $"{x.Container.m_width}x{x.Container.m_height}", $"Inventory size for '{Localization.instance.Localize(x.Piece.m_name)}'"));
    }

    public sealed class SmeltersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> FeedFromContainers { get; } = cfg.Bind(section, nameof(FeedFromContainers), false, "True to automatically feed smelters from nearby containers");
        public ConfigEntry<float> FeedFromContainersRange { get; } = cfg.Bind(section, nameof(FeedFromContainersRange), 4f, "Required proxmity of a container to a smelter to be used as feeding source");
        public ConfigEntry<int> FeedFromContainersLeaveAtLeastFuel { get; } = cfg.Bind(section, nameof(FeedFromContainersLeaveAtLeastFuel), 1, "Minimum amout of fuel to leave in a container");
        public ConfigEntry<int> FeedFromContainersLeaveAtLeastOre { get; } = cfg.Bind(section, nameof(FeedFromContainersLeaveAtLeastOre), 1, "Minimum amout of ore to leave in a container");
    }

    public sealed class WindmillsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> IgnoreWind { get; } = cfg.Bind(section, nameof(IgnoreWind), false, "True to make windmills ignore wind (Cover still decreases operating efficiency though)");
    }

    public sealed class CartsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> ContentMassMultiplier { get; } = cfg.Bind(section, nameof(ContentMassMultiplier), 1f, "Multiplier for a carts content weight. E.g. set to 0 to ignore a cart's content weight");
    }

    public sealed class DoorsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> AutoCloseMinPlayerDistance { get; } = cfg.Bind(section, nameof(AutoCloseMinPlayerDistance), float.NaN,
            $"Min distance all players must have to the door before it is closed. {float.NaN} to disable this feature");
    }

    public sealed class PlayersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> InfiniteBuildingStamina { get; } = cfg.Bind(section, nameof(InfiniteBuildingStamina), false,
            $"True to give players infinite stamina when building. If you want infinite stamina in general, set the global key '{nameof(GlobalKeys.StaminaRate)}' to 0");
        public ConfigEntry<bool> InfiniteFarmingStamina { get; } = cfg.Bind(section, nameof(InfiniteFarmingStamina), false,
            $"True to give players infinite stamina when farming. If you want infinite stamina in general, set the global key '{nameof(GlobalKeys.StaminaRate)}' to 0");
    }

    public sealed class TurretsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DontTargetPlayers { get; } = cfg.Bind(section, nameof(DontTargetPlayers), false, "True to stop ballistas from targeting players");
        public ConfigEntry<bool> DontTargetTames { get; } = cfg.Bind(section, nameof(DontTargetTames), false, "True to stop ballistas from targeting tames");
        public ConfigEntry<bool> LoadFromContainers { get; } = cfg.Bind(section, nameof(LoadFromContainers), false, "True to automatically load ballistas from containers");
        public ConfigEntry<float> LoadFromContainersRange { get; } = cfg.Bind(section, nameof(LoadFromContainersRange), 4f, "Required proxmity of a container to a ballista to be used as ammo source");
    }

    public sealed class WearNTearConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DisableRainDamage { get; } = cfg.Bind(section, nameof(DisableRainDamage), false, "True to prevent rain from damaging build pieces");

        public ConfigEntry<DisableSupportRequirementsOptions> DisableSupportRequirements { get; } = cfg.Bind(section, nameof(DisableSupportRequirements), DisableSupportRequirementsOptions.None,
            "Ignore support requirements on build pieces");

        [Flags]
        public enum DisableSupportRequirementsOptions
        {
            None,
            PlayerBuilt = (1 << 0),
            World = (1 << 1)
        }
    }

    public sealed class GlobalsKeysConfig(ConfigFile cfg, string section)
    {
        ConfigEntry<string>? _preset;
        public ConfigEntry<string> Preset => _preset ??= GetPreset(cfg, section);

        IReadOnlyDictionary<string, ConfigEntry<string>>? _modifiers;
        public IReadOnlyDictionary<string, ConfigEntry<string>> Modifiers => _modifiers ??= GetModifiers(cfg, section);

        IReadOnlyDictionary<GlobalKeys, ConfigEntryBase>? _keyConfigs;
        public IReadOnlyDictionary<GlobalKeys, ConfigEntryBase> KeyConfigs => _keyConfigs ??= GetGlobalKeyEntries(cfg, section);

        public ConfigEntry<bool> NoPortalsPreventsContruction { get; } = cfg.Bind(section, nameof(NoPortalsPreventsContruction), true,
            $"True to change the effect of the '{GlobalKeys.NoPortals}' global key, to prevent the construction of new portals but leave existing portals functional");

        static ConfigEntry<string> GetPreset(ConfigFile cfg, string section)
        {
            /// <see cref="ServerOptionsGUI.SetPreset(World, WorldPresets)"/>
            var presets = PrivateAccessor.GetServerOptionsGUIPresets();
            return cfg.Bind(section, nameof(Preset), "", new ConfigDescription("World preset",
                new AcceptableValueList<string>(["", .. presets.Select(x => $"{x.m_preset}")])));
        }

        static IReadOnlyDictionary<string, ConfigEntry<string>> GetModifiers(ConfigFile cfg, string section)
        {
            /// <see cref="ServerOptionsGUI.SetPreset(World, WorldModifiers, WorldModifierOption)"/>
            var modifiers = PrivateAccessor.GetServerOptionsGUIModifiers()
                .OfType<KeySlider>()
                .Select(keySlider => cfg.Bind(section, $"{keySlider.m_modifier}", "",
                    new ConfigDescription($"World modifier '{keySlider.m_modifier}'",
                        new AcceptableValueList<string>(["", .. keySlider.m_settings.Select(x => $"{x.m_modifierValue}")]))))
                .ToDictionary(x => x.Definition.Key);
            return modifiers;
        }

        static IReadOnlyDictionary<GlobalKeys, ConfigEntryBase> GetGlobalKeyEntries(ConfigFile cfg, string section)
        {
            static double TryGetAsDouble(FieldInfo field)
            {
                var obj = field.GetValue(null);
                try { return (double)Convert.ChangeType(obj, typeof(double)); }
                catch { return double.NaN; }
            }

            /// <see cref="ZoneSystem.GetGlobalKey(GlobalKeys, out string)"/>
            /// <see cref="Game.UpdateWorldRates(HashSet{string}, Dictionary{string, string})"/>
            var fields = typeof(Game).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(x => (Field: x, OrignalValueObject: x.GetValue(null), OriginalValue: TryGetAsDouble(x)))
                .Where(x => !double.IsNaN(x.OriginalValue))
                .ToList();

            IEnumerable<double> testValues = [float.MinValue, int.MinValue];
            testValues = testValues.Concat(Enumerable.Range(-100, 100).Select(x => (double)x));
            testValues = testValues.Concat([int.MaxValue, float.MaxValue]);

            HashSet<string> keys = [];
            Dictionary<string, string> keyTestValues = new();
            List<(double TestValue, double Value)> testResults = new();

            var result = new Dictionary<GlobalKeys, ConfigEntryBase>();

            MethodInfo? bindDefinition = null;

            foreach (GlobalKeys key in Enum.GetValues(typeof(GlobalKeys)))
            {
                if (key is GlobalKeys.Preset or >= GlobalKeys.NonServerOption)
                    continue;

                var name = key.ToString();
                var nameLower = name.ToLower();

                FieldInfo? field = null;
                object? orignalValueObject = null;
                double originalValue = double.NaN;
                testResults.Clear();
                foreach (var testValue in testValues)
                {
                    keyTestValues.Clear();
                    keyTestValues.Add(nameLower, FormattableString.Invariant($"{testValue}"));
                    try { Game.UpdateWorldRates(keys, keyTestValues); }
                    catch (NullReferenceException) { } /// expect in <see cref="Game.UpdateNoMap"/>
                    double value = double.NaN;
                    if (field is null)
                    {
                        (field, orignalValueObject, originalValue, value, var idx) = fields.Select((x, i) => (x.Field, x.OrignalValueObject, x.OriginalValue, Value: TryGetAsDouble(x.Field), i)).FirstOrDefault(x => x.OriginalValue != x.Value);
                        if (field is not null)
                            fields.RemoveAt(idx);
                    }
                    else
                    {
                        value = TryGetAsDouble(field);
                        if (value == originalValue)
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
                    originalValue *= multiplier;

                    AcceptableValueBase? range = null;
                    if (min > float.MinValue && max < float.MaxValue)
                        range = (AcceptableValueBase)Activator.CreateInstance(typeof(AcceptableValueRange<>).MakeGenericType(field.FieldType), Convert.ChangeType(min, field.FieldType), Convert.ChangeType(max, field.FieldType));
                    var desc = new ConfigDescription($"Sets the value for the '{name}' global key", range);
                    bindDefinition ??= new Func<string, string, bool, ConfigDescription, ConfigEntry<bool>>(cfg.Bind).Method.GetGenericMethodDefinition();
                    var entry = (ConfigEntryBase)bindDefinition.MakeGenericMethod(field.FieldType).Invoke(cfg, [section, name, Convert.ChangeType(originalValue, field.FieldType), desc]);
                    result.Add(key, entry);
                }
                else
                {
                    result.Add(key, cfg.Bind(section, name, false, $"True to set the '{name}' global key"));
                }

                field?.SetValue(null, orignalValueObject);
            }

            return result;
        }
    }

    public sealed class TradersConfig(ConfigFile cfg, string section)
    {
        IReadOnlyDictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>>? _alwaysUnlock;
        public IReadOnlyDictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>> AlwaysUnlock => _alwaysUnlock ??= GetAlwaysUnlock(cfg, section);

        static IReadOnlyDictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>> GetAlwaysUnlock(ConfigFile cfg, string section)
        {
            if (!ZNet.instance.IsServer() || !ZNet.instance.IsDedicated())
                return new Dictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>>();

            return ZNetScene.instance.m_prefabs.Select(x => x.GetComponent<Trader>()).Where(x => x is not null)
                .Select(trader => (Trader: trader, Entries: (IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>)trader.m_items
                    .Where(x => !string.IsNullOrEmpty(x.m_requiredGlobalKey))
                    .Select(item => (item.m_requiredGlobalKey, cfg.Bind(section, $"{nameof(AlwaysUnlock)}{trader.name}{item.m_prefab.name}", false,
                        $"Remove the progression requirements for buying {Localization.instance.Localize(item.m_prefab.m_itemData.m_shared.m_name)} from {Localization.instance.Localize(trader.m_name)}")))
                    .ToList()))
                .Where(x => x.Entries.Any())
                .ToDictionary(x => x.Trader, x => x.Entries);
        }
    }

    public sealed class PlantsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> GrowTimeMultiplier { get; } = cfg.Bind(section, nameof(GrowTimeMultiplier), 1f, "Multiply plant grow time by this factor");
        public ConfigEntry<float> SpaceRequirementMultiplier { get; } = cfg.Bind(section, nameof(SpaceRequirementMultiplier), 1f, "Multiply plant grow time by this factor");
        public ConfigEntry<bool> DontDestroyIfCantGrow { get; } = cfg.Bind(section, nameof(DontDestroyIfCantGrow), false, "True to keep plants which can't grow alive");
    }

    public sealed class SummonsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> UnsummonDistanceMultiplier { get; } = cfg.Bind(section, nameof(UnsummonDistanceMultiplier), 1f, "Multiply unsummon distance by this factor. 0 to disable distance-based unsummoning");
        public ConfigEntry<float> UnsummonLogoutTimeMultiplier { get; } = cfg.Bind(section, nameof(UnsummonLogoutTimeMultiplier), 1f, "Multiply the time after which summons are unsummoned when the player logs out. 0 to disable logout-based unsummoning");
    }
}