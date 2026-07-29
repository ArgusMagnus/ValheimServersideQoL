using BepInEx.Configuration;

namespace ServersideQoL.AutoMapTables;

partial class AutoMapTablesPlugin : ServersideQoLPluginBase<AutoMapTablesPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<MapTableProcessor>()
    .Add<ShipProcessor>();
}
