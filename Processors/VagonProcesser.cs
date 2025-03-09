using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class VagonProcesser(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    public override void Process(ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.Vagon is null || float.IsNaN(Config.Carts.ContentMassMultiplier.Value))
            return;

        if (SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        /// <see cref="Vagon.UpdateMass()"/>
        zdo.Fields<Vagon>()
            .SetHasFields(true)
            .Set(x => x.m_itemWeightMassFactor, Config.Carts.ContentMassMultiplier.Value);
        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    }
}