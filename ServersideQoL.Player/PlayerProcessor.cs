namespace ServersideQoL.Player;

[Processor("7b156eea-3364-40ca-83ad-417a55fa6e4b")]
public sealed class PlayerProcessor : Processor<PlayerProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(global::Player Player) : ProcessorPrefabInfo;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    Logger.DevLog("Not yet implemented");
    return ProcessResult.UnregisterProcessor;
  }
}
