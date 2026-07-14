using BepInEx;
using BepInEx.Configuration;

namespace ServersideQoL.AutoStore;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(ServersideQoL.PluginGuid, ServersideQoL.PluginVersion)]
public sealed partial class ServersideQoLAutoStore : ServersideQoLPluginBase<ServersideQoLAutoStore, Config>
{
    public const string PluginName = $"{nameof(ServersideQoL)}.AutoStore";
    public const string PluginGuid = $"argusmagnus.{PluginName}";

    protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

    protected override void RegisterProcessors(IProcessorCollection processors)
        => processors.Add<ContainerProcessor>();
}
