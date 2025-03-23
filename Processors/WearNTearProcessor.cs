using BepInEx.Logging;
using static Valheim.ServersideQoL.ModConfig.WearNTearConfig;

namespace Valheim.ServersideQoL.Processors;

sealed class WearNTearProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        if (zdo.PrefabInfo.WearNTear is null)
            return false;

        var fields = zdo.Fields<WearNTear>();
        var isPlayerBuilt = zdo.PrefabInfo is { Piece: not null, PieceTable: not null } && zdo.GetLong(ZDOVars.s_creator) is not 0;
        if (isPlayerBuilt && fields.SetIfChanged(x => x.m_noRoofWear, !Config.WearNTear.DisableRainDamage.Value))
            recreate = true;

        var disableSupport =
            (Config.WearNTear.DisableSupportRequirements.Value.HasFlag(DisableSupportRequirementsOptions.PlayerBuilt) && isPlayerBuilt) ||
            (Config.WearNTear.DisableSupportRequirements.Value.HasFlag(DisableSupportRequirementsOptions.World) && !isPlayerBuilt);

        if (fields.SetIfChanged(x => x.m_noSupportWear, !disableSupport))
            recreate = true;

        return true;
    }
}
