using BepInEx.Configuration;

namespace ServersideQoL.AdminOptions;

partial class AdminOptionsPlugin : ServersideQoLPluginBase<AdminOptionsPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<PlayerProcessor>()
    .Add<WearNTearProcessor>();
}
