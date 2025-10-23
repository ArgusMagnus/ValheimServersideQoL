using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class ShipProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("822bb8e2-75a4-4519-b144-0b4f927ae6c2");

    readonly HashSet<ExtendedZDO> _ships = [];
    public IReadOnlyCollection<ExtendedZDO> Ships => _ships;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        if (!firstTime)
            return;

        _ships.Clear();
        foreach (ExtendedZDO zdo in ZDOMan.instance.GetObjects())
        {
            if (zdo.PrefabInfo.Ship is null)
                continue;
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