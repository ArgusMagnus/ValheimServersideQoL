using ServersideQoL.Processors;

namespace ServersideQoL.ObliteratorOfPotential;

[Processor(Id)]
[DependsOn<ContainerRegistryProcessor>]
public sealed class IncineratorProcessor : Processor<IncineratorProcessor.PrefabInfo>
{
  public const string Id = "2006ea3b-d05a-49bb-9d59-28b19a4bd7f7";
  public sealed record PrefabInfo(Incinerator Incinerator, Container Container, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  readonly HashSet<ServersideQoLZDO> _incinerators = [];

  protected override void Initialize()
  {
    _incinerators.Clear();
  }

  protected override bool ClaimExclusive(ServersideQoLZDO zdo) => _incinerators.Contains(zdo) || base.ClaimExclusive(zdo);

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    //var state = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo.Container);
    if (_incinerators.Add(zdo))
      zdo.Destroyed += zdo => _incinerators.Remove(zdo);

    return ProcessResult.UnregisterProcessor;
  }
}
