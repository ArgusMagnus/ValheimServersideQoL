using BepInEx.Configuration;
using System.Runtime.CompilerServices;
using static System.Collections.Specialized.BitVector32;

namespace Valheim.ServersideQoL;

interface IConfig
{
    void RaiseInitialized();
    event EventHandler<SettingChangedEventArgs>? ConfigChanged;
    IServersideQoLPlugin Plugin { get; set; }
    ConfigEntry<bool> Enabled { get; }
}

public abstract class ConfigBase<TSelf>(ConfigFile configFile, Logger logger) : IConfig
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
