using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class VagonProcesser(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override void ProcessCore(ref ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Vagon is null)
            return;

        if (SharedProcessorState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        /// <see cref="Vagon.UpdateMass()"/>
        var fields = zdo.Fields<Vagon>();
        if (fields.GetFloat(x => x.m_itemWeightMassFactor) != (float.IsNaN(Config.Carts.ContentMassMultiplier.Value) ? zdo.PrefabInfo.Vagon.m_itemWeightMassFactor : Config.Carts.ContentMassMultiplier.Value))
        {
            if (float.IsNaN(Config.Carts.ContentMassMultiplier.Value))
                fields.Reset(x => x.m_itemWeightMassFactor);
            else
                fields.Set(x => x.m_itemWeightMassFactor, Config.Carts.ContentMassMultiplier.Value);
        }
        SharedProcessorState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
        Logger.LogWarning("Cart weight updated");
    }
}
