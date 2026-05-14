namespace Valheim.ServersideQoL.Processors;

sealed class GlobalPlayerKeysProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("f21976ad-a2b6-4aaf-94d1-8f9e65510704");

    readonly record struct GlobalKeyModification(GlobalKey Key, bool Add);

    readonly Dictionary<Trader, IReadOnlyList<GlobalKeyModification>> _globalKeyModifications = [];
    readonly Dictionary<ZDOID, Peer> _reset = [];
    IReadOnlyList<GlobalKeyModification> _mapTableModifications => field ??= [new(new(GlobalKeys.NoMap), false)];
    float _mapTableRangeSqr;
    readonly List<GlobalKeyModification> _modifications = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        _globalKeyModifications.Clear();
        foreach (var (trader, cfgList) in Config.Traders.AlwaysUnlock.Select(static x => (x.Key, x.Value)))
        {
            var list = cfgList
                .Where(static x => x.ConfigEntry.Value)
                .Select(static x => x.GlobalKey)
                .Distinct()
                .Select(static x => new GlobalKeyModification(new(x), true))
                .ToList();
            if (list.Count > 0)
                _globalKeyModifications.Add(trader, list);
        }

        _mapTableRangeSqr = 0;
        if (Config.MapTables.MapViewDistance is not null)
        {
            _mapTableRangeSqr = Config.MapTables.MapViewDistance.Value * Config.MapTables.MapViewDistance.Value;
            if (_mapTableRangeSqr > 0 && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoMap))
            {
                _mapTableRangeSqr = 0;
                Logger.LogWarning($"[{Config.MapTables.MapViewDistance.Definition.Section}].[{Config.MapTables.MapViewDistance.Definition.Key}] has no effect unless the {GlobalKeys.NoMap} global key is set");
            }
        }

        if (!firstTime)
            return;

        _reset.Clear();
        Instance<PlayerProcessor>().PlayerDestroyed -= OnPlayerDestroyed;
        Instance<PlayerProcessor>().PlayerDestroyed += OnPlayerDestroyed;
    }

    void OnPlayerDestroyed(ExtendedZDO zdo) => _reset.Remove(zdo.m_uid);

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        float minDistSqr;
        if (zdo.PrefabInfo.Trader is not null && _globalKeyModifications.TryGetValue(zdo.PrefabInfo.Trader, out var globalKeyModifications))
            minDistSqr = zdo.PrefabInfo.Trader.m_standRange * zdo.PrefabInfo.Trader.m_standRange;
        else if (_mapTableRangeSqr > 0 && zdo.PrefabInfo.MapTable is not null)
        {
            minDistSqr = _mapTableRangeSqr;
            globalKeyModifications = _mapTableModifications;
        }
        else
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        foreach (var peer in peers.AsEnumerable())
        {
            if (peer.Info is not { } peerInfo)
                continue;

            if (Utils.DistanceSqr(peer.m_refPos, zdo.GetPosition()) < minDistSqr)
            {
                foreach (var (key, add) in globalKeyModifications)
                    peer.Info.AddGlobalKeyModification(key, add);
            }
            else
            {
                foreach (var (key, _) in globalKeyModifications)
                    peer.Info.RemoveGlobalKeyModification(key);
            }
        }

        return false;
    }
}
