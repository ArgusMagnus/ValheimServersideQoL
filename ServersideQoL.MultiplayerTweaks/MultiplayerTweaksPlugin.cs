using BepInEx.Configuration;

namespace ServersideQoL.MultiplayerTweaks;

partial class MultiplayerTweaksPlugin : ServersideQoLPluginBase<MultiplayerTweaksPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<Processor>();
}
