using BepInEx.Configuration;

namespace ServersideQoL.Player;

partial class PlayerPlugin : ServersideQoLPluginBase<PlayerPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<PlayerProcessor>()
    .Add<CryptDoorProcessor>()
    .Add<VagonProcessor>();
}
