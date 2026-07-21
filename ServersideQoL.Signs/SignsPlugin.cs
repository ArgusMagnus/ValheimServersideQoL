using BepInEx;
using BepInEx.Configuration;

namespace ServersideQoL.Signs;

[BepInDependency(ServersideQoLPlugin.PluginGuid, ServersideQoLPlugin.PluginVersion)]
partial class SignsPlugin : ServersideQoLPluginBase<SignsPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors)
      => processors.Add<SignProcessor>();
}
