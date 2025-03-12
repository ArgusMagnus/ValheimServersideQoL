using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class WindmillProcesser(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    protected override void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.Windmill is null)
            return;

        if (SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        /// <see cref="Windmill.GetPowerOutput()"/>
        var fields = zdo.Fields(prefabInfo.Windmill);
        if (Config.Windmills.IgnoreWind.Value)
            fields.Set(x => x.m_minWindSpeed, float.MinValue);
        else
            fields.Reset(x => x.m_minWindSpeed);
        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    }
}
