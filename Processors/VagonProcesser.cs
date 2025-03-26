using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class VagonProcesser(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        zdo.Unregister(this);
        if (zdo.PrefabInfo.Vagon is null)
            return false;

        /// <see cref="Vagon.UpdateMass()"/>
        var fields = zdo.Fields<Vagon>();
        if (float.IsNaN(Config.Carts.ContentMassMultiplier.Value))
            fields.Reset(x => x.m_itemWeightMassFactor);
        else if (fields.SetIfChanged(x => x.m_itemWeightMassFactor, Config.Carts.ContentMassMultiplier.Value))
            recreate = true;

        return true;
    }
}
