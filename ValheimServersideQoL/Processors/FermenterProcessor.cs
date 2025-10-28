namespace Valheim.ServersideQoL.Processors;

sealed class FermenterProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("19367679-d46d-45e6-be8a-505d638cc133");

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Fermenter is null)
            return false;

        var fields = zdo.Fields<Fermenter>();
        if (Config.Fermenters.FermentationDurationMultiplier.Value is 1f)
            fields.Reset(static () => x => x.m_fermentationDuration);
        else if (fields.UpdateValue(static () => x => x.m_fermentationDuration, zdo.PrefabInfo.Fermenter.m_fermentationDuration * Config.Fermenters.FermentationDurationMultiplier.Value))
            RecreateZdo = true;

        return false;
    }
}
