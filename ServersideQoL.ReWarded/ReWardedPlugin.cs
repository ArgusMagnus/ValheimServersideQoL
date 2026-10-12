using BepInEx.Configuration;

namespace ServersideQoL.ReWarded;

partial class ReWardedPlugin : ServersideQoLPluginBase<ReWardedPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<Processor>();
}
