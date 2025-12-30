using BepInEx;
using Valheim.ZDOExtender;

namespace Valheim.ServersideQoL.Signs;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(ServersideQoL.PluginGuid, ServersideQoL.PluginVersion)]
public sealed partial class ServersideQoLSigns : BaseUnityPlugin
{
    public const string PluginName = nameof(ServersideQoL);
    public const string PluginGuid = $"argusmagnus.{PluginName}";

    internal static new readonly Logger Logger = new(PluginName);
    static new Config Config => Config.Instance;

    void Awake()
    {
        ServersideQoL.AddProcessor<SignProcessor>();
        ServersideQoL.AddConfig(() => new Config(base.Config, Logger));
    }
}
