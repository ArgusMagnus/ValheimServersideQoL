namespace Valheim.ServersideQoL.Processors;

sealed class TrapProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("9617e1bc-80d0-4852-af9d-2946bd6cb801");

    readonly List<(ExtendedZDO ZDO, DateTimeOffset RearmAfter)> _rearmAfter = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        UpdateRpcSubscription("RPC_OnStateChanged", RPC_OnStateChanged, Config.Traps.AutoRearm.Value);

        if (!firstTime)
            return;

        _rearmAfter.Clear();
    }

    void RPC_OnStateChanged(ExtendedZDO zdo, int state, long idOfClientModifyingState)
    {
        if (state is not 0) /// <see cref="Trap.TrapState.Unarmed"/>
            return;
        if (zdo.PrefabInfo.Trap is not { Trap.Value: not null })
            return;
        _rearmAfter.Add((zdo, DateTimeOffset.UtcNow.AddSeconds(zdo.PrefabInfo.Trap.Value.Trap.Value!.m_rearmCooldown)));
    }

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        for (int i = _rearmAfter.Count - 1; i >= 0; i--)
        {
            var (zdo, rearmAfter) = _rearmAfter[i];
            if (DateTimeOffset.UtcNow > rearmAfter)
            {
                if (zdo.IsValid() && zdo.PrefabInfo.Trap is { Trap.Value: not null })
                    RPC.RequestStateChange(zdo, 1); /// <see cref="Trap.TrapState.Armed"/>
                _rearmAfter.RemoveAt(i);
            }
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Trap is null || zdo.Vars.GetCreator() is 0)
            return false;

        if (zdo.PrefabInfo.Trap is { Trap.Value: not null })
        {
            if (!Config.Traps.DisableTriggeredByPlayers.Value)
                zdo.Fields<Trap>().Reset(static () => x => x.m_triggeredByPlayers);
            else if (zdo.Fields<Trap>().SetIfChanged(static () => x => x.m_triggeredByPlayers, false))
                RecreateZdo = true;
        }

        var fields = zdo.Fields<Aoe>();
        if (!Config.Traps.DisableFriendlyFire.Value)
            fields.Reset(static () => x => x.m_hitFriendly);
        else if (fields.SetIfChanged(static () => x => x.m_hitFriendly, false)) // hitFriendly does not seem to be respected by sharp stakes
            RecreateZdo = true;

        if (fields.SetIfChanged(static () => x => x.m_damageSelf, zdo.PrefabInfo.Trap.Value.Aoe.m_damageSelf * Config.Traps.SelfDamageMultiplier.Value))
            RecreateZdo = true;

        return false;
    }
}