using BepInEx.Logging;
using static Valheim.ServersideQoL.ModConfig.WearNTearConfig;

namespace Valheim.ServersideQoL.Processors;

sealed class WearNTearProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.WearNTear is null)
            return false;

        var fields = zdo.Fields<WearNTear>();
        var isPlayerBuilt = zdo.PrefabInfo.WearNTear is { Piece: { Value: not null }, PieceTable: { Value: not null } } && zdo.Vars.GetCreator() is not 0;
        if (isPlayerBuilt)
        {
            if (!Config.WearNTear.DisableRainDamage.Value)
                fields.Reset(x => x.m_noRoofWear);
            else if (fields.SetIfChanged(x => x.m_noRoofWear, false))
                RecreateZdo = true;
        }

        var disableSupport = Config.WearNTear.DisableSupportRequirements.Value is DisableSupportRequirementsOptions.None ? false : (
                (Config.WearNTear.DisableSupportRequirements.Value.HasFlag(DisableSupportRequirementsOptions.PlayerBuilt) && isPlayerBuilt) ||
                (Config.WearNTear.DisableSupportRequirements.Value.HasFlag(DisableSupportRequirementsOptions.World) && !isPlayerBuilt));

        if (!disableSupport)
            fields.Reset(x => x.m_noSupportWear);
        else if (fields.SetIfChanged(x => x.m_noSupportWear, false))
            RecreateZdo = true;

        return true;
    }
}