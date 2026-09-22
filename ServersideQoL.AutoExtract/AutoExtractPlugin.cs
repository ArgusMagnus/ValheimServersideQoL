using BepInEx.Configuration;

namespace ServersideQoL.AutoExtract;

partial class AutoExtractPlugin : ServersideQoLPluginBase<AutoExtractPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger)
    => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<ExtractProcessor>();
}
