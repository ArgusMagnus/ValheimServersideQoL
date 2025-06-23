using BepInEx.Logging;
using static Valheim.ServersideQoL.ModConfig.WearNTearConfig;

namespace Valheim.ServersideQoL.Processors;

sealed class WearNTearProcessor : Processor
{
    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.WearNTear is null || zdo.PrefabInfo.TeleportWorld is not null)
            return false;

        var fields = zdo.Fields<WearNTear>();
        var isPlayerBuilt = zdo.PrefabInfo.WearNTear is { Piece.Value: not null, PieceTable.Value: not null } && zdo.Vars.GetCreator() is not 0;
        if (isPlayerBuilt)
        {
            if (!Config.WearNTear.DisableRainDamage.Value)
                fields.Reset(x => x.m_noRoofWear);
            else if (fields.SetIfChanged(x => x.m_noRoofWear, false))
                RecreateZdo = true;

            if (!Config.WearNTear.MakeIndestructible.Value)
            {
                if (fields.ResetIfChanged(x => x.m_health))
                    zdo.Vars.RemoveHealth();
            }
            else if (fields.SetIfChanged(x => x.m_health, -1))
            {
                zdo.Vars.SetHealth(-1);
                RecreateZdo = true;
            }
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