using BepInEx.Configuration;

namespace ServersideQoL.PhoenixPersons;

partial class PhoenixPersonsPlugin : ServersideQoLPluginBase<PhoenixPersonsPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<TombStoneProcessor>();
}
