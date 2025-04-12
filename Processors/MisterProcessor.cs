using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class MisterProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Mister is null)
            return false;

        var fields = zdo.Fields<Mister>();
        if (fields.SetOrReset(x => x.m_radius, Config.World.RemoveMistlandsMist.Value, float.MinValue))
            RecreateZdo = true;

        return false;
    }
}
