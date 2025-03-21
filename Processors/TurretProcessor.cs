using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class TurretProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        if (zdo.PrefabInfo is not { Turret: not null, Piece: not null, PieceTable: not null })
            return false;

        var fields = zdo.Fields<Turret>();
        if (fields.GetBool(x => x.m_targetPlayers) != !Config.Turrets.DontTargetPlayers.Value)
        {
            fields.Set(x => x.m_targetPlayers, !Config.Turrets.DontTargetPlayers.Value);
            recreate = true;
        }
        if (fields.GetBool(x => x.m_targetTamed) != !Config.Turrets.DontTargetTames.Value)
        {
            fields.Set(x => x.m_targetTamed, !Config.Turrets.DontTargetTames.Value);
            recreate = true;
        }
        if (fields.GetBool(x => x.m_targetTamedConfig) != !Config.Turrets.DontTargetTames.Value)
        {
            fields.Set(x => x.m_targetTamedConfig, !Config.Turrets.DontTargetTames.Value);
            recreate = true;
        }

        /// <see cref="Turret.RPC_AddAmmo"/>

        return true;
    }
}
