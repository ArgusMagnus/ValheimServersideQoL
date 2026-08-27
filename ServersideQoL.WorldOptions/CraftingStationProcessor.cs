using ServersideQoL.Utilities;

namespace ServersideQoL.WorldOptions;

[Processor(Id)]
public sealed class CraftingStationProcessor : Processor<CraftingStationProcessor.PrefabInfo>
{
  public const string Id = "aaa674b7-2a64-42f1-aab5-e871f5dc0c63";

  public sealed record PrefabInfo(CraftingStation? CraftingStation, StationExtension? StationExtension) : ProcessorPrefabInfo;

  readonly Dictionary<ServersideQoLZDO, State> _states = [];

  protected override void Initialize()
  {
    _states.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (prefabInfo.CraftingStation is not null)
    {
      if (!_states.TryGetValue(zdo, out var state))
      {
        _states.Add(zdo, state = new(zdo, prefabInfo.CraftingStation));
        zdo.Destroyed += x => _states.Remove(x);
      }


    }
    else if (prefabInfo.StationExtension is not null)
    {
      var distSqr = zdo.Fields<StationExtension>().GetFloat(static () => x => x.m_maxStationDistance);
      distSqr *= distSqr;

      foreach (var state in _states.Values)
      {
        if (state.CraftingStation != prefabInfo.StationExtension.m_craftingStation)
          continue;
      }
    }

    return ProcessResult.UnregisterProcessor;
  }

  sealed class State(ServersideQoLZDO zdo, CraftingStation craftingStation)
  {
    public ServersideQoLZDO ZDO { get; } = zdo;
    public CraftingStation CraftingStation { get; } = craftingStation;
    public HashSet<ServersideQoLZDO> Extensions => field ??= [];
    public float BuildRange { get; set; } = craftingStation.m_rangeBuild;
  }
}
