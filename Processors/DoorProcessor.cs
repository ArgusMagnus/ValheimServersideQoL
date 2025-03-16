using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class DoorProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override void ProcessCore(ref ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Door is null || float.IsNaN(Config.Doors.AutoCloseMinPlayerDistance.Value))
            return;

        /// <see cref="Door.CanInteract"/>

        const int StateClosed = 0;

        if (zdo.GetInt(ZDOVars.s_state) is StateClosed || zdo.PrefabInfo.Door.m_keyItem is not null || zdo.PrefabInfo.Door.m_canNotBeClosed)
            return;

        if (!CheckMinDistance(peers, zdo, Config.Doors.AutoCloseMinPlayerDistance.Value))
            return;

        zdo.Set(ZDOVars.s_state, StateClosed);
    }
}