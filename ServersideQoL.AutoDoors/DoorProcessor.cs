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

      if (prefabInfo.PrefabInfo.Prefab is not null && _doors.TryAdd(prefabInfo, zdo))
      {
        Dictionary<string, Vector3>? snapPoints = null;

        foreach (var doors in _doors.EnumerateAdjacent((zdo.ZDO.GetPosition(), prefabInfo)))
        {
          foreach (var door in doors)
          {
            if (door == zdo || _pairedDoors.ContainsKey(door))
              continue;

            const string SnapPointTag = "snappoint";

            var originalPos = prefabInfo.PrefabInfo.Prefab.transform.localPosition;
            var originalRot = prefabInfo.PrefabInfo.Prefab.transform.localRotation;
            try
            {
              if (snapPoints is null)
              {
                snapPoints = new(prefabInfo.PrefabInfo.Prefab.transform.childCount);
                prefabInfo.PrefabInfo.Prefab.transform.localPosition = zdo.ZDO.GetPosition();
                prefabInfo.PrefabInfo.Prefab.transform.localRotation = zdo.ZDO.GetRotation();
                for (int i = 0; i < prefabInfo.PrefabInfo.Prefab.transform.childCount; i++)
                {
                  var child = prefabInfo.PrefabInfo.Prefab.transform.GetChild(i);
                  if (child.CompareTag(SnapPointTag))
                    snapPoints.Add(child.name, child.position);
                }
              }

              if (snapPoints.Count is 0)
                continue;

              prefabInfo.PrefabInfo.Prefab.transform.localPosition = door.ZDO.GetPosition();
              prefabInfo.PrefabInfo.Prefab.transform.localRotation = door.ZDO.GetRotation();
              for (int i = 0; i < prefabInfo.PrefabInfo.Prefab.transform.childCount; i++)
              {
                var child = prefabInfo.PrefabInfo.Prefab.transform.GetChild(i);
                if (!child.CompareTag(SnapPointTag))
                  continue;
                if (Utils.DistanceSqr(child.position, snapPoints[child.name]) > 0.01f)
                  continue;

                _pairedDoors.Add(zdo, door);
                _pairedDoors.Add(door, zdo);
                zdo.Destroyed += OnPairedDoorDestroyed;
                door.Destroyed += OnPairedDoorDestroyed;
                break;
              }
            }
            finally
            {
              prefabInfo.PrefabInfo.Prefab.transform.localPosition = originalPos;
              prefabInfo.PrefabInfo.Prefab.transform.localRotation = originalRot;
            }
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
