namespace Valheim.ServersideQoL.Processors;

sealed class ShieldGeneratorProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("0ccc72d2-cddb-4007-a206-61cea0308af3");

    readonly Dictionary<ExtendedZDO, bool> _shieldGenerators = [];

    public readonly record struct ShieldGeneratorInfo(ExtendedZDO ShieldGenerator, bool HasFuel);
    IReadOnlyList<ShieldGeneratorInfo>? _info;
    public IReadOnlyList<ShieldGeneratorInfo> ShieldGenerators => _info ??= [.. _shieldGenerators.Select(static x => new ShieldGeneratorInfo(x.Key, x.Value))];

    public delegate void ShieldGeneratorChangedHandler(ExtendedZDO shieldGenerator, bool hasFuel);
    public event ShieldGeneratorChangedHandler? ShieldGeneratorChanged;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        if (!firstTime)
            return;

        _shieldGenerators.Clear();
        _info = null;
    }

    void OnShieldGeneratorDestroyed(ExtendedZDO zdo)
    {
        if (_shieldGenerators.Remove(zdo))
            _info = null;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (zdo.PrefabInfo.ShieldGenerator is null)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        var hasFuel = zdo.Vars.GetFuel() > 0;
        if (!_shieldGenerators.TryGetValue(zdo, out var oldFuel) || hasFuel != oldFuel)
        {
            if (!_shieldGenerators.ContainsKey(zdo))
                zdo.Destroyed += OnShieldGeneratorDestroyed;
            _shieldGenerators[zdo] = hasFuel;
            _info = null;
            ShieldGeneratorChanged?.Invoke(zdo, hasFuel);
        }

        return true;
    }
}
