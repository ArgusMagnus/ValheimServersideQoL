using BepInEx.Configuration;

namespace ServersideQoL.AutoProcess;

partial class AutoProcessPlugin : ServersideQoLPluginBase<AutoProcessPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger)
    => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<SmelterProcessor>();
}
