using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class VagonProcesser : Processor
{
    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Vagon is null)
            return false;

        /// <see cref="Vagon.UpdateMass()"/>
        var fields = zdo.Fields<Vagon>();
        if (Config.Carts.ContentMassMultiplier.Value is 1f || float.IsNaN(Config.Carts.ContentMassMultiplier.Value))
            fields.Reset(x => x.m_itemWeightMassFactor);
        else if (fields.SetIfChanged(x => x.m_itemWeightMassFactor, zdo.PrefabInfo.Vagon.m_itemWeightMassFactor * Config.Carts.ContentMassMultiplier.Value))
            RecreateZdo = true;

        return true;
    }
}
