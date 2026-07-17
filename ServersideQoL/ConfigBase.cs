using BepInEx.Configuration;
using Mono.Cecil.Rocks;
using System.Runtime.CompilerServices;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.TypeInspectors;

namespace ServersideQoL;

interface IConfig
{
    void RaiseInitialized();
    event EventHandler<SettingChangedEventArgs>? ConfigChanged;
    IServersideQoLPlugin Plugin { get; set; }
    ConfigEntry<bool> Enabled { get; }
}

public abstract class ConfigBase
{
    private protected ConfigBase() { }

    static bool _initialized;
    static readonly Dictionary<string, Dictionary<string, IYamlConfigEntry>> __yaml = [];

    protected static YamlConfigEntry<T> BindYaml<T>(ConfigFile cfg, string fileName, string section)
        where T : notnull, new()
    {
        var configDir = Path.Combine(Path.GetDirectoryName(cfg.ConfigFilePath), Path.GetFileNameWithoutExtension(cfg.ConfigFilePath));
        var configPath = Path.Combine(configDir, fileName);

        if (!__yaml.TryGetValue(configPath, out var dict))
            __yaml.Add(configPath, dict = []);

        var entry = new YamlConfigEntry<T>(new());
        dict.Add(section, entry);
        return entry;
    }

    private protected static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        foreach (var (configPath, entries) in __yaml)
        {
            var dict = entries.ToDictionary(static x => x.Key, static x => x.Value.Value);
            var configDir = Path.GetDirectoryName(configPath);

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
                serializer.Serialize(file, dict);
            }

            if (!File.Exists(configPath))
                continue;

            try
            {
                var typeMap = dict.ToDictionary(static x => x.Key, static x => x.Value.GetType());

                var deserializer = new DeserializerBuilder()
                    .IncludeNonPublicProperties()
                    .EnablePrivateConstructors()
                    //.WithObjectFactory(new MyObjectFactory())
                    .WithTypeInspector(static x => new MyTypeInspector(x))
                    .WithNodeDeserializer(inner => new TypedDictionaryDeserializer(typeMap), static s => s.InsteadOf<ObjectNodeDeserializer>())
                    .Build();

                using (var stream = new StreamReader(configPath))
                    dict = deserializer.Deserialize<Dictionary<string, object>>(stream);

                foreach (var (key, value) in dict)
                    entries[key].Value = value;

                ServersideQoL.Logger.LogInfo($"Advanced config loaded from {Path.GetFileName(configPath)}");
            }
            catch (Exception ex)
            {
                ServersideQoL.Logger.LogWarning($"{Path.GetFileName(configPath)}: {ex}");
            }
        }
    }

    static void WriteYamlHeader(StreamWriter writer)
    {
        writer.WriteLine($"# IMPORTANT:");
        writer.WriteLine($"#   This file is for advanced tweaks. You are expected to be familiar with YAML and its pitfalls if you decide to edit it.");
        writer.WriteLine($"#   Check the log for warnings related to this file and DO NOT open issues asking for help on how to format this file.");
        writer.WriteLine();
    }

    interface IYamlConfigEntry
    {
        object Value { get; set; }
    }

    public sealed class YamlConfigEntry<T>(T value) : IYamlConfigEntry
        where T : notnull
    {
        public T Value { get; private set; } = value;

        object IYamlConfigEntry.Value
        {
            get => Value;
            set => Value = (T)value;
        }
    }

    sealed class TypedDictionaryDeserializer(Dictionary<string, Type> typeMap) : INodeDeserializer
    {
        readonly Dictionary<string, Type> _typeMap = typeMap;

        bool INodeDeserializer.Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            if (expectedType != typeof(Dictionary<string, object>))
            {
                value = null;
                return false;
            }

            reader.Consume<MappingStart>();

            var dict = new Dictionary<string, object?>();

            while (!reader.TryConsume<MappingEnd>(out _))
            {
                var key = (string?)nestedObjectDeserializer(reader, typeof(string)) ?? throw new Exception();
                var valType = _typeMap.TryGetValue(key, out var t) ? t : typeof(object);

                var val = nestedObjectDeserializer(reader, valType);
                dict[key] = val;
            }

            value = dict;
            return true;
        }
    }

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

    protected sealed class AcceptableEnum<T> : AcceptableValueBase
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
        => Invariant($"# Acceptable values: .NET Format strings for {testArgs.Length} arguments ({string.Join(", ", testArgs.Select(static x => x.GetType().Name))}): https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-string-format#get-started-with-the-stringformat-method");
    }
}

public abstract class ConfigBase<TSelf>(ConfigFile configFile, Logger logger) : ConfigBase, IConfig
    where TSelf : ConfigBase<TSelf>
{
    static event Action<ConfigFile, TSelf>? Initialized;
    public sealed record Deprecated(string Reason, Action<TSelf> AdjustConfig);
    static readonly HashSet<ConfigEntryBase> __deprecatedEntries = [];

    IServersideQoLPlugin IConfig.Plugin { get => field; set => field = value; } = default!;

    public static TSelf Instance { get => field ?? throw new InvalidOperationException("Config has not been initialized yet"); private set; }

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
        Initialize();
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
}
