using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class FireplaceProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    protected override void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.Fireplace is null || !(Config.Fireplaces.MakeToggleable.Value || Config.Fireplaces.InfiniteFuel.Value))
            return;

        if (SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        var fields = zdo.Fields(prefabInfo.Fireplace);
        if (
            fields.GetBool(x => x.m_canTurnOff) == Config.Fireplaces.MakeToggleable.Value &&
            fields.GetFloat(x => x.m_secPerFuel) == (Config.Fireplaces.InfiniteFuel.Value ? 0 : prefabInfo.Fireplace.m_secPerFuel) &&
            fields.GetBool(x => x.m_canRefill) == !Config.Fireplaces.InfiniteFuel.Value)
        {
            SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
            return;
        }

        fields.SetHasFields(true)
            .Set(x => x.m_canTurnOff, Config.Fireplaces.MakeToggleable.Value)
            .Set(x => x.m_secPerFuel, Config.Fireplaces.InfiniteFuel.Value ? 0 : prefabInfo.Fireplace.m_secPerFuel)
            .Set(x => x.m_canRefill, !Config.Fireplaces.InfiniteFuel.Value);
        //.Set(x => x.m_infiniteFuel, true) // works, but removes the turn on/off hover text (turning on/off still works)

        SharedState.DataRevisions.TryRemove(zdo.m_uid, out _);
        zdo = zdo.Recreate();

        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    }
}
