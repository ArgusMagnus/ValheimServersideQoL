using HarmonyLib;

namespace ServersideQoL.MultiplayerTweaks;

[Processor("71988cd7-0b04-4603-8759-21ab8585ac05")]
[DependsOn<PlayerRegistryProcessor>]
public sealed class Processor : Processor<Processor.PrefabInfo>
{
  public sealed record PrefabInfo(Ship? Ship, Smelter? Smelter, CookingStation? CookingStation, Character? Character) : ProcessorPrefabInfo;

  bool _patched;
  Timestamp _maxOwnerTimestamp;

  protected override void Initialize()
  {
    if (!_patched && Config.Instance.ForcePlayerMapPin.Value)
    {
      _patched = true;
      MultiplayerTweaksPlugin.HarmonyInstance.PatchAll(typeof(ZNetServerSyncedPlayerDataPatch));
    }
  }

  protected override void PreProcess(PeersEnumerable peers)
  {
    _maxOwnerTimestamp = Timestamp.Now.AddSeconds(2);
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!zdo.ZDO.Persistent)
      return ProcessResult.UnregisterProcessor;

    if (prefabInfo.Ship is not null && Config.Instance.AssignShipsToCaptain.Value)
    {
      var userPlayerID = zdo.Vars.GetUser();
      if (Instance<PlayerRegistryProcessor>().GetStateForPlayerID(userPlayerID) is { } playerState)
        zdo.ZDO.SetOwner(playerState.Owner);
      return default;
    }

    if (ShouldAssignToClosestPlayer(zdo, prefabInfo))
    {
      var peerCount = peers.Count;
      if (peerCount > 1 && zdo.OwnerTimestamp < _maxOwnerTimestamp)
      {
        Peer? closest = null;
        var minDistSqr = float.MaxValue;
        foreach (var peer in peers.Enumerate())
        {
          if (peer.PlayerState is not { } playerState)
            continue;
          var distSqr = Utils.DistanceSqr(zdo.ZDO.GetPosition(), playerState.ZDO.ZDO.GetPosition());
          if (distSqr < minDistSqr)
          {
            minDistSqr = distSqr;
            closest = peer;
          }
        }

        if (closest?.ZNetPeer.m_rpc.GetTimeSinceLastPing() < Config.Instance.Advanced.Value.MaxTimeSinceLastPingSeconds)
          zdo.ZDO.SetOwner(closest.ZNetPeer.m_uid);
      }

      return ProcessResult.ScheduleReprocessing;
    }

    return ProcessResult.UnregisterProcessor;
  }

  bool ShouldAssignToClosestPlayer(ServersideQoLZDO zdo, PrefabInfo prefabInfo) =>
      (Config.Instance.AssignInteractablesToClosestPlayer.Value && prefabInfo is not { Smelter: null, CookingStation: null }) ||
      (Config.Instance.AssignMobsToClosestPlayer.Value && prefabInfo.Character is not null && !zdo.Vars.GetTamed());

  [HarmonyPatch(typeof(ZNet), "RPC_ServerSyncedPlayerData")]
  static class ZNetServerSyncedPlayerDataPatch
  {
    [HarmonyPostfix]
    public static void Postfix(ZNet __instance)
    {
      if (!Config.Instance.ForcePlayerMapPin.Value)
        return;
      foreach (var peer in __instance.GetPeers())
        peer.m_publicRefPos = true;
    }
  }
}
