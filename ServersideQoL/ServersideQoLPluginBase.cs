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

public abstract class ServersideQoLPluginBase<TSelf, TConfig> : BaseUnityPlugin, IServersideQoLPlugin
    where TSelf : ServersideQoLPluginBase<TSelf, TConfig>
    where TConfig : ConfigBase<TConfig>
{
  protected abstract TConfig CreateConfigSingleton(ConfigFile configFile, Logger logger);

  static TConfig? _config;
  public static new TConfig Config => _config ?? throw new InvalidOperationException("Config has not been initialized yet");
  IConfig IServersideQoLPlugin.Config
  {
    get
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
  }

  public static new Logger Logger { get; private set; } = default!;

  readonly HashSet<Processor> _processors = [];
  IReadOnlyCollection<Processor> IServersideQoLPlugin.Processors => _processors;

  protected ServersideQoLPluginBase()
  {
    var pluginName = GetType().GetCustomAttribute<BepInPlugin>().Name;
    Logger = new(pluginName);
    ServersideQoLPlugin.RegisterPlugin(this);
  }

  protected abstract void RegisterProcessors(IProcessorCollection processors);

  void IServersideQoLPlugin.RegisterProcessors()
      => RegisterProcessors(new ProcessorCollection(this, Logger));

  //protected void RegisterProcessor<T>()
  //    where T : Processor, new()

  //{
  //    ServersideQoL.RegisterPlugin(this);
  //    var processor = Processor.Instance<T>();
  //    if (_processors.Contains(processor))
  //        return;

  //    processor.Plugin = this;
  //    processor.ValidateProcessorInternal();
  //    _processors.Add(processor);
  //}

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
