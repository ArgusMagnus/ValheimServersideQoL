namespace ServersideQoL.MultiplayerTweaks;

[Processor("71988cd7-0b04-4603-8759-21ab8585ac05")]
[DependsOn<PlayerRegistryProcessor>]
public sealed class Processor : Processor<Processor.PrefabInfo>
{
  public sealed record PrefabInfo(Ship? Ship, Smelter? Smelter, CookingStation? CookingStation, Character? Character) : ProcessorPrefabInfo;

  Timestamp _maxOwnerTimestamp;

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
        PlayerState? closest = null;
        var minDistSqr = float.MaxValue;
        foreach (var peer in peers.Enumerate())
        {
          if (peer.PlayerState is not { } playerState)
            continue;
          var distSqr = Utils.DistanceSqr(zdo.ZDO.GetPosition(), playerState.ZDO.ZDO.GetPosition());
          if (distSqr < minDistSqr)
          {
            minDistSqr = distSqr;
            closest = playerState;
          }
        }

        if (closest is not null)
          zdo.ZDO.SetOwner(closest.Owner);
      }

      return ProcessResult.ScheduleReprocessing;
    }

    return ProcessResult.UnregisterProcessor;
  }

  bool ShouldAssignToClosestPlayer(ServersideQoLZDO zdo, PrefabInfo prefabInfo) =>
      (Config.Instance.AssignInteractablesToClosestPlayer.Value && prefabInfo is not { Smelter: null, CookingStation: null }) ||
      (Config.Instance.AssignMobsToClosestPlayer.Value && prefabInfo.Character is not null && !zdo.Vars.GetTamed());
}
