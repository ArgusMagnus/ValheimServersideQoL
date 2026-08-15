using HarmonyLib;
using ServersideQoL.Processors;

namespace ServersideQoL.JustSleep;

[Processor("500d524e-2dbc-493d-8231-c8e9f03158bf")]
[DependsOn<PlayerRegistryProcessor>]
public sealed class PlayerProcessor : Processor<ProcessorPrefabInfo<Player>>
{
  bool _patched;
  protected override void Initialize()
  {
    if (_patched)
      return;
    _patched = true;
    JustSleepPlugin.HarmonyInstance.PatchAll(typeof(EverybodyIsTryingToSleepPatch));
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<Player> prefabInfo)
  {
    // This processor is only used to ensure that the PlayerRegistryProcessor is registered and running.
    return ProcessResult.UnregisterProcessor;
  }
  
  static bool EverybodyIsTryingToSleep()
  {
    if (Instance<PlayerRegistryProcessor>().PlayerStates is not { Count: > 0 } states)
      return false;

    var inBed = 0;
    var sitting = 0;
    foreach (var state in states)
    {
      if (state.ZDO.Vars.GetInBed())
        inBed++;
      else if (state.ZDO.Vars.GetEmote() is Emotes.Sit)
        sitting++;
    }

    if (inBed == states.Count)
      return true;
    if (inBed < Config.Instance.MinPlayersInBed.Value)
      return false;

    var total = inBed + sitting;
    if (total * 100 / states.Count >= Config.Instance.RequiredPlayerPercentage.Value)
      return true;

    RPC.ShowMessage(ZRoutedRpc.Everybody, Config.Instance.SleepPromptMessageType.Value,
        Config.Instance.Localization.Value.FormatPrompt(total, states.Count));

    return false;
  }

  [HarmonyPatch(typeof(Game), "EverybodyIsTryingToSleep")]
  static class EverybodyIsTryingToSleepPatch
  {
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
      __result = EverybodyIsTryingToSleep();
      return false;
    }
  }
}
