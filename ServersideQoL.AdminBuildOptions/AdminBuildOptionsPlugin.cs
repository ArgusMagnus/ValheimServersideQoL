using BepInEx.Configuration;

namespace ServersideQoL.AdminBuildOptions;

partial class AdminBuildOptionsPlugin : ServersideQoLPluginBase<AdminBuildOptionsPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<PlayerProcessor>()
    .Add<WearNTearProcessor>();
}
