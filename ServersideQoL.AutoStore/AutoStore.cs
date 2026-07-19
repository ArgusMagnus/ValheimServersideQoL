using BepInEx;
using BepInEx.Configuration;

namespace ServersideQoL.AutoStore;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(ServersideQoL.PluginGuid, ServersideQoL.PluginVersion)]
partial class AutoStore : ServersideQoLPluginBase<AutoStore, Config>
{
    protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

    protected override void RegisterProcessors(IProcessorCollection processors)
        => processors.Add<ContainerProcessor>();
}
