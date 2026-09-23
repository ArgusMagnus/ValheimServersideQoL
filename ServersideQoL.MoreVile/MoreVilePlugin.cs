using BepInEx.Configuration;

namespace ServersideQoL.MoreVile;

partial class MoreVilePlugin : ServersideQoLPluginBase<MoreVilePlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<VileProcessor>();
}
