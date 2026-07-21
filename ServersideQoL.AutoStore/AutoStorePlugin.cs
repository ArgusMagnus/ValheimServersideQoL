using BepInEx;
using BepInEx.Configuration;

namespace ServersideQoL.AutoStore;

[BepInDependency(ServersideQoLPlugin.PluginGuid, ServersideQoLPlugin.PluginVersion)]
partial class AutoStorePlugin : ServersideQoLPluginBase<AutoStorePlugin, Config>
{
    public static class ContainerFloats
    {
        public const string PickupRange = "PickupRange";
    }

    protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

    protected override void RegisterProcessors(IProcessorCollection processors)
        => processors.Add<ContainerProcessor>();
}
