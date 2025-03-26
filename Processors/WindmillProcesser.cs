using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class WindmillProcesser(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        zdo.Unregister(this);
        if (zdo.PrefabInfo.Windmill is null)
            return false;

        /// <see cref="Windmill.GetPowerOutput()"/>
        var fields = zdo.Fields<Windmill>();
        if (!Config.Windmills.IgnoreWind.Value)
            fields.Reset(x => x.m_minWindSpeed);
        else if (fields.SetIfChanged(x => x.m_minWindSpeed, float.MinValue))
            recreate = true;

        return true;
    }
}
