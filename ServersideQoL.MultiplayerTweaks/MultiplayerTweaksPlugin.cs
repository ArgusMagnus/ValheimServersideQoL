using BepInEx.Configuration;
using HarmonyLib;

namespace ServersideQoL.MultiplayerTweaks;

partial class MultiplayerTweaksPlugin : ServersideQoLPluginBase<MultiplayerTweaksPlugin, Config>
{
  internal static Harmony HarmonyInstance { get; } = new(PluginGuid);

  protected override Config CreateConfigSingleton(ConfigFile configFile, Logger logger) => new(configFile, logger);

  protected override void RegisterProcessors(IProcessorCollection processors) => processors
    .Add<Processor>();
}
