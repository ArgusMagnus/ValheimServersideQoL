using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class FireplaceProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
	protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        zdo.Unregister(this);
        if (zdo.PrefabInfo.Fireplace is null)
            return false;

        var fields = zdo.Fields<Fireplace>();
        if (!Config.Fireplaces.MakeToggleable.Value)
            fields.Reset(x => x.m_canTurnOff);
        else if (fields.SetIfChanged(x => x.m_canTurnOff, true))
            recreate = true;

        if (!Config.Fireplaces.InfiniteFuel.Value)
            fields.Reset(x => x.m_secPerFuel).Reset(x => x.m_canRefill);
        else
        {
            if (fields.SetIfChanged(x => x.m_secPerFuel, 0))
                recreate = true;
            if (fields.SetIfChanged(x => x.m_canRefill, false))
                recreate = true;
            zdo.Vars.SetFuel(fields.GetFloat(x => x.m_maxFuel));
        }
        
        return true;
    }
}
