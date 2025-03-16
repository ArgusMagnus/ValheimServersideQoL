using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class FireplaceProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override void ProcessCore(ref ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Fireplace is null || !(Config.Fireplaces.MakeToggleable.Value || Config.Fireplaces.InfiniteFuel.Value))
            return;

        if (SharedProcessorState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        var fields = zdo.Fields<Fireplace>();
        if (
            fields.GetBool(x => x.m_canTurnOff) == Config.Fireplaces.MakeToggleable.Value &&
            fields.GetFloat(x => x.m_secPerFuel) == (Config.Fireplaces.InfiniteFuel.Value ? 0 : zdo.PrefabInfo.Fireplace.m_secPerFuel) &&
            fields.GetBool(x => x.m_canRefill) == !Config.Fireplaces.InfiniteFuel.Value)
        {
            SharedProcessorState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
            return;
        }

        fields
            .Set(x => x.m_canTurnOff, Config.Fireplaces.MakeToggleable.Value)
            .Set(x => x.m_canRefill, !Config.Fireplaces.InfiniteFuel.Value);
        //.Set(x => x.m_infiniteFuel, Config.Fireplaces.InfiniteFuel.Value) // works, but removes the turn on/off hover text (turning on/off still works)
        if (Config.Fireplaces.InfiniteFuel.Value)
            fields.Set(x => x.m_secPerFuel, 0);
        else
            fields.Reset(x => x.m_secPerFuel);

        SharedProcessorState.DataRevisions.TryRemove(zdo.m_uid, out _);
        zdo = zdo.Recreate();

        SharedProcessorState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    }
}
