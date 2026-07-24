using BepInEx.Configuration;

namespace ServersideQoL.ContainerSigns;

partial class ContainerSignsPlugin : ServersideQoLPluginBase<ContainerSignsPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<ContainerProcessor>()
    .Add<SignProcessor>();
}
