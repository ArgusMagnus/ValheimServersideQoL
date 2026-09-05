using BepInEx.Configuration;
using System;

namespace ServersideQoL.Backpack;

partial class BackpackPlugin : ServersideQoLPluginBase<BackpackPlugin, Config>
{
  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<BackpackProcessor>();
}
