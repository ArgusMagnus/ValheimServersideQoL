using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class WindmillProcesser(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Windmill is null)
            return false;

        /// <see cref="Windmill.GetPowerOutput()"/>
        var fields = zdo.Fields<Windmill>();
        if (!Config.Windmills.IgnoreWind.Value)
            fields.Reset(x => x.m_minWindSpeed);
        else if (fields.SetIfChanged(x => x.m_minWindSpeed, float.MinValue))
            RecreateZdo = true;

        return true;
    }
}
