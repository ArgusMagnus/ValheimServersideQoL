using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class TrapProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Trap is null || zdo.Vars.GetCreator() is 0)
            return false;

        if (zdo.PrefabInfo.Trap is { Trap.Value: not null })
        {
            if (!Config.Traps.DisableTriggeredByPlayers.Value)
                zdo.Fields<Trap>().Reset(x => x.m_triggeredByPlayers);
            else if (zdo.Fields<Trap>().SetIfChanged(x => x.m_triggeredByPlayers, false))
                RecreateZdo = true;
        }

        var fields = zdo.Fields<Aoe>();
        if (!Config.Traps.DisableFriendlyFire.Value)
            fields.Reset(x => x.m_hitFriendly);
        else if (fields.SetIfChanged(x => x.m_hitFriendly, false)) // hitFriendly does not seem to be respected by sharp stakes
            RecreateZdo = true;

        if (fields.SetIfChanged(x => x.m_damageSelf, zdo.PrefabInfo.Trap.Value.Aoe.m_damageSelf * Config.Traps.SelfDamageMultiplier.Value))
            RecreateZdo = true;

        return false;
    }
}