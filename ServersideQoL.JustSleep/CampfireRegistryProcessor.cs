using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.JustSleep;

[Processor("a4269470-d5c9-4ca5-b755-a8fb9f8cb4ee", OnlyWhenDependedOn = true)]
public sealed class CampfireRegistryProcessor : Processor<ProcessorPrefabInfo<Fireplace>>
{
  readonly SectorDictionary<HashSet<ServersideQoLZDO>> _fires = new(ZoneSystem.c_ZoneSize);
  readonly Dictionary<ServersideQoLZDO, Vector3> _positions = [];

  protected internal override void Initialize()
  {
    foreach (var zdo in _positions.Keys)
      zdo.Destroyed -= OnFireDestroyed;
    _fires.Clear();
    _positions.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<Fireplace> prefabInfo)
  {
    var position = zdo.ZDO.GetPosition();
    if (_positions.TryGetValue(zdo, out var oldPosition))
    {
      if (oldPosition != position)
      {
        _fires.Remove(oldPosition, zdo);
        _fires.TryAdd(position, zdo);
        _positions[zdo] = position;
      }
    }
    else
    {
      _positions.Add(zdo, position);
      _fires.TryAdd(position, zdo);
      zdo.Destroyed += OnFireDestroyed;
    }

    return default;
  }

  public bool IsNearLitFire(Vector3 position, float range)
  {
    var rangeSqr = range * range;
    foreach (var fires in _fires.EnumerateAdjacent(position))
    {
      foreach (var fire in fires)
      {
        if (fire.Vars.GetFuel() > 0 && Vector3.SqrMagnitude(position - fire.ZDO.GetPosition()) <= rangeSqr)
          return true;
      }
    }

    return false;
  }

  void OnFireDestroyed(ServersideQoLZDO zdo)
  {
    if (!_positions.Remove(zdo, out var position))
      return;
    _fires.Remove(position, zdo);
  }
}
