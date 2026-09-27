using BepInEx.Configuration;
using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace ServersideQoL.AutoDoors;

[Processor("f91beb92-c76b-43d5-91d4-82c1f7de2929")]
public sealed class DoorProcessor : Processor<DoorProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Door Door) : ProcessorPrefabInfo
  {
    public ConfigEntry<bool> AutoClose { get; private set; } = default!;

    [MemberNotNullWhen(true, nameof(AutoClose))]
    public override bool IsValid => (AutoClose = (Config.Instance.AutoClose.TryGetValue(Door, out var cfg) ? cfg : null!)) is not null;
  }

  readonly Dictionary<ServersideQoLZDO, Timestamp> _closeAfter = [];

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    const int StateClosed = 0;

    if (!prefabInfo.AutoClose.Value)
      return ProcessResult.UnregisterProcessor;

    if (zdo.Vars.GetCreator().Value is 0)
      return ProcessResult.UnregisterProcessor;

    if (zdo.Vars.GetState() is StateClosed)
    {
      if (_closeAfter.Remove(zdo))
        zdo.Destroyed -= OnDoorDestroyed;
      return default;
    }

    if (!CheckMinDistance(peers, zdo, Config.Instance.AutoCloseMinPlayerDistance.Value))
      return ScheduleReprocessing();

    if (!_closeAfter.TryGetValue(zdo, out var closeAfter))
    {
      _closeAfter.Add(zdo, closeAfter = Timestamp.Now.AddSeconds(Config.Instance.AutoCloseMinOpenSeconds.Value));
      zdo.Destroyed += OnDoorDestroyed;
      return ScheduleReprocessing(Config.Instance.AutoCloseMinOpenSeconds.Value);
    }

    var delay = closeAfter.Seconds - Timestamp.Now.Seconds;
    if (delay > 0)
      return ScheduleReprocessing(delay);

    zdo.Vars.SetState(StateClosed);
    if (_closeAfter.Remove(zdo))
      zdo.Destroyed -= OnDoorDestroyed;

    return default;
  }

  void OnDoorDestroyed(ServersideQoLZDO zdo) => _closeAfter.Remove(zdo);
}
