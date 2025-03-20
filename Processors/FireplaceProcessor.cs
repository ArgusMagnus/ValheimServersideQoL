using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class FireplaceProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
	protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        if (zdo.PrefabInfo.Fireplace is null || !(Config.Fireplaces.MakeToggleable.Value || Config.Fireplaces.InfiniteFuel.Value))
            return false;

        var fields = zdo.Fields<Fireplace>();
        if (
            fields.GetBool(x => x.m_canTurnOff) == Config.Fireplaces.MakeToggleable.Value &&
            fields.GetFloat(x => x.m_secPerFuel) == (Config.Fireplaces.InfiniteFuel.Value ? 0 : zdo.PrefabInfo.Fireplace.m_secPerFuel) &&
            fields.GetBool(x => x.m_canRefill) == !Config.Fireplaces.InfiniteFuel.Value)
        {
            return true;
        }

        fields
            .Set(x => x.m_canTurnOff, Config.Fireplaces.MakeToggleable.Value)
            .Set(x => x.m_canRefill, !Config.Fireplaces.InfiniteFuel.Value);
        //.Set(x => x.m_infiniteFuel, Config.Fireplaces.InfiniteFuel.Value) // works, but removes the turn on/off hover text (turning on/off still works)
        if (Config.Fireplaces.InfiniteFuel.Value)
        {
            fields.Set(x => x.m_secPerFuel, 0);
            zdo.Set(ZDOVars.s_fuel, fields.GetFloat(x => x.m_maxFuel));
        }
        else
        {
            fields.Reset(x => x.m_secPerFuel);
        }

        recreate = true;
        return true;
    }
}
