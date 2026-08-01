using BepInEx.Configuration;

namespace ServersideQoL.PrefabConfigurator;

partial class PrefabConfiguratorPlugin : ServersideQoLPluginBase<PrefabConfiguratorPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<PrefabProcessor>();
}
