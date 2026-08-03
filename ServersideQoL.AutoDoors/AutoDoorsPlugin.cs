using BepInEx.Configuration;

namespace ServersideQoL.AutoDoors;

partial class AutoDoorsPlugin : ServersideQoLPluginBase<AutoDoorsPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<DoorProcessor>();
}
