using BepInEx.Configuration;

namespace ServersideQoL.TeleportFollowers;

partial class TeleportFollowersPlugin : ServersideQoLPluginBase<TeleportFollowersPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<PlayerProcessor>();
}
