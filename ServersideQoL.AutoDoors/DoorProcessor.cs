using BepInEx.Configuration;
using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace ServersideQoL.AutoDoors;

[Processor(Id)]
public sealed class DoorProcessor : Processor<DoorProcessor.PrefabInfo>
{
  public const string Id = "f91beb92-c76b-43d5-91d4-82c1f7de2929";
  public sealed record PrefabInfo(Door Door) : ProcessorPrefabInfo
  {
    public ConfigEntry<bool> AutoClose { get; private set; } = default!;

    [MemberNotNullWhen(true, nameof(AutoClose))]
    public override bool IsValid => (AutoClose = (Config.Instance.AutoClose.TryGetValue(Door, out var cfg) ? cfg : null!)) is not null;
  }

  const int StateClosed = 0;

  readonly Dictionary<ServersideQoLZDO, Timestamp> _closeAfter = [];
  readonly SectorDictionary<PrefabInfo, HashSet<ServersideQoLZDO>> _doors = new(4);
  readonly Quaternion _expectedRot = Quaternion.Euler(0, 180, 0);
  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _pairedDoors = [];

  protected override void Initialize()
  {
    _closeAfter.Clear();
    _doors.Clear();
    _pairedDoors.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (zdo.Vars.GetCreator().Value is 0)
      return ProcessResult.UnregisterProcessor;

    var result = ProcessResult.UnregisterProcessor;

    int? state = null;
    if (prefabInfo.AutoClose.Value)
    {
      result &= ~ProcessResult.UnregisterProcessor;
      state = zdo.Vars.GetState();
      if (state is StateClosed)
      {
        if (_closeAfter.Remove(zdo))
          zdo.Destroyed -= OnDoorDestroyed;
      }
      else
      {
        if (!CheckMinDistance(peers, zdo, Config.Instance.AutoCloseMinPlayerDistance.Value))
          result |= ScheduleReprocessing();
        else if (!_closeAfter.TryGetValue(zdo, out var closeAfter))
        {
          _closeAfter.Add(zdo, closeAfter = Timestamp.Now.AddSeconds(Config.Instance.AutoCloseMinOpenSeconds.Value));
          zdo.Destroyed += OnDoorDestroyed;
          result |= ScheduleReprocessing(Config.Instance.AutoCloseMinOpenSeconds.Value);
        }
        else
        {
          var delay = closeAfter.Seconds - Timestamp.Now.Seconds;
          if (delay > 0)
            result |= ScheduleReprocessing(delay);
          else
          {
            state = StateClosed;
            zdo.Vars.SetState(StateClosed);
            if (_closeAfter.Remove(zdo))
              zdo.Destroyed -= OnDoorDestroyed;
          }
        }
      }
    }

    if (Config.Instance.SynchronizePairs.Value)
    {
      result &= ~ProcessResult.UnregisterProcessor;

      if (prefabInfo.PrefabInfo.Prefab?.GetBoundingBox() is { } bounds && _doors.TryAdd(prefabInfo, zdo))
      {
        var pos = zdo.ZDO.GetPosition();
        var rot = zdo.ZDO.GetRotation();
        Quaternion? inverseRot = null;
        float? expectedDistanceSqr = null;

        foreach (var doors in _doors.EnumerateAdjacent((pos, prefabInfo)))
        {
          foreach (var door in doors)
          {
            if (door == zdo || _pairedDoors.ContainsKey(door))
              continue;

            inverseRot ??= Quaternion.Inverse(rot);
            var relativeRot = inverseRot.Value * door.ZDO.GetRotation();
            var angle = Quaternion.Angle(relativeRot, _expectedRot);
            //Logger.DevLog($"{prefabInfo.PrefabInfo.PrefabName}: Angle={angle}");
            if (angle > 0.1f)
              continue;

            var v = door.ZDO.GetPosition() - pos;
            expectedDistanceSqr ??= Vector3.Project(rot * bounds.size, v).sqrMagnitude;
            var distanceSqr = v.sqrMagnitude;
            //Logger.DevLog($"{prefabInfo.PrefabInfo.PrefabName}: Distance={Mathf.Sqrt(distanceSqr)}, ExpectedDistance={Mathf.Sqrt(expectedDistanceSqr.Value)}");
            if (Mathf.Abs(distanceSqr - expectedDistanceSqr.Value) > 0.1f)
              continue;

            _pairedDoors.Add(zdo, door);
            _pairedDoors.Add(door, zdo);

            zdo.Destroyed += OnPairedDoorDestroyed;
            door.Destroyed += OnPairedDoorDestroyed;
            //Logger.DevLog("Pair found");
          }
        }
      }

      if (_pairedDoors.TryGetValue(zdo, out var paired))
      {
        state ??= zdo.Vars.GetState();
        paired.Vars.SetState(-state.Value);
      }
    }

    return result;
  }

  void OnDoorDestroyed(ServersideQoLZDO zdo) => _closeAfter.Remove(zdo);

  void OnPairedDoorDestroyed(ServersideQoLZDO zdo)
  {
    if (!_pairedDoors.Remove(zdo, out zdo))
      return;
    if (_pairedDoors.Remove(zdo))
      zdo.Destroyed -= OnPairedDoorDestroyed;
  }
}
