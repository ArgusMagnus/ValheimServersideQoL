namespace ServersideQoL.AutoMapTables;

[Processor("5450041b-4bfe-4839-9a55-7762457b2a36")]
public sealed class ShipProcessor : Processor<ShipProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Ship Ship, Piece Piece, ShipControlls ShipControls) : ProcessorPrefabInfo;

  readonly HashSet<ServersideQoLZDO> _ships = [];
  public IReadOnlyCollection<ServersideQoLZDO> Ships => _ships;

  protected override void Initialize()
  {
    _ships.Clear();
    foreach (var zdo in ZDOMan.instance.GetObjects().Select(static x => x.ServersideQoLZDO))
    {
      if (GetProcessorPrefabInfo(zdo) is null)
        continue;
      if (_ships.Add(zdo))
        zdo.Destroyed += OnShipDestroyed;
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (_ships.Add(zdo))
      zdo.Destroyed += OnShipDestroyed;
    return ProcessResult.UnregisterProcessor;
  }

  void OnShipDestroyed(ServersideQoLZDO zdo) => _ships.Remove(zdo);
}
