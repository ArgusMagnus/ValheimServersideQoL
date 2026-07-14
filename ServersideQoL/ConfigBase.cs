using BepInEx.Configuration;
using System.Runtime.CompilerServices;

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
