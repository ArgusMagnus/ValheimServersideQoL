﻿using BepInEx.Logging;
using System.Collections.Concurrent;

namespace Valheim.ServersideQoL.Processors;

sealed class TraderProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly ConcurrentDictionary<Trader, List<string>> _globalKeysToSet = [];
    readonly ConcurrentHashSet<ZNetPeer> _reset = [];

    public override void Initialize()
    {
        base.Initialize();
        _globalKeysToSet.Clear();
        foreach(var (trader, cfgList) in Config.Traders.AlwaysUnlock.Select(x => (x.Key, x.Value)))
        {
            var list = cfgList.Where(x => x.ConfigEntry.Value).Select(x => x.GlobalKey).ToList();
            _globalKeysToSet.TryAdd(trader, list);
        }

        List<ZNetPeer>? remove = null;
        foreach (var peer in _reset)
        {
            if (!peer.m_socket.IsConnected())
                (remove ??= []).Add(peer);
        }

        if (remove is not null)
        {
            foreach (var peer in remove)
                _reset.Remove(peer);
        }
    }

    protected override async ValueTask<bool> ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
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
                if (_reset.Add(peer))
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
            else if (_reset.Remove(peer))
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
                _globalKeysToSet.TryRemove(zdo.PrefabInfo.Trader, out _);
        }

        return false;
    }
}
