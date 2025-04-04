using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class FireplaceProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
	protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Fireplace is null)
            return false;

        var fields = zdo.Fields<Fireplace>();
        if (!Config.Fireplaces.MakeToggleable.Value)
            fields.Reset(x => x.m_canTurnOff);
        else if (fields.SetIfChanged(x => x.m_canTurnOff, true))
            RecreateZdo = true;

        if (!Config.Fireplaces.InfiniteFuel.Value)
            fields.Reset(x => x.m_secPerFuel).Reset(x => x.m_canRefill);
        else
        {
            if (fields.SetIfChanged(x => x.m_secPerFuel, 0))
                RecreateZdo = true;
            if (fields.SetIfChanged(x => x.m_canRefill, false))
                RecreateZdo = true;
            zdo.Vars.SetFuel(fields.GetFloat(x => x.m_maxFuel));
        }

        if (!Config.Fireplaces.IgnoreRain.Value)
            fields.Reset(x => x.m_coverCheckOffset);
        else if (fields.SetIfChanged(x => x.m_coverCheckOffset, -25))
            RecreateZdo = true;
        
        return true;
    }
}
