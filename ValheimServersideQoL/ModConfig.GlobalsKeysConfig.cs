using BepInEx.Configuration;
using System.Reflection;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class GlobalsKeysConfig(ConfigFile cfg, string section, object? tmp = null)
    {
        public ConfigEntry<bool> SetGlobalKeysFromConfig { get; } = cfg.BindEx(section, false,
            "True to set global keys according to the following configuration entries");
        public IReadOnlyDictionary<GlobalKeys, ConfigEntryBase> KeyConfigs { get; } = ((GlobalKeyConfigFinder)(tmp ??= new GlobalKeyConfigFinder()))
            .Get<GlobalKeys>(GlobalKeys.Preset, cfg, section, Invariant(
                $"Sets the value for the '{{0}}' global key. Enable '{nameof(SetGlobalKeysFromConfig)}' for this to have an effect"));

        public ConfigEntry<bool> NoPortalsPreventsContruction { get; } = cfg.BindEx(section, true,
            Invariant($"True to change the effect of the '{GlobalKeys.NoPortals}' global key, to prevent the construction of new portals but leave existing portals functional"));
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
            List<(double TestValue, double Value)> testResults = new();
            IEnumerable<double> testValues = [float.MinValue, int.MinValue, .. Enumerable.Range(-100, 100).Select(static x => (double)x), int.MaxValue, float.MaxValue];
            Dictionary<string, string> keyTestValues = new();

            List<FieldInfoEx> fields = [.. typeof(Game).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static x => !x.IsLiteral && !x.IsInitOnly)
            .Select(static x => new FieldInfoEx(x, x.GetValue(null), TryGetAsDouble(x)))
            .Where(static x => !double.IsNaN(x.RestoreValue))];

            MethodInfo? bindDefinition = null;

            // set all fields to default values (in case they were changed before this method is called)
            try { Game.UpdateWorldRates(new HashSet<string>(), keyTestValues); }
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
                    try { Game.UpdateWorldRates(new HashSet<string>(), keyTestValues); }
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
}