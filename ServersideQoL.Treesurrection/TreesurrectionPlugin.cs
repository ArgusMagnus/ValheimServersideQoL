using BepInEx.Configuration;

namespace ServersideQoL.Treesurrection;

partial class TreesurrectionPlugin : ServersideQoLPluginBase<TreesurrectionPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<StumpProcessor>();
}
