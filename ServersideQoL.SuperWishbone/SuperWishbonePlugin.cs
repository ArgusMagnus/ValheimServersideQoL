using BepInEx.Configuration;

namespace ServersideQoL.SuperWishbone;

partial class SuperWishbonePlugin : ServersideQoLPluginBase<SuperWishbonePlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<LocationProxyProcessor>();
}
