using BepInEx.Configuration;

namespace ServersideQoL.CreatureLevelUp;

partial class CreatureLevelUpPlugin : ServersideQoLPluginBase<CreatureLevelUpPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<CreatureLevelUpProcessor>()
    .Add<CreatureProcessor>();
}
