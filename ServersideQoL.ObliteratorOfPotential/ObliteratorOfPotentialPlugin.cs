using BepInEx.Configuration;

namespace ServersideQoL.ObliteratorOfPotential;

partial class ObliteratorOfPotentialPlugin : ServersideQoLPluginBase<ObliteratorOfPotentialPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<IncineratorProcessor>();
}
