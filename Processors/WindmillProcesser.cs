using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class WindmillProcesser(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    public override void Process(ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.Windmill is null || !Config.Windmills.IgnoreWind.Value)
            return;

        if (SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        /// <see cref="Windmill.GetPowerOutput()"/>
        zdo.Fields<Windmill>()
            .SetHasFields(true)
            .Set(x => x.m_minWindSpeed, float.MinValue);
        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    }
}
