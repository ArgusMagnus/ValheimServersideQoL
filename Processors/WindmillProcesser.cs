using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class WindmillProcesser(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        if (zdo.PrefabInfo.Windmill is null)
            return false;

        /// <see cref="Windmill.GetPowerOutput()"/>
        var fields = zdo.Fields<Windmill>();
        if (fields.GetFloat(x => x.m_minWindSpeed) == (Config.Windmills.IgnoreWind.Value ? float.MinValue : zdo.PrefabInfo.Windmill.m_minWindSpeed))
            return true;

        if (Config.Windmills.IgnoreWind.Value)
            fields.Set(x => x.m_minWindSpeed, float.MinValue);
        else
            fields.Reset(x => x.m_minWindSpeed);

        recreate = true;
        return true;
    }
}
