using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class ShieldGeneratorProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly ConcurrentHashSet<ExtendedZDO> _shieldGenerators = new();
    public IReadOnlyCollection<ExtendedZDO> ShieldGenerators => _shieldGenerators;

    public override void Initialize()
    {
        base.Initialize();
        RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _shieldGenerators.Remove(zdo);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.ShieldGenerator is not null)
            _shieldGenerators.Add(zdo);
        return false;
    }
}
