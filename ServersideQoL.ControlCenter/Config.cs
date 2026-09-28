using BepInEx.Configuration;
using ServersideQoL.Utilities;
using System.Reflection;

namespace ServersideQoL.ControlCenter;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");

  const WorldPresets NoPreset = (WorldPresets)(-1);
  public ConfigEntry<WorldPresets> Preset { get; } = BindEx(cfg, nameof(WorldModifiers), NoPreset,
    $"The world preset to set. {NoPreset} to not set any preset",
    new AcceptableEnum<WorldPresets>([NoPreset, .. ServerOptionsGUI.m_presets.Select(static x => x.m_preset)]));

  const WorldModifierOption NoModifier = (WorldModifierOption)(-1);
  public IReadOnlyDictionary<ConfigEntry<WorldModifierOption>, WorldModifiers> WorldModifiers { get; } = ServerOptionsGUI.m_modifiers
    .OfType<KeySlider>()
    .Select(keySlider => (Key: keySlider.m_modifier, Cfg: BindEx(cfg, nameof(WorldModifiers), NoModifier,
      Invariant($"World modifier '{keySlider.m_modifier}'. {NoModifier} to not set the world modifier"),
      new AcceptableEnum<WorldModifierOption>([NoModifier, .. keySlider.m_settings.Select(static x => x.m_modifierValue)]))))
    .ToDictionary(static x => x.Cfg, static x => x.Key);

  public IReadOnlyList<ConfigEntryBase> GlobalKeys { get; } = Get(global::GlobalKeys.Preset, cfg, "Sets the value for the '{0}' global key", nameof(GlobalKeys));

  sealed record FieldInfoEx(FieldInfo Field, object? RestoreValueObject, double RestoreValue)
  {
    public double ComparisonValue { get; set; } = double.NaN;
  }

  static IReadOnlyList<ConfigEntryBase> Get(GlobalKeys? maxEclusive, ConfigFile cfg, string descriptionFormat, string section)
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
    try { Game.UpdateWorldRates(new HashSet<string>(), keyTestValues); }
    catch (NullReferenceException) { } /// expect in <see cref="Game.UpdateNoMap"/>

    foreach (var field in fields)
      field.ComparisonValue = TryGetAsDouble(field.Field);

    var result = new List<ConfigEntryBase>();
    var emptySet = new HashSet<string>();
    foreach (GlobalKeys key in Enum.GetValues(typeof(GlobalKeys)))
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
        emptySet.Clear();
        try { Game.UpdateWorldRates(emptySet, keyTestValues); }
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
        bindDefinition ??= new Func<ConfigFile, string, bool, string, AcceptableValueBase?, Deprecated?, string, ConfigEntry<bool>>(BindEx).Method.GetGenericMethodDefinition();
        var entry = (ConfigEntryBase)bindDefinition.MakeGenericMethod(field.FieldType).Invoke(null, [cfg, section, Convert.ChangeType(comparisonValue, field.FieldType), string.Format(descriptionFormat, name), range, null, name]);
        result.Add(entry);
      }
      else
      {
        result.Add(BindEx(cfg, section, false, string.Format(descriptionFormat, name), null, null, key: name));
      }

      field?.SetValue(null, restoreValueObject);
    }

    foreach (var field in fields)
      field.Field.SetValue(null, field.RestoreValueObject);

    return result;

    static double TryGetAsDouble(FieldInfo field)
    {
      var obj = field.GetValue(null);
      try { return (double)Convert.ChangeType(obj, typeof(double)); }
      catch { return double.NaN; }
    }
  }
}
