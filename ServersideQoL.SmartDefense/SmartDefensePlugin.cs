using BepInEx.Configuration;

namespace ServersideQoL.SmartDefense;

partial class SmartDefensePlugin : ServersideQoLPluginBase<SmartDefensePlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<TurretProcessor>();
}
