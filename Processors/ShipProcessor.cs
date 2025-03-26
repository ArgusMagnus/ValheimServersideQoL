using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class ShipProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        zdo.Unregister(this);
        if (zdo.PrefabInfo.Ship is not null)
            SharedProcessorState.Ships.Add(zdo);
        return false;
    }
}