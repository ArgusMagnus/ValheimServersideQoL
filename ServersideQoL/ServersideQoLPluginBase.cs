using BepInEx;
using BepInEx.Configuration;
using ServersideQoL.Utilities;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ServersideQoL;

interface IServersideQoLPlugin
{
  BepInPlugin BepInPlugin { get; }
  IConfig Config { get; }
  IReadOnlyCollection<Processor> Processors { get; }
  void RegisterProcessors();
}

public interface IProcessorCollection
{
  IProcessorCollection Add<T>() where T : Processor, new();
}

public abstract class ServersideQoLPluginBase : BaseUnityPlugin, IServersideQoLPlugin
{
  private protected ServersideQoLPluginBase() { }
  private protected abstract IConfig GetConfig();
  private protected abstract IReadOnlyCollection<Processor> GetProcessors();
  private protected abstract void RegisterProcessors();
  private protected abstract BepInPlugin GetBepInPlugin();

  IConfig IServersideQoLPlugin.Config => GetConfig();
  IReadOnlyCollection<Processor> IServersideQoLPlugin.Processors => GetProcessors();
  void IServersideQoLPlugin.RegisterProcessors() => RegisterProcessors();
  BepInPlugin IServersideQoLPlugin.BepInPlugin => GetBepInPlugin();
}

[BepInDependency(ServersideQoLPlugin.PluginGuid, ServersideQoLPlugin.PluginVersion)]
public abstract class ServersideQoLPluginBase<TSelf, TConfig> : ServersideQoLPluginBaseCore<TSelf, TConfig>
    where TSelf : ServersideQoLPluginBase<TSelf, TConfig>
    where TConfig : ConfigBase<TConfig>;

public abstract class ServersideQoLPluginBaseCore<TSelf, TConfig> : ServersideQoLPluginBase
    where TSelf : ServersideQoLPluginBaseCore<TSelf, TConfig>
    where TConfig : ConfigBase<TConfig>
{
  public static TSelf Instance { get; private set; } = default!;
  protected abstract TConfig CreateConfigSingleton(ConfigFile configFile, Logger logger);

  TConfig? _config;
  public new TConfig Config => _config ?? throw new InvalidOperationException("Config has not been initialized yet");
  private protected sealed override IConfig GetConfig()
  {
    IConfig? cfg = _config;
    if (cfg is null)
    {
#if DEBUG
      ConfigBase.GeneratingConfigMarkdown = true;
      GenerateDefaultConfigMarkdown(GetMarkdownConfigPath() is { } path ?
        CreateConfigSingleton(new ConfigFile(path, false) { SaveOnConfigSet = false }, Logger).ConfigFile : null);
      ConfigBase.GeneratingConfigMarkdown = false;
#endif

      cfg = _config = CreateConfigSingleton(GetConfigFile(base.Config), Logger);
      if (_config is Config { ConfigPerWorld.Value: true })
      {
        Logger.LogInfo("Using world config file");
        cfg = _config = CreateConfigSingleton(GetPerWorldConfigFile(base.Config), Logger);
      }
      cfg.RaiseInitialized(this);
    }
    return cfg;

    static ConfigFile GetConfigFile(ConfigFile configFile)
    {
      if (typeof(TConfig) != typeof(Config))
      {
        if (ServersideQoL.Config.Instance.UnifiedConfig.Value)
          configFile = ServersideQoL.Config.Instance.ConfigFile;
        else if (ServersideQoL.Config.Instance.ConfigPerWorld.Value)
          configFile = GetPerWorldConfigFile(configFile);
      }
      return configFile;
    }

    static ConfigFile GetPerWorldConfigFile(ConfigFile configFile)
    {
      var path = ZNet.World.GetSaveDirectory(FileHelpers.FileSource.Local);
      path = Path.Combine(path, Path.GetFileName(configFile.ConfigFilePath));
      if (!File.Exists(path) && File.Exists(configFile.ConfigFilePath))
        File.Copy(configFile.ConfigFilePath, path);

      var srcDir = Path.Combine(Path.GetDirectoryName(configFile.ConfigFilePath), Path.GetFileNameWithoutExtension(configFile.ConfigFilePath));
      if (Directory.Exists(srcDir))
      {
        var dstDir = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
        Directory.CreateDirectory(dstDir);
        foreach (var file in Directory.EnumerateFiles(srcDir))
        {
          var dstFile = Path.Combine(dstDir, Path.GetFileName(file));
          if (!File.Exists(dstFile))
            File.Copy(file, dstFile);
        }
      }

      return new(path, saveOnInit: false, BepInPlugin);
    }

#if DEBUG
    static string? GetMarkdownConfigPath()
    {
      if (typeof(TSelf).GetField("ProjectDirectory", BindingFlags.Static | BindingFlags.NonPublic)?.GetRawConstantValue() is string dir)
        return Path.Combine(dir, "CONFIG.md");
      return null;
    }

    static void GenerateDefaultConfigMarkdown(ConfigFile? cfg)
    {
      if (cfg is null)
        return;

      using var writer = new StreamWriter(cfg.ConfigFilePath, false, new UTF8Encoding(false));

      var prevSection = "";

      foreach (var (def, entry) in cfg.OrderBy(static x => x.Key.Section))
      {
        if (ConfigBase.IsDeprecated(entry))
          continue;

        if (def.Section != prevSection)
        {
          if (!string.IsNullOrEmpty(prevSection))
            writer.WriteLine("</details>");
          writer.WriteLine($"<details open><summary><b>{def.Section}</b></summary>");
          writer.WriteLine();
          writer.WriteLine("|Option|Default Value|Acceptable Values|Description|");
          writer.WriteLine("|------|-------------|-----------------|-----------|");
          prevSection = def.Section;
        }

        var accetableValues = entry.Description.AcceptableValues?.ToDescriptionString();
        if (accetableValues is not null)
          accetableValues = Regex.Replace(accetableValues, @"^#.+?\:\s*", "");
        else if (entry.SettingType == typeof(bool))
          accetableValues = Invariant($"{bool.TrueString}/{bool.FalseString}");
        else if (entry.SettingType.IsEnum)
        {
          if (entry.SettingType.GetCustomAttribute<FlagsAttribute>() is null)
            accetableValues = Invariant($"One of {string.Join(", ", Enum.GetNames(entry.SettingType))}");
          else
            accetableValues = Invariant($"Combination of {string.Join(", ", Enum.GetNames(entry.SettingType))}");
        }

        writer.WriteLine(Invariant($"|{def.Key}|{entry.DefaultValue}|{accetableValues}|{entry.Description.Description
          .Replace("<", "&lt;").Replace(">", "&gt;").Replace(Environment.NewLine, " <br>")}|"));
      }
    }
#endif
  }

  public static new Logger Logger { get; private set; } = default!;
  public static BepInPlugin BepInPlugin { get; } = typeof(TSelf).GetCustomAttribute<BepInPlugin>();
  private protected sealed override BepInPlugin GetBepInPlugin() => BepInPlugin;

  readonly HashSet<Processor> _processors = [];
  private protected sealed override IReadOnlyCollection<Processor> GetProcessors() => _processors;

  private protected ServersideQoLPluginBaseCore()
  {
    Instance = (TSelf)this;
    Logger = new(BepInPlugin.Name);
    ServersideQoLPlugin.RegisterPlugin(this);
  }

  /// <typeparam name="T">
  /// Supports all default ZDO variable types plus all unmanaged types and collections of unmanaged types.
  /// Any other type will cause an exception.
  /// </typeparam>
  public static ServerVar<T> RegisterServerVar<T>(string name) => ServerVar.Create<T>($"{BepInPlugin.GUID}.{name}");

  protected abstract void RegisterProcessors(IProcessorCollection processors);

  private protected sealed override void RegisterProcessors()
      => RegisterProcessors(new ProcessorCollection(this, Logger));

  sealed class ProcessorCollection(ServersideQoLPluginBaseCore<TSelf, TConfig> plugin, Logger logger) : IProcessorCollection
  {
    public IProcessorCollection Add<T>() where T : Processor, new()
    {
      var processor = Processor.Instance<T>();
      if (plugin._processors.Contains(processor))
        return this;

      processor.Init(plugin, logger);
      plugin._processors.Add(processor);
      return this;
    }
  }
}
