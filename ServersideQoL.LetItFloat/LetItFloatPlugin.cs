using BepInEx.Configuration;

namespace ServersideQoL.LetItFloat;

partial class LetItFloatPlugin : ServersideQoLPluginBase<LetItFloatPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<ItemDropProcessor>();
}
