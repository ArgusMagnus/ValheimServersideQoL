using BepInEx;
using BepInEx.Configuration;
using System.Reflection;

namespace ServersideQoL;

interface IServersideQoLPlugin
{
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

  IConfig IServersideQoLPlugin.Config => GetConfig();
  IReadOnlyCollection<Processor> IServersideQoLPlugin.Processors => GetProcessors();
  void IServersideQoLPlugin.RegisterProcessors() => RegisterProcessors();
}

public abstract class ServersideQoLPluginBase<TSelf, TConfig> : ServersideQoLPluginBase
    where TSelf : ServersideQoLPluginBase<TSelf, TConfig>
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
      cfg = _config = CreateConfigSingleton(base.Config, Logger);
      cfg.Plugin = this;
      cfg.RaiseInitialized();
    }
    return cfg;
  }

  public static new Logger Logger { get; private set; } = default!;
  public static BepInPlugin BepInPlugin { get; } = typeof(TSelf).GetCustomAttribute<BepInPlugin>();

  readonly HashSet<Processor> _processors = [];
  private protected sealed override IReadOnlyCollection<Processor> GetProcessors() => _processors;

  protected ServersideQoLPluginBase()
  {
    Instance = (TSelf)this;
    Logger = new(BepInPlugin.Name);
    ServersideQoLPlugin.RegisterPlugin(this);
  }

  public static int RegisterServerVar(string name) => $"{BepInPlugin.GUID}.{name}".GetStableHashCode();

  protected abstract void RegisterProcessors(IProcessorCollection processors);

  private protected sealed override void RegisterProcessors()
      => RegisterProcessors(new ProcessorCollection(this, Logger));

  sealed class ProcessorCollection(ServersideQoLPluginBase<TSelf, TConfig> plugin, Logger logger) : IProcessorCollection
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
