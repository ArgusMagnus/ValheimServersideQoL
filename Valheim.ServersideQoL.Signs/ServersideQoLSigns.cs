using BepInEx;
using BepInEx.Configuration;

namespace Valheim.ServersideQoL.Signs;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(ServersideQoL.PluginGuid, ServersideQoL.PluginVersion)]
public sealed partial class ServersideQoLSigns : ServersideQoLPluginBase<ServersideQoLSigns, Config>
{
    public const string PluginName = nameof(ServersideQoL);
    public const string PluginGuid = $"argusmagnus.{PluginName}";

    protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

    void Awake()
    {
        RegisterProcessor<SignProcessor>();
    }
}
