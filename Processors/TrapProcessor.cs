using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class TrapProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Trap is null)
            return false;

        if (!Config.Traps.DisableTriggeredByPlayers.Value)
            zdo.Fields<Trap>().Reset(x => x.m_triggeredByPlayers);
        else if (zdo.Fields<Trap>().SetIfChanged(x => x.m_triggeredByPlayers, false))
            RecreateZdo = true;

        var fields = zdo.Fields<Aoe>();
        if (!Config.Traps.DisableFriendlyFire.Value)
            fields.Reset(x => x.m_hitFriendly);
        else if (fields.SetIfChanged(x => x.m_hitFriendly, false))
            RecreateZdo = true;

        if (fields.SetIfChanged(x => x.m_damageSelf, zdo.PrefabInfo.Trap.Value.Aoe.m_damageSelf * Config.Traps.SelfDamageMultiplier.Value))
            RecreateZdo = true;        

        return false;
    }
}