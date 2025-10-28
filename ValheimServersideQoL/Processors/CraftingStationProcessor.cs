
namespace Valheim.ServersideQoL.Processors;

sealed class CraftingStationProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("a04a766c-a580-470a-9e6c-758a6ae7ccdb");

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.CraftingStation is not null)
        {
            if (!Config.CraftingStations.StationConfig.TryGetValue(zdo.PrefabInfo.CraftingStation, out var cfg))
                return false;
            var fields = zdo.Fields<CraftingStation>();
            if (cfg.BuildRange is not null && fields.UpdateValue(static () => x => x.m_rangeBuild, cfg.BuildRange.Value))
                RecreateZdo = true;
            if (cfg.ExtraBuildRangePerLevel is not null && fields.UpdateValue(static () => x => x.m_extraRangePerLevel, cfg.ExtraBuildRangePerLevel.Value))
                RecreateZdo = true;
        }
        else if (zdo.PrefabInfo.StationExtension is not null)
        {
            if (!Config.CraftingStations.StationConfig.TryGetValue(zdo.PrefabInfo.StationExtension.m_craftingStation, out var cfg) || cfg.MaxExtensionDistance is null)
                return false;
            var fields = zdo.Fields<StationExtension>();
            if (float.IsNaN(cfg.MaxExtensionDistance.Value))
                fields.Reset(static () => x => x.m_maxStationDistance);
            else if (fields.UpdateValue(static () => x => x.m_maxStationDistance, cfg.MaxExtensionDistance.Value))
                RecreateZdo = true;
        }
        return false;
    }
}
