namespace ServersideQoL.AutoMapTables;

[Processor("5450041b-4bfe-4839-9a55-7762457b2a36")]
public sealed class ShipProcessor : Processor<ShipProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Ship Ship, Piece Piece, ShipControlls ShipControls) : ProcessorPrefabInfo;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    Logger.DevLog("Not implemented");
    return ProcessResult.UnregisterProcessor;
  }
}
