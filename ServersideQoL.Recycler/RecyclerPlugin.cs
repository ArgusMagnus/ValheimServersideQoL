using BepInEx.Configuration;

namespace ServersideQoL.Recycler;

partial class RecyclerPlugin : ServersideQoLPluginBase<RecyclerPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<IncineratorProcessor>();
}
