using ServersideQoL.Utilities;

namespace ServersideQoL.SmartDefense;

[Processor(Id)]
public sealed class TrapsProcessor : Processor<TrapsProcessor.PrefabInfo>
{
  public const string Id = "983b37e4-1777-42b0-be4a-90b72e2f96d8";

  public sealed record PrefabInfo(Aoe Aoe, Piece Piece, PieceTable PieceTable, Trap? Trap) : ProcessorPrefabInfo;

  readonly Dictionary<ServersideQoLZDO, Timestamp> _rearmAfter = [];

  protected override void Initialize()
  {
    _rearmAfter.Clear();

    RPC.Intercept.UpdateInterception("RPC_OnStateChanged", RPC_OnStateChanged, Config.Instance.Traps.AutoRearm.Value);
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (Config.Instance.Traps.AutoRearm.Value && _rearmAfter.Remove(zdo, out var rearmAfter))
    {
      var now = Timestamp.Now;
      if (now < rearmAfter)
      {
        _rearmAfter.Add(zdo, rearmAfter);
        return ScheduleReprocessing(rearmAfter.Seconds - now.Seconds);
      }
      
      if (prefabInfo.Trap is not null)
        RPC.RequestStateChange(zdo, 1); /// <see cref="Trap.TrapState.Armed"/>
      return default;
    }

    if (zdo.Vars.GetCreator().Value is 0)
      return ProcessResult.UnregisterProcessor;

    var result = ProcessResult.Default;

    if (prefabInfo.Trap is not null)
    {
      if (!Config.Instance.Traps.DisableTriggeredByPlayers.Value)
        zdo.Fields<Trap>().Reset(static () => x => x.m_triggeredByPlayers);
      else if (zdo.Fields<Trap>().UpdateValue(static () => x => x.m_triggeredByPlayers, false))
        result |= ProcessResult.RecreateZDO;
    }

    var fields = zdo.Fields<Aoe>();
    if (!Config.Instance.Traps.DisableFriendlyFire.Value)
      fields.Reset(static () => x => x.m_hitFriendly);
    else if (fields.UpdateValue(static () => x => x.m_hitFriendly, false)) // hitFriendly does not seem to be respected by sharp stakes
      result |= ProcessResult.RecreateZDO;

    if (fields.UpdateValue(static () => x => x.m_damageSelf, prefabInfo.Aoe.m_damageSelf * Config.Instance.Traps.SelfDamageMultiplier.Value))
      result |= ProcessResult.RecreateZDO;

    if (!Config.Instance.Traps.AutoRearm.Value)
      result |= ProcessResult.UnregisterProcessor;
    return result;
  }

  void RPC_OnStateChanged(ServersideQoLZDO zdo, int state, long idOfClientModifyingState)
  {
    if (state is not 0) /// <see cref="Trap.TrapState.Unarmed"/>
      return;
    if (GetProcessorPrefabInfo(zdo)?.Trap is not { } trap)
      return;
    _rearmAfter[zdo] = Timestamp.Now.AddSeconds(trap.m_rearmCooldown);
  }
}
