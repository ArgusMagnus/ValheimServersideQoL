using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class VagonProcesser(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    readonly int CartPrefab = "Cart".GetStableHashCode();
    public override void Process(ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        // not working prefabInfo.Vagon is always null and setting m_itemWeightMassFactor anyway has no effect

        ////if (prefabInfo.Vagon is null || float.IsNaN(Config.Carts.ContentMassMultiplier.Value))
        ////    return;

        //if (zdo.GetPrefab() == CartPrefab)
        //    return;

        //Logger.LogWarning($"Vagon found, multiplier = {Config.Carts.ContentMassMultiplier.Value}");

        //if (SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
        //    return;

        ///// <see cref="Vagon.UpdateMass()"/>
        //zdo.Fields<Vagon>()
        //    .SetHasFields(true)
        //    .Set(x => x.m_itemWeightMassFactor, Config.Carts.ContentMassMultiplier.Value);
        //SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
        //Logger.LogWarning("Cart weight updated");
    }
}