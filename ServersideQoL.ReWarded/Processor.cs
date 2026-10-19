using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.ReWarded;

[Processor(Id)]
public sealed class Processor : Processor<Processor.PrefabInfo>
{
  public const string Id = "fc6f1a13-ac94-44a5-acb4-61f012f2dfed";

  public sealed record PrefabInfo(WearNTear WearNTear, Piece Piece, PieceTable PieceTable, PrivateArea? PrivateArea) : ProcessorPrefabInfo;

  SectorDictionary<HashSet<ServersideQoLZDO>> _pieces = new(ZoneSystem.c_ZoneSizeHalf);
  readonly HashSet<ServersideQoLZDO> _wards = [];

  protected override void Initialize()
  {
    _pieces.Clear();
    _wards.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (zdo.Vars.GetCreator().Value is 0)
      return ProcessResult.UnregisterProcessor;

    if (prefabInfo.PrivateArea is not null)
    {
      if (_wards.Add(zdo))
        zdo.Destroyed += zdo => _wards.Remove(zdo);

      var fields = zdo.Fields<PrivateArea>();
      var radius = Mathf.Ceil(fields.GetFloat(static () => x => x.m_radius));
      if (_pieces is null)
        _pieces = new(radius);
      else if (_pieces.SectorWidth != radius)
      {
        (var old, _pieces) = (_pieces, new(radius));
        foreach (var pieceSet in old.Values)
        {
          foreach (var piece in pieceSet)
            _pieces.Add(piece);
          pieceSet.Clear();
        }
        old.Clear();
      }
    }



    _pieces.TryAdd(zdo);

    return ProcessResult.UnregisterProcessor;
  }
}
