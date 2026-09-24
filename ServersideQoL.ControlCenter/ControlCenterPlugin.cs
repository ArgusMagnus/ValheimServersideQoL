using BepInEx.Configuration;

namespace ServersideQoL.ControlCenter;

partial class ControlCenterPlugin : ServersideQoLPluginBase<ControlCenterPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<Processor>();
}
