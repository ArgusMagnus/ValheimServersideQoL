using BepInEx.Configuration;
using ServersideQoL.Utilities;
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace ServersideQoL;

interface IConfig
{
  void RaiseInitialized();
  event EventHandler<SettingChangedEventArgs>? ConfigChanged;
  IServersideQoLPlugin Plugin { get; set; }
  ConfigEntry<bool> Enabled { get; }
  ConfigFile ConfigFile { get; }
}

public abstract class ConfigBase
{
  private protected ConfigBase() { }

  protected static class Shared
  {
    public static ConfigEntry<bool>? AutoPickup { get; set => Set(ref field, value); }
    public static ConfigEntry<int>? AutoPickupMaxRange { get; set => Set(ref field, value); }
    public static ConfigEntry<bool>? FeedFromContainers { get; set => Set(ref field, value); }
    public static ConfigEntry<int>? FeedFromContainersMaxRange { get; set => Set(ref field, value); }

    static void Set<T>(ref T? field, T? value, [CallerMemberName] string configName = default!) where T : class
    {
      if (field is not null)
        throw new Exception($"Shared config {configName} already set");
      field = value;
    }
  }

  public const Emotes DisabledEmote = (Emotes)(-1);
  public const Emotes AnyEmote = (Emotes)(-2);

  protected static IReadOnlyDictionary<string, PieceTable> PieceTablesByPieceName => ServersideQoLPlugin.Instance.PieceTablesByPieceName;

  private protected interface IYamlConfigEntry
  {
    object Value { get; set; }
  }

  public sealed class YamlConfigEntry<T>(T value) : IYamlConfigEntry
    where T : notnull
  {
    public T Value { get; private set { IsDefault = value.Equals(field); field = value; } } = value;
    public bool IsDefault { get; private set; } = true;

    object IYamlConfigEntry.Value
    {
      get => Value;
      set => Value = (T)value;
    }
  }

  private protected sealed class MyTypeInspector(ITypeInspector inner) : TypeInspectorSkeleton
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

  public sealed class AcceptableEnum<T> : AcceptableValueBase
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
    : base(typeof(T))
    {
      if (SQoLEnumUtils.IsBitSet<T>())
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

      if (SQoLEnumUtils.IsBitSet<T>())
      {
        var val = e.ToUInt64();
        ulong result = 0;
        foreach (var flag in AcceptableValues.Select(static x => x.ToUInt64()).Where(x => (val & x) == x))
          result |= flag;
        return SQoLEnumUtils.ToEnum<T>(result);
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
      // breaks gale's config editor
      //if (EnumUtils.IsBitSet<T>())
      //  return Invariant($"# Acceptable values: {_default} or combination of {string.Join(", ", AcceptableValues.Where(x => !x.Equals(_default)))}");

      if (!SQoLEnumUtils.IsBitSet<T>())
        return $"# Acceptable values: {string.Join(", ", AcceptableValues)}";
      return Invariant($"""
        # Acceptable values: {string.Join(", ", AcceptableValues)}
        # Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)
        """);
    }
  }

  protected sealed class AcceptableFormatString(object[] testArgs) : AcceptableValueBase(typeof(string))
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
    => Invariant($"# Acceptable value formats: .NET Format strings for {testArgs.Length} arguments ({string.Join(", ", testArgs.Select(static x => x.GetType().Name))}): https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-string-format#get-started-with-the-stringformat-method");
  }
}

public abstract class ConfigBase<TSelf>(ConfigFile configFile, Logger logger) : ConfigBase, IConfig
  where TSelf : ConfigBase<TSelf>
{
  static event Action<ConfigFile, TSelf>? Initialized;
  public sealed record Deprecated(string Reason, Action<TSelf> AdjustConfig);
  static readonly HashSet<ConfigEntryBase> __deprecatedEntries = [];

  static Dictionary<string, IYamlConfigEntry>? __yaml = [];

  IServersideQoLPlugin IConfig.Plugin { get => field; set => field = value; } = default!;

  public static TSelf Instance { get => field ?? throw new InvalidOperationException("Config has not been initialized yet"); private set; }

  protected static IReadOnlyDictionary<Heightmap.Biome, Character> BossesByBiome => Processor.BossesByBiome;
  public ConfigFile ConfigFile { get; } = configFile;
  protected Logger Logger { get; } = logger;
  public abstract ConfigEntry<bool> Enabled { get; }

  EventHandler<SettingChangedEventArgs>? _configChanged;

  public event EventHandler<SettingChangedEventArgs>? ConfigChanged
  {
    add
    {
      if (_configChanged is null)
        ConfigFile.SettingChanged += OnSettingsChanged;
      _configChanged += value;
    }
    remove
    {
      _configChanged -= value;
      if (_configChanged is null)
        ConfigFile.SettingChanged -= OnSettingsChanged;
    }
  }

  void OnSettingsChanged(object? sender, SettingChangedEventArgs args)
      => _configChanged?.Invoke(this, args);

  void IConfig.RaiseInitialized()
  {
    Instance = (TSelf)this;

    foreach (var (configPath, entry) in __yaml!)
      BindYaml(configPath, entry);
    __yaml = null;

    Initialized?.Invoke(ConfigFile, (TSelf)this);
  }

  public static bool IsDeprecated(ConfigEntryBase entry)
      => __deprecatedEntries.Contains(entry);

  protected static ConfigEntry<T> BindEx<T>(ConfigFile config, string section, T defaultValue, string description,
      AcceptableValueBase? acceptableValues = null,
      Deprecated? deprecated = null,
      [CallerMemberName] string key = default!)
  {
    if (deprecated is not null)
      description = string.Join(Environment.NewLine, [$"DEPRECATED: {deprecated.Reason}", description]);
    var cfg = config.Bind(section, key, defaultValue, new ConfigDescription(description, acceptableValues));
    if (deprecated is not null)
    {
      __deprecatedEntries.Add(cfg);
      Initialized += OnInitialized;
    }
    return cfg;

    void OnInitialized(ConfigFile cfgFile, TSelf modConfig)
    {
      if (!ReferenceEquals(cfgFile, cfg.ConfigFile))
        return;
      Initialized -= OnInitialized;
      cfg.SettingChanged += (_, _) => OnSettingChanged(deprecated, cfg, modConfig);
      OnSettingChanged(deprecated, cfg, modConfig);
    }

    static void OnSettingChanged(Deprecated deprecated, ConfigEntry<T> cfg, TSelf modCfg)
    {
      if (EqualityComparer<T>.Default.Equals(cfg.Value, (T)cfg.DefaultValue))
        return;
      deprecated.AdjustConfig(modCfg);
      modCfg.Logger.LogWarning($"[{cfg.Definition.Section}].[{cfg.Definition.Key}] is deprecated: {deprecated.Reason}");
    }
  }

  protected static YamlConfigEntry<T> BindYaml<T>(ConfigFile cfg, [CallerMemberName] string fileName = default!)
      where T : notnull, new()
  {
    if (__yaml is null)
      throw new InvalidOperationException("Config alredy initialized");

    var configDir = Path.Combine(Path.GetDirectoryName(cfg.ConfigFilePath), ServersideQoLPlugin.PluginGuid);
    var configPath = Path.Combine(configDir, $"{Path.GetFileNameWithoutExtension(cfg.ConfigFilePath)}.{fileName}.yml");

    var entry = new YamlConfigEntry<T>(new());
    __yaml.Add(configPath, entry);
    return entry;
  }

  static void BindYaml(string configPath, IYamlConfigEntry entry)
  {
    var configDir = Path.GetDirectoryName(configPath);

    var serializer = new SerializerBuilder()
        .IncludeNonPublicProperties()
        .WithTypeInspector(static x => new MyTypeInspector(x))
        .Build();

    {
      Directory.CreateDirectory(configDir);
      var defaultConfigPath = Path.ChangeExtension(configPath, "default.yml");
      using var file = new StreamWriter(defaultConfigPath, append: false);
      file.WriteLine($"""
        # {Path.GetFileName(defaultConfigPath)} contains the default values and is overwritten regularly.
        # Rename it to {Path.GetFileName(configPath)} if you want to change values.
        """);
      file.WriteLine();
      WriteYamlHeader(file);
      serializer.Serialize(file, entry.Value);
    }

    if (!File.Exists(configPath))
      return;

    try
    {
      var deserializer = new DeserializerBuilder()
          .IncludeNonPublicProperties()
          .EnablePrivateConstructors()
          //.WithObjectFactory(new MyObjectFactory())
          .WithTypeInspector(static x => new MyTypeInspector(x))
          .Build();

      using (var stream = new StreamReader(configPath))
        entry.Value = deserializer.Deserialize(stream, entry.Value.GetType()) ?? entry.Value;

      ServersideQoLPlugin.Logger.LogInfo($"Advanced config loaded from {Path.GetFileName(configPath)}");
    }
    catch (Exception ex)
    {
      ServersideQoLPlugin.Logger.LogWarning($"{Path.GetFileName(configPath)}: {ex}");
    }
  }

  static void WriteYamlHeader(StreamWriter writer) => writer.WriteLine("""
    # IMPORTANT:
    #   This file is for advanced tweaks.
    #   You are expected to be familiar with YAML and its pitfalls if you decide to edit it.
    #   Check the log for warnings related to this file.

    """);
}
