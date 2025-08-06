namespace Valheim.ServersideQoL.Processors;

sealed class FermenterProcessor : Processor
{
    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Fermenter is null)
            return false;

        var fields = zdo.Fields<Fermenter>();
        if (Config.Fermenters.FermentationDurationMultiplier.Value is 1f)
            fields.Reset(static x => x.m_fermentationDuration);
        else if (fields.SetIfChanged(static x => x.m_fermentationDuration, zdo.PrefabInfo.Fermenter.m_fermentationDuration * Config.Fermenters.FermentationDurationMultiplier.Value))
            RecreateZdo = true;

        return false;
    }
}
