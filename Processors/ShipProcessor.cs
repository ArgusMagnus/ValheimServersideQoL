using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class ShipProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ref ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Ship is not null)
            SharedProcessorState.Ships.Add(zdo.m_uid);
        return false;
    }
}