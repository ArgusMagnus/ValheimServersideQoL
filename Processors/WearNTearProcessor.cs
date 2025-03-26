using BepInEx.Logging;
using static Valheim.ServersideQoL.ModConfig.WearNTearConfig;

namespace Valheim.ServersideQoL.Processors;

sealed class WearNTearProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        zdo.Unregister(this);
        if (zdo.PrefabInfo.WearNTear is null)
            return false;

        var fields = zdo.Fields<WearNTear>();
        var isPlayerBuilt = zdo.PrefabInfo is { Piece: not null, PieceTable: not null } && zdo.Vars.GetCreator() is not 0;
        if (isPlayerBuilt)
        {
            if (!Config.WearNTear.DisableRainDamage.Value)
                fields.Reset(x => x.m_noRoofWear);
            else if (fields.SetIfChanged(x => x.m_noRoofWear, false))
                recreate = true;
        }

        var disableSupport = Config.WearNTear.DisableSupportRequirements.Value is DisableSupportRequirementsOptions.None ? false : (
                (Config.WearNTear.DisableSupportRequirements.Value.HasFlag(DisableSupportRequirementsOptions.PlayerBuilt) && isPlayerBuilt) ||
                (Config.WearNTear.DisableSupportRequirements.Value.HasFlag(DisableSupportRequirementsOptions.World) && !isPlayerBuilt));

        if (!disableSupport)
            fields.Reset(x => x.m_noSupportWear);
        else if (fields.SetIfChanged(x => x.m_noSupportWear, false))
            recreate = true;

        return true;
    }
}