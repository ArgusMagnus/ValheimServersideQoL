using BepInEx.Configuration;
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
    public FireplacesConfig Fireplaces { get; } = new(cfg, "B - Fireplaces");
    public ContainersConfig Containers { get; } = new(cfg, "B - Containers");
    public SmeltersConfig Smelters { get; } = new(cfg, "B - Smelters");
    public WindmillsConfig Windmills { get; } = new(cfg, "B - Windmills");
    public CartsConfig Carts { get; } = new(cfg, "B - Carts");
    public DoorsConfig Doors { get; } = new(cfg, "B - Doors");


    //PrefabsConfig? _prefabs;
    //public PrefabsConfig? Prefabs => _prefabs ??= ZNetScene.instance is null ? null : new PrefabsConfig(cfg, "C - Prefabs", ZNetScene.instance);

    public sealed class GeneralConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enabled { get; } = cfg.Bind(section, nameof(Enabled), true, "Enables/disables the entire mode");
        public ConfigEntry<float> StartDelay { get; } = cfg.Bind(section, nameof(StartDelay), 0f, "Time (in seconds) before the mod starts processing the world");
        public ConfigEntry<float> Frequency { get; } = cfg.Bind(section, nameof(Frequency), 5f, "How many times per second the mod processes the world");
        public ConfigEntry<int> MaxProcessingTime { get; } = cfg.Bind(section, nameof(MaxProcessingTime), 20, "Max processing time (in ms) per update");
        public ConfigEntry<int> ZonesAroundPlayers { get; } = cfg.Bind(section, nameof(ZonesAroundPlayers), 1, "Zones to process around each player");
        public ConfigEntry<float> MinPlayerDistance { get; } = cfg.Bind(section, nameof(MinPlayerDistance), 4f, "Min distance all players must have to a ZDO for it to be modified");
        public ConfigEntry<bool> IgnoreGameVersionCheck { get; } = cfg.Bind(section, nameof(IgnoreGameVersionCheck), false, "True to ignore the game version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreNetworkVersionCheck { get; } = cfg.Bind(section, nameof(IgnoreNetworkVersionCheck), false, "True to ignore the network version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreItemDataVersionCheck { get; } = cfg.Bind(section, nameof(IgnoreItemDataVersionCheck), false, "True to ignore the item data version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
        public ConfigEntry<bool> IgnoreWorldVersionCheck { get; } = cfg.Bind(section, nameof(IgnoreWorldVersionCheck), false, "True to ignore the world version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption");
    }

    [RequiredPrefabs<Sign>]
    public sealed class SignsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> TimeSigns { get; }= cfg.Bind(section, nameof(TimeSigns), true, $"True to update sign texts which contain time emojis (any of {string.Concat(SignProcessor.ClockEmojis)}) with the in-game time");
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
        [RequiredPrefabs<Character>]
        public ConfigEntry<bool> SendTamingPogressMessages { get; } = cfg.Bind(section, nameof(SendTamingPogressMessages), true, "True to send taming progress messages to nearby players");
        public ConfigEntry<bool> AlwaysFed { get; } = cfg.Bind(section, nameof(AlwaysFed), false, "True to make tames always fed (not hungry)");
        [RequiredPrefabs<Player>]
        public ConfigEntry<bool> TeleportFollow { get; } = cfg.Bind(section, nameof(TeleportFollow), true, "True to teleport following tames to the players location if the player gets too far away from them");
    }

    [RequiredPrefabs<Fireplace>]
    public sealed class FireplacesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MakeToggleable { get; } = cfg.Bind(section, nameof(MakeToggleable), true, "True to make all fireplaces (including torches, braziers, etc.) toggleable");
        public ConfigEntry<bool> InfiniteFuel { get; } = cfg.Bind(section, nameof(InfiniteFuel), true, "True to make all fireplaces have infinite fuel");
    }

    [RequiredPrefabs<Container, Piece> /* Require Container and Piece */]
    [RequiredPrefabs<Container, Piece, ZSyncTransform>]
    public sealed class ContainersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> AutoSort { get; } = cfg.Bind(section, nameof(AutoSort), true, "True to auto sort container inventories");
        [RequiredPrefabs<ItemDrop>]
        [RequiredPrefabs<ItemDrop, Piece>]
        public ConfigEntry<bool> AutoPickup { get; } = cfg.Bind(section, nameof(AutoPickup), true, "True to automatically put dropped items into containers if they already contain said item");
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

    [RequiredPrefabs<Smelter>]
    public sealed class SmeltersConfig(ConfigFile cfg, string section)
    {
        [RequiredPrefabs<Container, Piece>]
        public ConfigEntry<bool> FeedFromContainers { get; } = cfg.Bind(section, nameof(FeedFromContainers), true, "True to automatically feed smelters from nearby containers");
        public ConfigEntry<float> FeedFromContainersRange { get; } = cfg.Bind(section, nameof(FeedFromContainersRange), 4f, "Required proxmity of a container to a smelter to be used as feeding source");
        public ConfigEntry<int> FeedFromContainersLeaveAtLeastFuel { get; } = cfg.Bind(section, nameof(FeedFromContainersLeaveAtLeastFuel), 1, "Minimum amout of fuel to leave in a container");
        public ConfigEntry<int> FeedFromContainersLeaveAtLeastOre { get; } = cfg.Bind(section, nameof(FeedFromContainersLeaveAtLeastOre), 1, "Minimum amout of ore to leave in a container");
    }

    [RequiredPrefabs<Windmill>]
    public sealed class WindmillsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> IgnoreWind { get; } = cfg.Bind(section, nameof(IgnoreWind), true, "True to make windmills ignore wind (Cover still decreases operating efficiency though)");
    }

    [RequiredPrefabs<Vagon>]
    public sealed class CartsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> ContentMassMultiplier { get; } = cfg.Bind(section, nameof(ContentMassMultiplier), float.NaN, "Multiplier for a carts content weight. E.g. set to 0 to ignore a cart's content weight");
    }

    [RequiredPrefabs<Door>]
    public sealed class DoorsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> AutoCloseMinPlayerDistance { get; } = cfg.Bind(section, nameof(AutoCloseMinPlayerDistance), 8f,
            $"Min distance all players must have to the door before it is closed. {float.NaN} to disable this feature");
    }

    public sealed class GlobalsKeysConfig(ConfigFile cfg, string section)
    {
        ConfigEntry<string>? _preset;
        public ConfigEntry<string> Preset => _preset ??= GetPreset(cfg, section);

        IReadOnlyDictionary<string, ConfigEntry<string>>? _modifiers;
        public IReadOnlyDictionary<string, ConfigEntry<string>> Modifiers => _modifiers ??= GetModifiers(cfg, section);

        IReadOnlyDictionary<GlobalKeys, ConfigEntryBase>? _keyConfigs;
        public IReadOnlyDictionary<GlobalKeys, ConfigEntryBase> KeyConfigs => _keyConfigs ??= GetGlobalKeyEntries(cfg, section);

        [RequiredPrefabs<TeleportWorld>]
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

    //public sealed class PrefabsConfig
    //{
    //    public IReadOnlyList<ConfigEntryBase> Entries { get; }

    //    public PrefabsConfig(ConfigFile cfg, string section, ZNetScene zNetScene)
    //    {
    //        static Func<string, object?, ConfigEntryBase> GetBind(ConfigFile cfg, string section, Type type)
    //        {
    //            var valueType = type;
    //            var parName = Expression.Parameter(typeof(string));
    //            var parValue = Expression.Parameter(type);
    //            Expression argValue = parValue;
    //            if (type == typeof(GameObject) || type == typeof(ItemDrop))
    //            {
    //                valueType = typeof(string);
    //                argValue = Expression.Condition(
    //                    Expression.ReferenceEqual(parValue, Expression.Constant(null, type)),
    //                    Expression.Constant(""),
    //                    Expression.Property(parValue, nameof(UnityEngine.Object.name)));
    //            }
    //            return Expression.Lambda<Func<string, object?, ConfigEntryBase>>(
    //                Expression.Call(
    //                    Expression.Constant(cfg),
    //                    typeof(ConfigFile).GetMethod(nameof(ConfigFile.Bind)).MakeGenericMethod(valueType),
    //                    Expression.Constant(section), parName, argValue, Expression.Constant("")),
    //                parName, parValue).Compile();
    //        }

    //        /// <see cref="ZNetView.LoadFields()"/>
    //        var supportedFieldTypes = ((IEnumerable<Type>)[typeof(int), typeof(float), typeof(bool), typeof(Vector3), typeof(string), typeof(GameObject), typeof(ItemDrop)])
    //            .ToDictionary(x => x, x => GetBind(cfg, section, x));

    //        var componentInfo = typeof(Game).Assembly.ExportedTypes.Where(x => x.IsClass && typeof(MonoBehaviour).IsAssignableFrom(x))
    //            .Select(x => (Type: x, Fields: x
    //                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
    //                .Where(y => supportedFieldTypes.ContainsKey(y.FieldType))
    //                .ToList()))
    //            .Where(x => x.Fields.Count > 0)
    //            .ToList();

    //        List<ConfigEntryBase> entries = [];
    //        Entries = entries;

    //        foreach (var prefab in zNetScene.m_prefabs)
    //        {
    //            foreach (var (componentType, fields) in componentInfo)
    //            {
    //                if (prefab.GetComponent(componentType) is not { } component)
    //                    continue;

    //                foreach (var field in fields)
    //                {
    //                    var name = $"{prefab.name}.{componentType.Name}.{field.Name}";
    //                    var value = field.GetValue(component);
    //                    entries.Add(supportedFieldTypes[field.FieldType].Invoke(name, value));
    //                }
    //            }
    //        }
    //    }
    //}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
abstract class RequiredPrefabsAttribute(params Type[] prefabs) : Attribute
{
    public Type[] Prefabs { get; } = prefabs;
}

sealed class RequiredPrefabsAttribute<T>() : RequiredPrefabsAttribute(typeof(T))
    where T : MonoBehaviour;

sealed class RequiredPrefabsAttribute<T1, T2>() : RequiredPrefabsAttribute(typeof(T1), typeof(T2))
    where T1 : MonoBehaviour where T2 : MonoBehaviour;

sealed class RequiredPrefabsAttribute<T1, T2, T3>() : RequiredPrefabsAttribute(typeof(T1), typeof(T2), typeof(T3))
    where T1 : MonoBehaviour where T2 : MonoBehaviour where T3 : MonoBehaviour;