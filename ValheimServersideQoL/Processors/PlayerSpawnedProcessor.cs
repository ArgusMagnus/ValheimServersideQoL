namespace Valheim.ServersideQoL.Processors;

sealed class PlayerSpawnedProcessor : Processor
{
    sealed record SpawnInfo(int MaxSpawned, string MaxSummonReached, Dictionary<int, List<ExtendedZDO>> SpawnedByPrefab);

    readonly Dictionary<string, SpawnInfo> _spawnInfo = [];
    readonly Dictionary<int, List<ExtendedZDO>> _spawnedByPrefab = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        if (Config.Summons.AllowReplacementSummon.Value && _spawnInfo.Count is 0)
        {
            foreach (var item in ObjectDB.instance.m_items.Select(x => x.GetComponent<ItemDrop>()))
            {
                var attack = item.m_itemData.m_shared.m_attack;
                if (attack.m_attackProjectile?.GetComponent<SpawnAbility>() is not { } spawnAbility)
                    continue;

                Dictionary<int, List<ExtendedZDO>> dict = [];
                foreach (var prefab in spawnAbility.m_spawnPrefab)
                {
                    if (prefab.GetComponent<Tameable>() is not null || prefab.GetComponent<Humanoid>() is not { m_faction: Character.Faction.PlayerSpawned })
                        continue;
                    var hash = prefab.name.GetStableHashCode();
                    if (!_spawnedByPrefab.TryGetValue(hash, out var list))
                        _spawnedByPrefab.Add(hash, list = []);
                    dict.Add(hash, list);
                }
                if (dict.Count > 0)
                    _spawnInfo.Add(attack.m_attackAnimation, new(spawnAbility.m_maxSpawned, spawnAbility.m_maxSummonReached, dict));
            }

            foreach (var zdo in ZDOMan.instance.GetObjectsByID().Values.Cast<ExtendedZDO>())
            {
                if (!_spawnedByPrefab.TryGetValue(zdo.GetPrefab(), out var list) || list.Contains(zdo))
                    continue;
                list.Add(zdo);
                zdo.Destroyed += x => list.Remove(x);
            }
        }

        UpdateRpcSubscription("SetTrigger", OnZSyncAnimationSetTrigger, Config.Summons.AllowReplacementSummon.Value);
    }

    /// <see cref="ZSyncAnimation.SetTrigger(string)"/>
    void OnZSyncAnimationSetTrigger(ZRoutedRpc.RoutedRPCData data, string name)
    {
        if (!_spawnInfo.TryGetValue(name, out var spawnInfo))
            return;

        var playerZDOID = data.m_targetZDO;

        foreach (var list in spawnInfo.SpawnedByPrefab.Values)
        {
            if (list.Count < spawnInfo.MaxSpawned)
                continue;

            RPC.Damage(list[0], new(float.MaxValue) { m_attacker = playerZDOID });
            RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, spawnInfo.MaxSummonReached);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Humanoid is not { Humanoid.m_faction: Character.Faction.PlayerSpawned })
            return false;

        if (zdo.PrefabInfo.Tameable is null /*&& Config.Summons.AllowReplacementSummon.Value*/)
        {
            if (!_spawnedByPrefab.TryGetValue(zdo.GetPrefab(), out var list))
                _spawnedByPrefab.Add(zdo.GetPrefab(), list = []);
            if (!list.Contains(zdo))
            {
                list.Add(zdo);
                zdo.Destroyed += x => list.Remove(x);
            }
        }

        if (Config.Summons.MakeFriendly.Value && !zdo.Vars.GetTamed())
        {
            UnregisterZdoProcessor = false;
            RPC.SetTamed(zdo, true);
            return true;
        }

        return false;
    }
}
