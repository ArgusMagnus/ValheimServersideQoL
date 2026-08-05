using BepInEx.Configuration;

namespace ServersideQoL.PortalProgression;

partial class PortalProgressionPlugin : ServersideQoLPluginBase<PortalProgressionPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<PortalProcessor>();
}
