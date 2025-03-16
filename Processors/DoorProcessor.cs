using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class DoorProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ref ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Door is null || float.IsNaN(Config.Doors.AutoCloseMinPlayerDistance.Value))
            return false;

        /// <see cref="Door.CanInteract"/>
        if (zdo.PrefabInfo.Door.m_keyItem is not null || zdo.PrefabInfo.Door.m_canNotBeClosed)
            return false;

        if (!CheckMinDistance(peers, zdo, Config.Doors.AutoCloseMinPlayerDistance.Value))
            return false;

        const int StateClosed = 0;

        if (zdo.GetInt(ZDOVars.s_state) is not StateClosed)
            zdo.Set(ZDOVars.s_state, StateClosed);

        return true;
    }
}