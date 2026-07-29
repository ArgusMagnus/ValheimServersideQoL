using BepInEx.Configuration;

namespace ServersideQoL.AutoMapTables;

partial class AutoMapTablesPlugin : ServersideQoLPluginBase<AutoMapTablesPlugin, Config>
{
  public static readonly int PluginGuidHash = PluginGuid.GetStableHashCode();

  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<MapTableProcessor>()
    .Add<ShipProcessor>();
}
