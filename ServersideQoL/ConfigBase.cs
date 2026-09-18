using BepInEx.Configuration;
using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace ServersideQoL;

interface IConfig
{
  void RaiseInitialized(IServersideQoLPlugin plugin);
  event EventHandler<SettingChangedEventArgs>? ConfigChanged;
  IServersideQoLPlugin Plugin { get; }
  ConfigEntry<bool> Enabled { get; }
  ConfigFile ConfigFile { get; }
}

public abstract class ConfigBase
{
  private protected ConfigBase(ConfigFile configFile, Logger logger)
  {
    ConfigFile = configFile;
    Logger = logger;
  }

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
  protected static IReadOnlyDictionary<Heightmap.Biome, Character> BossesByBiome => Processor.BossesByBiome;
  public ConfigFile ConfigFile { get; }
  protected Logger Logger { get; }
  public abstract ConfigEntry<bool> Enabled { get; }

  static readonly bool __configWatcherInstalled = AppDomain.CurrentDomain.GetAssemblies().Any(static x => !x.IsDynamic && Path.GetFileName(x.Location) is "ConfigWatcher.dll");
  static readonly Dictionary<ConfigFile, DebouncedFileWatcher> __fileWatcher = [];

  private protected void InitializeFileWatcher()
  {
    if (__configWatcherInstalled || !Config.Instance.AutoReload.Value || __fileWatcher.ContainsKey(ConfigFile))
      return;

    var fileWatcher = new DebouncedFileWatcher(ConfigFile.ConfigFilePath);
    fileWatcher.FileCreatedOrChanged += (_, _) =>
    {
      (var saveOnConfigSet, ConfigFile.SaveOnConfigSet) = (ConfigFile.SaveOnConfigSet, false);
      ConfigFile.Reload();
      ConfigFile.SaveOnConfigSet = saveOnConfigSet;
    };
    fileWatcher.Enabled = true;
    __fileWatcher.Add(ConfigFile, fileWatcher);
  }

  private protected interface IYamlConfigEntry
  {
    string FilePath { get; }
    object Value { get; }
    void Deserialize();
  }

  public sealed class YamlConfigEntry<T>(string filePath, T value) : IYamlConfigEntry
    where T : notnull
  {
    readonly string _filePath = filePath;
    public T Value { get; private set { IsDefault = value.Equals(field); field = value; } } = value;
    public bool IsDefault { get; private set; } = true;
    public event Action<YamlConfigEntry<T>>? ValueChanged;
    readonly DebouncedFileWatcher _fileWatcher = new(filePath);

    string IYamlConfigEntry.FilePath => _filePath;
    object IYamlConfigEntry.Value => Value;

    void Deserialize()
    {
      if (!File.Exists(_filePath))
        return;

      _fileWatcher.Enabled = false;
      try
      {
        var deserializer = new DeserializerBuilder()
            .IncludeNonPublicProperties()
            .EnablePrivateConstructors()
            //.WithObjectFactory(new MyObjectFactory())
            .WithTypeInspector(static x => new MyTypeInspector(x))
            .Build();

        using (var stream = new StreamReader(_filePath))
          Value = (T?)deserializer.Deserialize(stream, typeof(T)) ?? Value;

        ServersideQoLPlugin.Logger.LogInfo($"Advanced config loaded from {Path.GetFileName(_filePath)}");
        ValueChanged?.Invoke(this);
      }
      catch (Exception ex)
      {
        ServersideQoLPlugin.Logger.LogWarning($"{Path.GetFileName(_filePath)}: {ex}");
      }
      _fileWatcher.Enabled = true;
    }

    void IYamlConfigEntry.Deserialize()
    {
      Deserialize();
      _fileWatcher.FileCreatedOrChanged += OnFileCreatedOrChanged;
      _fileWatcher.Enabled = true;
    }

    async void OnFileCreatedOrChanged(object sender, FileSystemEventArgs e) => Deserialize();
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

    T ClampCore(object value)
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

    public override object Clamp(object value) => ClampCore(value);

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

  private protected interface ICustomConfigType
  {
    void EnsureConverterAdded();
  }

  public sealed class ConfigArray<T>(IReadOnlyList<T> value) : ICustomConfigType
  {
    public static ConfigArray<T> Empty => field ??= new([]);
    public IReadOnlyList<T> Items { get; } = value;

    public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is ConfigArray<T> other && Items.SequenceEqual(other.Items);
    public override int GetHashCode() => Items.GetHashCode();

    void ICustomConfigType.EnsureConverterAdded()
    {
      if (TomlTypeConverter.CanConvert(typeof(ConfigArray<T>)))
        return;
      if (!TomlTypeConverter.CanConvert(typeof(T)))
        throw new NotSupportedException();
      TomlTypeConverter.AddConverter(typeof(ConfigArray<T>), new()
      {
        ConvertToObject = static (str, type) => string.IsNullOrWhiteSpace(str) ? Empty : new([.. str.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(static x => TomlTypeConverter.ConvertToValue<T>(x))]),
        ConvertToString = static (obj, type) => string.Join(", ", ((ConfigArray<T>)obj).Items.Select(static x => TomlTypeConverter.ConvertToString(x, typeof(T))))
      });
    }
  }

  protected sealed class AcceptableArrayValues<T>(IReadOnlyList<T> values) : AcceptableValueBase(typeof(ConfigArray<T>))
  {
    readonly ConfigArray<T> _values = new(values);
    readonly HashSet<T> _allowedValues = [.. values];

    ConfigArray<T> ClampCore(object value)
    {
      if (value is not ConfigArray<T> arr)
        return ConfigArray<T>.Empty;

      foreach (var item in arr.Items)
      {
        if (!_allowedValues.Contains(item))
          return new([.. arr.Items.Where(_allowedValues.Contains)]);
      }
      return arr;
    }

    public override object Clamp(object value) => ClampCore(value);
    public override bool IsValid(object value) => Equals(value, Clamp(value));

    public override string ToDescriptionString() => Invariant($"""
      # Acceptable values: {TomlTypeConverter.ConvertToString(_values, _values.GetType())}
      # Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)
      """);
  }

  protected static class AcceptableArrayValues
  {
    public static AcceptableArrayValues<T> Get<T>(IReadOnlyList<T> values) => new(values);
  }
}

public abstract class ConfigBase<TSelf>(ConfigFile configFile, Logger logger) : ConfigBase(configFile, logger), IConfig
  where TSelf : ConfigBase<TSelf>
{
  static event Action<ConfigFile, TSelf>? Initialized;
  public sealed record Deprecated(string Reason, Action<TSelf> AdjustConfig);
  static readonly HashSet<ConfigEntryBase> __deprecatedEntries = [];

  static Dictionary<string, IYamlConfigEntry>? __yaml = [];
  //static readonly Dictionary<string, FileSystemWatcher> __yamlFileWatchers = new(StringComparer.OrdinalIgnoreCase);

  IServersideQoLPlugin _plugin = default!;
  IServersideQoLPlugin IConfig.Plugin => _plugin;

  internal static bool IsInitialized { get; private set; }
  public static TSelf Instance { get => field ?? throw new InvalidOperationException("Config has not been initialized yet"); private set; }
  protected static string Section => field ??= ((typeof(TSelf) == typeof(Config) || !Config.Instance.UnifiedConfig.Value) ? __section : $"M.{__section}");
  static readonly string __section = typeof(TSelf).Namespace.Split('.') is { Length: > 1 } parts ? parts[^1] : "General";

  EventHandler<SettingChangedEventArgs>? _configChanged;
  public event EventHandler<SettingChangedEventArgs>? ConfigChanged
  {
    add
    {
      if (_configChanged is null && value is not null)
      {
        ConfigFile.SettingChanged -= OnSettingsChanged;
        ConfigFile.SettingChanged += OnSettingsChanged;
      }
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
  {
    if (_configChanged is null)
      return;

    if (Config.Instance.UnifiedConfig.Value)
    {
      var expectedSection = Section;
      var section = args.ChangedSetting.Definition.Section;
      if (section != expectedSection && !section.StartsWith($"{expectedSection}."))
        return;
    }

    _configChanged(this, args);
  }

  [MemberNotNull(nameof(_plugin))]
  void IConfig.RaiseInitialized(IServersideQoLPlugin plugin)
  {
    _plugin = plugin;
    Instance = (TSelf)this;

    foreach (var entry in __yaml!.Values)
      BindYaml(entry);
    __yaml = null;

    IsInitialized = true;
    Initialized?.Invoke(ConfigFile, (TSelf)this);

    InitializeFileWatcher();
  }

  public static bool IsDeprecated(ConfigEntryBase entry)
    => __deprecatedEntries.Contains(entry);

  protected static ConfigEntry<T> BindEx<T>(ConfigFile config, T defaultValue, string description,
    AcceptableValueBase? acceptableValues = null,
    Deprecated? deprecated = null,
    [CallerMemberName] string key = default!)
    => BindExCore(config, Section, defaultValue, description, acceptableValues, deprecated, key);

  protected static ConfigEntry<T> BindEx<T>(ConfigFile config, string subSection, T defaultValue, string description,
    AcceptableValueBase? acceptableValues = null,
    Deprecated? deprecated = null,
    [CallerMemberName] string key = default!)
  {
    var section = Config.Instance.UnifiedConfig.Value ? $"{Section}.{subSection}" : subSection;
    return BindExCore(config, section, defaultValue, description, acceptableValues, deprecated, key);
  }

  static ConfigEntry<T> BindExCore<T>(ConfigFile config, string section, T defaultValue, string description,
    AcceptableValueBase? acceptableValues,
    Deprecated? deprecated, string key)
  {
    (defaultValue as ICustomConfigType)?.EnsureConverterAdded();

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
    => BindYaml<T>(cfg, null!, fileName);

  protected static YamlConfigEntry<T> BindYaml<T>(ConfigFile cfg, Action<T> onChanged, [CallerMemberName] string fileName = default!)
    where T : notnull, new()
  {
    if (__yaml is null)
      throw new InvalidOperationException("Config alredy initialized");

    if (typeof(TSelf) != typeof(Config) && Config.Instance.UnifiedConfig.Value)
      fileName = $"{__section}.{fileName}";
    var configDir = Path.Combine(Path.GetDirectoryName(cfg.ConfigFilePath), ServersideQoLPlugin.PluginGuid);
    var configPath = Path.Combine(configDir, $"{Path.GetFileNameWithoutExtension(cfg.ConfigFilePath)}.{fileName}.yml");

    var entry = new YamlConfigEntry<T>(configPath, new());
    if (onChanged is not null)
      entry.ValueChanged += x => onChanged(x.Value);
    __yaml.Add(configPath, entry);
    return entry;
  }

  static void BindYaml(IYamlConfigEntry entry)
  {
    var configDir = Path.GetDirectoryName(entry.FilePath);

    var serializer = new SerializerBuilder()
        .IncludeNonPublicProperties()
        .WithTypeInspector(static x => new MyTypeInspector(x))
        .DisableAliases()
        .Build();

    {
      Directory.CreateDirectory(configDir);
      var defaultConfigPath = Path.ChangeExtension(entry.FilePath, "default.yml");
      using var file = new StreamWriter(defaultConfigPath, append: false);
      file.WriteLine($"""
        # {Path.GetFileName(defaultConfigPath)} contains the default values and is overwritten regularly.
        # Rename it to {Path.GetFileName(entry.FilePath)} if you want to change values.
        """);
      file.WriteLine();
      WriteYamlHeader(file);
      serializer.Serialize(file, entry.Value);
    }

    entry.Deserialize();
  }

  static void WriteYamlHeader(StreamWriter writer) => writer.WriteLine("""
    # IMPORTANT:
    #   This file is for advanced tweaks.
    #   You are expected to be familiar with YAML and its pitfalls if you decide to edit it.
    #   Check the log for warnings related to this file.

    """);
}
