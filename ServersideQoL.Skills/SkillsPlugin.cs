using BepInEx.Configuration;

namespace ServersideQoL.Skills;

partial class SkillsPlugin : ServersideQoLPluginBase<SkillsPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<MineRockProcessor>();
}
