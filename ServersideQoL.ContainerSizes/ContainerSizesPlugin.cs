using BepInEx.Configuration;

namespace ServersideQoL.ContainerSizes;

partial class ContainerSizesPlugin : ServersideQoLPluginBase<ContainerSizesPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<ContainerProcessor>();
}
