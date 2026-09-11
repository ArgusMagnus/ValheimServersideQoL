namespace ServersideQoL.PrefabConfigurator;

[Processor(Id)]
[RunBefore<PrefabProcessor>]
public sealed class PlantProcessor : Processor<ProcessorPrefabInfo<Plant>>
{
  public const string Id = "74bb1f8b-b268-4264-b117-7ecae6957f74";

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<Plant> prefabInfo)
  {
    var result = ProcessResult.UnregisterProcessor;
    var fields = zdo.Fields<Plant>();
    if (Config.Instance.Plants.GrowTimeMultiplier.Value is not 1f)
    {
      if (fields.UpdateValue(static () => x => x.m_growTime, prefabInfo.Component.m_growTime * Config.Instance.Plants.GrowTimeMultiplier.Value))
        result |= ProcessResult.RecreateZDO;
      if (fields.UpdateValue(static () => x => x.m_growTimeMax, prefabInfo.Component.m_growTimeMax * Config.Instance.Plants.GrowTimeMultiplier.Value))
        result |= ProcessResult.RecreateZDO;
    }
    if (Config.Instance.Plants.SpaceRequirementMultiplier.Value is not 1f)
    {
      if (fields.UpdateValue(static () => x => x.m_growRadius, prefabInfo.Component.m_growRadius * Config.Instance.Plants.SpaceRequirementMultiplier.Value))
        result |= ProcessResult.RecreateZDO;
      //if (fields.SetIfChanged(static x => x.m_growRadiusVines, prefabInfo.Component.m_growRadiusVines * Config.Instance.Plants.SpaceRequirementMultiplier.Value))
      //  result |= ProcessResult.RecreateZDO;
    }

    if (!Config.Instance.Plants.DontDestroyIfCantGrow.Value)
      fields.Reset(static () => x => x.m_destroyIfCantGrow);
    else if (fields.UpdateValue(static () => x => x.m_destroyIfCantGrow, false))
      result |= ProcessResult.RecreateZDO;

    return result;
  }
}
