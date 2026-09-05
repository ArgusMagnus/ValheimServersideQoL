using BepInEx.Configuration;

namespace ServersideQoL.DropControl;

partial class DropControlPlugin : ServersideQoLPluginBase<DropControlPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<CharacterDropAndRagdollProcessor>();
}
