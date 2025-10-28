namespace Valheim.ServersideQoL.Processors;

sealed class VagonProcesser : Processor
{
    protected override Guid Id { get; } = Guid.Parse("c4463e98-16e0-419b-9096-90307a332803");

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Vagon is null)
            return false;

        /// <see cref="Vagon.UpdateMass()"/>
        var fields = zdo.Fields<Vagon>();
        if (Config.Carts.ContentMassMultiplier.Value is 1f || float.IsNaN(Config.Carts.ContentMassMultiplier.Value))
            fields.Reset(static () => x => x.m_itemWeightMassFactor);
        else if (fields.UpdateValue(static () => x => x.m_itemWeightMassFactor, zdo.PrefabInfo.Vagon.m_itemWeightMassFactor * Config.Carts.ContentMassMultiplier.Value))
            RecreateZdo = true;

        return true;
    }
}
