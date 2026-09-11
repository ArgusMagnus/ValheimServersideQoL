namespace ServersideQoL.PrefabConfigurator;

[Processor(Id)]
[RunBefore<PrefabProcessor>]
public sealed class FireplaceProcessor : Processor<ProcessorPrefabInfo<Fireplace>>
{
  public const string Id = "8fbc8f94-481c-429a-b201-f4db85f772eb";

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<Fireplace> prefabInfo)
  {
    if (zdo.Vars.GetCreator() == default)
      return ProcessResult.UnregisterProcessor;

    var result = ProcessResult.UnregisterProcessor;
    var fields = zdo.Fields<Fireplace>();
    if (!Config.Instance.Fireplaces.MakeToggleable.Value)
      fields.Reset(static () => x => x.m_canTurnOff);
    else if (fields.UpdateValue(static () => x => x.m_canTurnOff, true))
      result |= ProcessResult.RecreateZDO;

    if (!Config.Instance.Fireplaces.InfiniteFuel.Value)
      fields.Reset(static () => x => x.m_secPerFuel).Reset(static () => x => x.m_canRefill);
    else
    {
      if (fields.UpdateValue(static () => x => x.m_secPerFuel, 0))
        result |= ProcessResult.RecreateZDO;
      if (fields.UpdateValue(static () => x => x.m_canRefill, false))
        result |= ProcessResult.RecreateZDO;
      zdo.Vars.SetFuel(fields.GetFloat(static () => x => x.m_maxFuel));
    }

    return result;
  }
}
