namespace ServersideQoL.PrefabConfigurator;

[Processor(Id)]
[RunBefore<PrefabProcessor>]
public sealed class CartProcessor : Processor<CartProcessor.PrefabInfo>
{
  public const string Id = "53933a3a-e84b-44be-9ebf-3242d413f5fb";
  public sealed record PrefabInfo(Vagon Vagon, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    var result = ProcessResult.UnregisterProcessor;
    if (zdo.Vars.GetCreator() == default)
      return result;

    /// <see cref="Vagon.UpdateMass()"/>
    var fields = zdo.Fields<Vagon>();
    if (Config.Instance.Carts.ContentMassMultiplier.Value is 1f || float.IsNaN(Config.Instance.Carts.ContentMassMultiplier.Value))
      fields.Reset(static () => x => x.m_itemWeightMassFactor);
    else if (fields.UpdateValue(static () => x => x.m_itemWeightMassFactor, prefabInfo.Vagon.m_itemWeightMassFactor * Config.Instance.Carts.ContentMassMultiplier.Value))
      result |= ProcessResult.RecreateZDO;

    if (zdo.Fields<Piece>().UpdateValue(static () => x => x.m_canBeRemoved, Config.Instance.Carts.DeconstructWithHammer.Value))
      result |= ProcessResult.RecreateZDO;

    return result;
  }
}
