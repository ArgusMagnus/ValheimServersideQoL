using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class DoorProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    protected override void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.Door is null || float.IsNaN(Config.Doors.AutoCloseMinPlayerDistance.Value))
            return;

        /// <see cref="Door.CanInteract"/>

        const int StateClosed = 0;

        if (zdo.GetInt(ZDOVars.s_state) is StateClosed || prefabInfo.Door.m_keyItem is not null || prefabInfo.Door.m_canNotBeClosed)
            return;

        if (!CheckMinDistance(peers, zdo, Config.Doors.AutoCloseMinPlayerDistance.Value))
            return;

        zdo.Set(ZDOVars.s_state, StateClosed);
    }
}