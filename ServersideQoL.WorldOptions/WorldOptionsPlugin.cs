using BepInEx.Configuration;

namespace ServersideQoL.WorldOptions;

partial class WorldOptionsPlugin : ServersideQoLPluginBase<WorldOptionsPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<MisterProcessor>();
}
