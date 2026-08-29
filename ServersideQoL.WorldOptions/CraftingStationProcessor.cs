namespace ServersideQoL.WorldOptions;

[Processor(Id)]
public sealed class CraftingStationProcessor : Processor<CraftingStationProcessor.PrefabInfo>
{
  public const string Id = "aaa674b7-2a64-42f1-aab5-e871f5dc0c63";

  public sealed record PrefabInfo(CraftingStation? CraftingStation, StationExtension? StationExtension) : ProcessorPrefabInfo;

  readonly Dictionary<CraftingStation, StationInfo> _infos = [];

  protected override void Initialize()
  {
    _infos.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!Config.Instance.CraftingStationsShareBuildRadius.Value || PlacedObjects.Contains(zdo))
      return ProcessResult.UnregisterProcessor;

    if (prefabInfo.CraftingStation is not null)
    {
      if (!_infos.TryGetValue(prefabInfo.CraftingStation, out var info))
        _infos.Add(prefabInfo.CraftingStation, info = new(prefabInfo.CraftingStation));

      if (!info.States.TryGetValue(zdo, out var state))
      {
        info.States.Add(zdo, state = new(zdo, info));
        zdo.Destroyed += OnCraftingStationDestroyed;
      }

      if (state.Stations is { Count: > 0 })
      {
        foreach (var piece in state.Stations.Values)
          DestroyObject(piece);
        state.Stations.Clear();
      }      

      foreach (var otherInfo in _infos.Values)
      {
        if (otherInfo == info)
          continue;

        foreach (var other in otherInfo.States.Values)
        {
          var maxDistSqr = state.GetBuildRange() + other.GetBuildRange();
          maxDistSqr *= maxDistSqr;
          if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), other.ZDO.ZDO.GetPosition()) > maxDistSqr)
            continue;

          PlaceStation(state, other.ZDO.ZDO.GetPrefab());
          PlaceStation(other, zdo.ZDO.GetPrefab());
          break;
        }
      }

      return default;
    }
    else if (prefabInfo.StationExtension is not null)
    {
      if (!_infos.TryGetValue(prefabInfo.StationExtension.m_craftingStation, out var info))
        _infos.Add(prefabInfo.StationExtension.m_craftingStation, info = new(prefabInfo.StationExtension.m_craftingStation));
      info.Extensions.Add(zdo);
      zdo.Destroyed += OnExtensionDestroyed;
      ScheduleReprocessStationsInRange(info, zdo, prefabInfo.StationExtension);
    }

    return ProcessResult.UnregisterProcessor;
  }

  void OnCraftingStationDestroyed(ServersideQoLZDO zdo)
  {
    if (GetProcessorPrefabInfo(zdo)?.CraftingStation is not { } craftingStation)
      return;
    if (!_infos.TryGetValue(craftingStation, out var info))
      return;
    if (!info.States.Remove(zdo))
      return;
    foreach (var x in info.States.Keys)
      ScheduleReprocessing(x);
  }

  void OnExtensionDestroyed(ServersideQoLZDO zdo)
  {
    if (GetProcessorPrefabInfo(zdo)?.StationExtension is not { } stationExtension)
      return;
    if (!_infos.TryGetValue(stationExtension.m_craftingStation, out var info))
      return;
    if (info.Extensions.Remove(zdo))
      ScheduleReprocessStationsInRange(info, zdo, stationExtension);
  }

  void ScheduleReprocessStationsInRange(StationInfo info, ServersideQoLZDO zdo, StationExtension stationExtension)
  {
    var distSqr = zdo.Fields<StationExtension>().GetFloat(static () => x => x.m_maxStationDistance);
    distSqr *= distSqr;
    foreach (var state in info.States.Values)
    {
      if (info.CraftingStation != stationExtension.m_craftingStation)
        continue;
      if (Utils.DistanceSqr(state.ZDO.ZDO.GetPosition(), zdo.ZDO.GetPosition()) > distSqr)
        continue;

      state.ResetBuildRange();
      ScheduleReprocessing(state.ZDO);
    }
  }

  void PlaceStation(State state, int prefab)
  {
    if (state.Stations?.ContainsKey(prefab) is true)
      return;

    var pos = state.ZDO.ZDO.GetPosition();
    pos.y -= 10000;
    var zdo = PlacePiece(pos, prefab, 0);
    zdo.Fields<CraftingStation>()
      .Set(static () => x => x.m_rangeBuild, state.GetBuildRange())
      .Set(static () => x => x.m_extraRangePerLevel, 0);
    (state.Stations ??= []).Add(prefab, zdo);
  }

  sealed class StationInfo(CraftingStation craftingStation)
  {
    public CraftingStation CraftingStation { get; } = craftingStation;
    public Dictionary<ServersideQoLZDO, State> States { get; } = [];
    public List<ServersideQoLZDO> Extensions { get; } = [];
  }

  sealed class State(ServersideQoLZDO zdo, StationInfo info)
  {
    public ServersideQoLZDO ZDO { get; } = zdo;
    public StationInfo Info { get; } = info;
    public Dictionary<int, ServersideQoLZDO>? Stations { get; set; }
    float? _buildRange;
    public void ResetBuildRange() => _buildRange = null;
    public float GetBuildRange()
    {
      if (_buildRange is not { } value)
      {
        var count = 0;
        foreach (var extension in Info.Extensions)
        {
          var distSqr = extension.Fields<StationExtension>().GetFloat(static () => x => x.m_maxStationDistance);
          distSqr *= distSqr;
          if (Utils.DistanceSqr(ZDO.ZDO.GetPosition(), extension.ZDO.GetPosition()) <= distSqr)
            count++;
        }

        var fields = ZDO.Fields<CraftingStation>();
        _buildRange = value = fields.GetFloat(static () => x => x.m_rangeBuild) +
          count * fields.GetFloat(static () => x => x.m_extraRangePerLevel);
      }
      return value;
    }
  }
}
