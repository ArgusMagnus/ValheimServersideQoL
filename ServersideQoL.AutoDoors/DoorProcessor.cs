namespace ServersideQoL.AutoDoors;

[Processor("f91beb92-c76b-43d5-91d4-82c1f7de2929")]
public sealed class DoorProcessor : Processor<DoorProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Door Door) : ProcessorPrefabInfo
  {
    public override bool IsValid => Door is { m_keyItem: null, m_canNotBeClosed: false };
  }

  readonly Dictionary<ServersideQoLZDO, Timestamp> _closeAfter = [];

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    const int StateClosed = 0;

    if (zdo.Vars.GetCreator().Value is 0)
      return ProcessResult.UnregisterProcessor;

    if (zdo.Vars.GetState() is StateClosed)
    {
      if (_closeAfter.Remove(zdo))
        zdo.Destroyed -= OnDoorDestroyed;
      return default;
    }

    if (!CheckMinDistance(peers, zdo, Config.Instance.AutoCloseMinPlayerDistance.Value))
      return ProcessResult.ScheduleReprocessing;

    if (!_closeAfter.TryGetValue(zdo, out var closeAfter))
    {
      _closeAfter.Add(zdo, closeAfter = Timestamp.Now.AddSeconds(Config.Instance.AutoCloseMinOpenSeconds.Value));
      zdo.Destroyed += OnDoorDestroyed;
      zdo.DelaySchedulingFor(Config.Instance.AutoCloseMinOpenSeconds.Value);
      return ProcessResult.ScheduleReprocessing;
    }

    var delay = closeAfter.Seconds - Timestamp.Now.Seconds;
    if (delay > 0)
    {
      zdo.DelaySchedulingFor(delay);
      return ProcessResult.ScheduleReprocessing;
    }

    zdo.Vars.SetState(StateClosed);
    if (_closeAfter.Remove(zdo))
      zdo.Destroyed -= OnDoorDestroyed;

    return default;
  }

  void OnDoorDestroyed(ServersideQoLZDO zdo) => _closeAfter.Remove(zdo);
}
