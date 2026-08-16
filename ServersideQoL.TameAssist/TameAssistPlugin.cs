using BepInEx.Configuration;

namespace ServersideQoL.TameAssist;

partial class TameAssistPlugin : ServersideQoLPluginBase<TameAssistPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<TameableProcessor>()
    .Add<PlayerProcessor>()
    .Add<GrowingProcessor>();
}
