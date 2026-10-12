namespace ServersideQoL.ReWarded;

[Processor(Id)]
public sealed class Processor : Processor<Processor.PrefabInfo>
{
  public const string Id = "fc6f1a13-ac94-44a5-acb4-61f012f2dfed";

  public sealed record PrefabInfo : ProcessorPrefabInfo;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    return ProcessResult.UnregisterProcessor;
  }
}
