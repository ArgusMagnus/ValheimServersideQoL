using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class FireplaceProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    public override void Process(ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.Fireplace is null || !Config.Fireplaces.MakeToggleable.Value)
            return;

        if (SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        zdo.Fields<Fireplace>()
            .SetHasFields(true)
            //.Set(x => x.m_infiniteFuel, true) // works, but removes the turn on/off hover text (turning on/off still works)
            .Set(x => x.m_secPerFuel, 0)
            .Set(x => x.m_canTurnOff, true)
            .Set(x => x.m_canRefill, false);

        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    }
}
