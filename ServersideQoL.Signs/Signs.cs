using BepInEx;
using BepInEx.Configuration;

namespace ServersideQoL.Signs;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(ServersideQoL.PluginGuid, ServersideQoL.PluginVersion)]
partial class Signs : ServersideQoLPluginBase<Signs, Config>
{
    protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

    protected override void RegisterProcessors(IProcessorCollection processors)
        => processors.Add<SignProcessor>();
}
