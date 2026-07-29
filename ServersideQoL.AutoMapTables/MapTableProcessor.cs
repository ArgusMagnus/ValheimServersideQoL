namespace ServersideQoL.AutoMapTables;

[Processor("05450dd6-13bd-42cc-9bd3-b1eed5e501af")]
public sealed class MapTableProcessor : Processor<MapTableProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(MapTable MapTable) : ProcessorPrefabInfo;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    Logger.DevLog("Not implemented");
    return ProcessResult.UnregisterProcessor;
  }
}
