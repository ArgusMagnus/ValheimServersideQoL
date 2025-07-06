namespace Valheim.ServersideQoL.Processors;

sealed class PlayerSpawnedProcessor : Processor
{
    sealed record SpawnInfo(int MaxSpawned, string MaxSummonReached, Dictionary<int, List<ExtendedZDO>> SpawnedByPrefab);

    readonly Dictionary<string, SpawnInfo> _spawnInfo = [];
    readonly Dictionary<int, List<ExtendedZDO>> _spawnedByPrefab = [];
    readonly Dictionary<ExtendedZDO, SpawnedState> _spawnedStates = [];
    ExtendedZDO? _lastSummoningPlayer;

    sealed class SpawnedState
    {
        public ExtendedZDO? FollowTarget { get; set; }
        public DateTimeOffset LastPatrolPointUpdate { get; set; }
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        if (firstTime)
        {
            foreach (var list in _spawnedByPrefab.Values)
                list.Clear();
        }

        if (_spawnInfo.Count is 0)
        {
            if (Config.HostileSummons.AllowReplacementSummon.Value || Config.HostileSummons.FollowSummoner.Value)
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

                foreach (var list in _spawnedByPrefab.Values)
                    SortBySpawnTime(list);
            }
        }

        UpdateRpcSubscription("SetTrigger", OnZSyncAnimationSetTrigger,
            Config.HostileSummons.AllowReplacementSummon.Value || Config.HostileSummons.FollowSummoner.Value);

        if (!firstTime)
            return;

        _spawnedStates.Clear();
        _lastSummoningPlayer = null;
    }

    static void SortBySpawnTime(List<ExtendedZDO> list)
    {
        list.Sort(static (a, b) => (int)(a.Vars.GetSpawnTime().Ticks - b.Vars.GetSpawnTime().Ticks));
    }

    /// <see cref="ZSyncAnimation.SetTrigger(string)"/>
    void OnZSyncAnimationSetTrigger(ZRoutedRpc.RoutedRPCData data, string name)
    {
        if (!_spawnInfo.TryGetValue(name, out var spawnInfo) || !Instance<PlayerProcessor>().Players.TryGetValue(data.m_targetZDO, out _lastSummoningPlayer))
            return;

        if (!Config.HostileSummons.AllowReplacementSummon.Value)
            return;

        foreach (var list in spawnInfo.SpawnedByPrefab.Values)
        {
            if (list.Count < spawnInfo.MaxSpawned)
                continue;

            if (list[0].GetOwner() == data.m_senderPeerID &&
                ZNetScene.InActiveArea(ZoneSystem.GetZone(list[0].GetPosition()), ZoneSystem.GetZone(_lastSummoningPlayer.GetPosition())))
            {
                RPC.Damage(list[0], new(float.MaxValue) { m_attacker = _lastSummoningPlayer.m_uid });
            }
            else
            {
                list[0].Destroy(); // does not show death animation, but is faster and therefore more reliable
            }
            RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, spawnInfo.MaxSummonReached);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Humanoid is not { Humanoid.m_faction: Character.Faction.PlayerSpawned } || zdo.PrefabInfo.Tameable is { Tameable.m_startsTamed: true })
            return false;

        if (!_spawnedStates.TryGetValue(zdo, out var state))
        {
            _spawnedStates.Add(zdo, state = new());
            zdo.Destroyed += x => _spawnedStates.Remove(x);

            if (zdo.PrefabInfo.Tameable is null /*&& Config.Summons.AllowReplacementSummon.Value*/)
            {
                if (!_spawnedByPrefab.TryGetValue(zdo.GetPrefab(), out var list))
                    _spawnedByPrefab.Add(zdo.GetPrefab(), list = []);
                if (!list.Contains(zdo))
                {
                    list.Add(zdo);
                    SortBySpawnTime(list);
                    zdo.Destroyed += x => list.Remove(x);
                }
            }
        }

        if (Config.HostileSummons.FollowSummoner.Value && state.FollowTarget is null)
        {
            var playerName = zdo.Vars.GetFollow();
            if (!string.IsNullOrEmpty(playerName))
                state.FollowTarget = Instance<PlayerProcessor>().Players.Values.FirstOrDefault(x => x.Vars.GetPlayerName() == playerName);
            state.FollowTarget ??= _lastSummoningPlayer;
            if (state.FollowTarget is not null && string.IsNullOrEmpty(playerName))
                zdo.Vars.SetFollow(playerName = state.FollowTarget.Vars.GetPlayerName());
        }

        if (Config.HostileSummons.MakeFriendly.Value && !zdo.Vars.GetTamed())
        {
            UnregisterZdoProcessor = false;
            RPC.SetTamed(zdo, true);
            return true;
        }

        if (Config.HostileSummons.FollowSummoner.Value && zdo.PrefabInfo.Tameable is null)
        {
            var cfg = Config.Advanced.HostileSummons.FollowSummoners;
            var fields = zdo.Fields<MonsterAI>();
            if (fields.SetIfChanged(x => x.m_randomMoveInterval, cfg.MoveInterval))
                RecreateZdo = true;
            if (fields.SetIfChanged(x => x.m_randomMoveRange, cfg.MaxDistance))
                RecreateZdo = true;
            if (RecreateZdo)
                return false;

            UnregisterZdoProcessor = false;
            if (state.FollowTarget is not null &&
                (DateTimeOffset.UtcNow - state.LastPatrolPointUpdate) > TimeSpan.FromSeconds(cfg.MoveInterval / 2) &&
                Utils.DistanceXZ(zdo.GetPosition(), state.FollowTarget.GetPosition()) > cfg.MaxDistance / 2)
            {
                state.LastPatrolPointUpdate = DateTimeOffset.UtcNow;
                var rev = zdo.DataRevision;
                zdo.Vars.SetSpawnPoint(state.FollowTarget.GetPosition());
                zdo.Vars.SetPatrol(true);
                zdo.Vars.SetPatrolPoint(state.FollowTarget.GetPosition());
                if (rev != zdo.DataRevision) // values changed
                    zdo.DataRevision += 100;
            }
        }

        return false;
    }
}
