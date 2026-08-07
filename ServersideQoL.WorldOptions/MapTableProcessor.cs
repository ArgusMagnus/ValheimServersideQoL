namespace ServersideQoL.WorldOptions;

public sealed class MapTableProcessor : Processor<ProcessorPrefabInfo<MapTable>>
{
  float _mapTableRangeSqr;

  protected override void Initialize()
  {
    _mapTableRangeSqr = Config.Instance.MapTableMapViewDistance.Value * Config.Instance.MapTableMapViewDistance.Value;
    if (_mapTableRangeSqr > 0 && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoMap))
    {
      _mapTableRangeSqr = 0;
      Logger.LogWarning($"[{Config.Instance.MapTableMapViewDistance.Definition.Section}].[{Config.Instance.MapTableMapViewDistance.Definition.Key}] has no effect unless the {GlobalKeys.NoMap} global key is set");
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<MapTable> prefabInfo)
  {
    foreach (var peer in peers.Enumerate())
    {
      if (peer.PlayerState is not { } playerState)
        continue;

      if (Utils.DistanceSqr(peer.RefPos, zdo.ZDO.GetPosition()) < _mapTableRangeSqr)
        playerState.AddGlobalKeyModification(new(GlobalKeys.NoMap), false);
      else
        playerState.RemoveGlobalKeyModification(new(GlobalKeys.NoMap));
    }

    return ProcessResult.ScheduleReprocessing;
  }
}
