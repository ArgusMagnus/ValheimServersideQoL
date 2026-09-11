namespace ServersideQoL.PrefabConfigurator;

[Processor(Id)]
[RunBefore<PrefabProcessor>]
public sealed class ShipProcessor : Processor<ShipProcessor.PrefabInfo>
{
  public const string Id = "30b54643-5490-4e55-aca9-d9ad7b634398";
  public sealed record PrefabInfo(Ship Ship, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    var result = ProcessResult.UnregisterProcessor;
    if (zdo.Vars.GetCreator() == default)
      return result;

    if (zdo.Fields<Piece>().UpdateValue(static () => x => x.m_canBeRemoved, Config.Instance.Ships.DeconstructWithHammer.Value))
      result |= ProcessResult.RecreateZDO;

    return result;
  }
}
