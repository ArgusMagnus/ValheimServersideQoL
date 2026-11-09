using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.Processors;

[Processor("8317acff-1602-4505-a2b3-cf74edc54a73", OnlyWhenDependedOn = true)]
public sealed class ShipRegistryProcessor  : Processor<ShipRegistryProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Ship Ship, Container? Container, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  readonly HashSet<ServersideQoLZDO> _ships = [];

  public bool IsInShipBounds(Vector3 pos)
  {
    foreach (var ship in _ships)
    {
      if (GetProcessorPrefabInfo(ship)?.PrefabInfo.Prefab?.GetBoundingBox() is not { } bounds)
        continue;

      var p = Quaternion.Inverse(ship.ZDO.GetRotation()) * (pos - ship.ZDO.GetPosition());
      if (bounds.Contains(p))
        return true;
    }
    return false;
  }

  protected internal override void Initialize()
  {
    _ships.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (_ships.Add(zdo))
      zdo.Destroyed += zdo => _ships.Remove(zdo);
    return ProcessResult.UnregisterProcessor;
  }
}
