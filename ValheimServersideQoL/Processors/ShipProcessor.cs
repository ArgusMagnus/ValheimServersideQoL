using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class ShipProcessor : Processor
{
    readonly HashSet<ExtendedZDO> _ships = [];
    public IReadOnlyCollection<ExtendedZDO> Ships => _ships;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        if (!firstTime)
            return;

        _ships.Clear();
        foreach (var zdo in ZDOMan.instance.GetObjectsByID().Values.Cast<ExtendedZDO>().Where(static x => x.PrefabInfo.Ship is not null))
        {
            _ships.Add(zdo);
            zdo.Destroyed += OnShipDestroyed;
        }
    }

    void OnShipDestroyed(ExtendedZDO zdo) => _ships.Remove(zdo);

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Ship is null)
            return false;
        _ships.Add(zdo);
        zdo.Destroyed += OnShipDestroyed;
        return false;
    }
}