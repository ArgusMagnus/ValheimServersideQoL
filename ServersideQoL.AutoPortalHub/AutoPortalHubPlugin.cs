using BepInEx.Configuration;

namespace ServersideQoL.AutoPortalHub;

partial class AutoPortalHubPlugin : ServersideQoLPluginBase<AutoPortalHubPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger)
    => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<PortalHubProcessor>();
}
