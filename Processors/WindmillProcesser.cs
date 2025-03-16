using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class WindmillProcesser(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override void ProcessCore(ref ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Windmill is null)
            return;

        if (SharedProcessorState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        /// <see cref="Windmill.GetPowerOutput()"/>
        var fields = zdo.Fields<Windmill>();
        if (Config.Windmills.IgnoreWind.Value)
            fields.Set(x => x.m_minWindSpeed, float.MinValue);
        else
            fields.Reset(x => x.m_minWindSpeed);
        SharedProcessorState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    }
}
