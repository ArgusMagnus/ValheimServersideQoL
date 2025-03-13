using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class VagonProcesser(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    protected override void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.Vagon is null)
            return;

        if (SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        /// <see cref="Vagon.UpdateMass()"/>
        var fields = zdo.Fields(prefabInfo.Vagon);
        if (fields.GetFloat(x => x.m_itemWeightMassFactor) != (float.IsNaN(Config.Carts.ContentMassMultiplier.Value) ? prefabInfo.Vagon.m_itemWeightMassFactor : Config.Carts.ContentMassMultiplier.Value))
        {
            if (float.IsNaN(Config.Carts.ContentMassMultiplier.Value))
                fields.Reset(x => x.m_itemWeightMassFactor);
            else
                fields.Set(x => x.m_itemWeightMassFactor, Config.Carts.ContentMassMultiplier.Value);
        }
        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
        Logger.LogWarning("Cart weight updated");
    }
}
