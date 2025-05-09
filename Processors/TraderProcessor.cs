using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class TraderProcessor : Processor
{
    readonly Dictionary<Trader, List<string>> _globalKeysToSet = [];
    readonly Dictionary<ZDOID, Peer> _reset = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        _globalKeysToSet.Clear();
        foreach(var (trader, cfgList) in Config.Traders.AlwaysUnlock.Select(x => (x.Key, x.Value)))
        {
            var list = cfgList.Where(x => x.ConfigEntry.Value).Select(x => x.GlobalKey).ToList();
            if (list.Count > 0)
                _globalKeysToSet.Add(trader, list);
        }

        if (!firstTime)
            return;

        Instance<PlayerProcessor>().PlayerDestroyed += zdo => _reset.Remove(zdo.m_uid);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        if (zdo.PrefabInfo.Trader is null || !_globalKeysToSet.TryGetValue(zdo.PrefabInfo.Trader, out var globalKeysToSet))
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        float? minDistSqr = null;
        List<string>? serverKeys = null;
        List<string>? keys = null;
        List<string>? remove = null;
        foreach (var peer in peers)
        {
            minDistSqr ??= zdo.PrefabInfo.Trader.m_standRange * zdo.PrefabInfo.Trader.m_standRange;
            if (Utils.DistanceSqr(peer.m_refPos, zdo.GetPosition()) < minDistSqr)
            {
                if (_reset.TryAdd(peer.m_characterID, peer))
                {
                    if (keys is null)
                    {
                        keys = ZoneSystem.instance.GetGlobalKeys();
                        foreach (var key in globalKeysToSet)
                        {
                            if (keys.Contains(key))
                                (remove ??= []).Add(key);
                            else
                                keys.Add(key);
                        }
                    }
                    RPC.SendGlobalKeys(peer, keys);
                }
            }
            else if (_reset.Remove(peer.m_characterID))
            {
                serverKeys ??= ZoneSystem.instance.GetGlobalKeys();
                RPC.SendGlobalKeys(peer, serverKeys);
            }
        }

        if (remove is not null)
        {
            foreach (var key in remove)
                globalKeysToSet.Remove(key);
            if (globalKeysToSet.Count is 0)
                _globalKeysToSet.Remove(zdo.PrefabInfo.Trader);
        }

        return false;
    }
}
