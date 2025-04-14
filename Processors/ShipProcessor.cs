using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class ShipProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    ConcurrentHashSet<ExtendedZDO>? _ships;
    public ConcurrentHashSet<ExtendedZDO> Ships => _ships!;

    public override void Initialize()
    {
        base.Initialize();
        if (_ships is not null)
            return;

        _ships = [];
        foreach (var zdo in ZDOMan.instance.GetObjectsByID().Values.Cast<ExtendedZDO>().Where(x => x.PrefabInfo.Ship is not null))
            _ships.Add(zdo);
        RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        Ships.Remove(zdo);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Ship is not null)
            Ships.Add(zdo);
        return false;
    }
}