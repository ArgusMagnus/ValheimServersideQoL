using UnityEngine;
using static Skills;

namespace Valheim.ServersideQoL.Processors;

sealed class PlayerSpawnedProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("7766ee34-0ade-4f71-8e6e-5931419cc303");

    sealed record SpawnInfo(int MaxSpawned, string MaxSummonReached, Dictionary<int, List<ExtendedZDO>> SpawnedByPrefab);

    readonly Dictionary<string, SpawnInfo> _spawnInfo = [];
    readonly Dictionary<int, List<ExtendedZDO>> _spawnedByPrefab = [];
    readonly Dictionary<ExtendedZDO, SpawnedState> _spawnedStates = [];
    PlayerProcessor.IPeerInfo? _lastSummoningPlayer;
    int _makeFriendlyChance;
    int _levelUpChance;
    int _tolerateLavaChance;

    sealed class SpawnedState
    {
        public ExtendedZDO? FollowTarget { get; set; }
        public DateTimeOffset NextPatrolPointUpdate { get; set; }
        public bool ChancesEvaluated { get; set; }
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        if (firstTime)
        {
            foreach (var list in _spawnedByPrefab.Values)
                list.Clear();
        }

        _makeFriendlyChance = Config.HostileSummons.MakeFriendly.Value ? 100 :
            Math.Min(Config.Skills.BloodmagicMakeHostileSummonsFriendlyMinChance.Value, Config.Skills.BloodmagicMakeHostileSummonsFriendlyMaxChance.Value);

        _levelUpChance = Math.Min(Config.Skills.BloodmagicSummonsMinLevelUpChance.Value, Config.Skills.BloodmagicSummonsMaxLevelUpChance.Value);
        _tolerateLavaChance = Math.Min(Config.Skills.BloodmagicMakeSummonsTolerateLavaMinChance.Value, Config.Skills.BloodmagicMakeSummonsTolerateLavaMaxChance.Value);

        if (_spawnInfo.Count is 0)
        {
            if (Config.HostileSummons.AllowReplacementSummon.Value || _makeFriendlyChance > -1)
            {
                foreach (var item in ObjectDB.instance.m_items.Select(static x => x.GetComponent<ItemDrop>()))
                {
                    var attack = item.m_itemData.m_shared.m_attack;
                    if (attack.m_attackProjectile?.GetComponent<SpawnAbility>() is not { } spawnAbility)
                        continue;

                    Dictionary<int, List<ExtendedZDO>> dict = [];
                    foreach (var prefab in spawnAbility.m_spawnPrefab)
                    {
                        if (prefab.GetComponent<Humanoid>() is not { m_faction: Character.Faction.Players or Character.Faction.PlayerSpawned } humanoid)
                            continue;

                        var hash = prefab.name.GetStableHashCode();
                        if (!_spawnedByPrefab.TryGetValue(hash, out var list))
                            _spawnedByPrefab.Add(hash, list = []);
                        dict.Add(hash, list);
                    }
                    if (dict.Count > 0)
                        _spawnInfo.Add(attack.m_attackAnimation, new(spawnAbility.m_maxSpawned, spawnAbility.m_maxSummonReached, dict));
                }

                foreach (ExtendedZDO zdo in ZDOMan.instance.GetObjects())
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
            Config.HostileSummons.AllowReplacementSummon.Value || _makeFriendlyChance > -1);

        if (!firstTime)
            return;

        _spawnedStates.Clear();
        _lastSummoningPlayer = null;
    }

    static void SortBySpawnTime(List<ExtendedZDO> list)
    {
        list.Sort(static (a, b) => Math.Sign(a.Vars.GetSpawnTime().Ticks - b.Vars.GetSpawnTime().Ticks));
    }

    /// <see cref="ZSyncAnimation.SetTrigger(string)"/>
    void OnZSyncAnimationSetTrigger(ZRoutedRpc.RoutedRPCData data, string name)
    {
        if (!_spawnInfo.TryGetValue(name, out var spawnInfo) || (_lastSummoningPlayer = Instance<PlayerProcessor>().GetPeerInfo(data.m_senderPeerID)) is null)
            return;

        float? skill = null;
        if (!Config.HostileSummons.MakeFriendly.Value && _makeFriendlyChance > -1)
        {
            if (skill is null)
            {
                skill = _lastSummoningPlayer.GetEstimatedSkillLevel(SkillType.BloodMagic);
                if (float.IsNaN(skill.Value))
                    skill = 0;
            }
            _makeFriendlyChance = Mathf.RoundToInt(Utils.Lerp(Config.Skills.BloodmagicMakeHostileSummonsFriendlyMinChance.Value, Config.Skills.BloodmagicMakeHostileSummonsFriendlyMaxChance.Value, skill.Value));
        }

        if (_levelUpChance > -1)
        {
            if (skill is null)
            {
                skill = _lastSummoningPlayer.GetEstimatedSkillLevel(SkillType.BloodMagic);
                if (float.IsNaN(skill.Value))
                    skill = 0;
            }
            _levelUpChance = Mathf.RoundToInt(Utils.Lerp(Config.Skills.BloodmagicSummonsMinLevelUpChance.Value, Config.Skills.BloodmagicSummonsMaxLevelUpChance.Value, skill.Value));
        }

        if (_tolerateLavaChance > -1)
        {
            if (skill is null)
            {
                skill = _lastSummoningPlayer.GetEstimatedSkillLevel(SkillType.BloodMagic);
                if (float.IsNaN(skill.Value))
                    skill = 0;
            }
            _tolerateLavaChance = Mathf.RoundToInt(Utils.Lerp(Config.Skills.BloodmagicMakeSummonsTolerateLavaMinChance.Value, Config.Skills.BloodmagicMakeSummonsTolerateLavaMaxChance.Value, skill.Value));
        }

        if (!Config.HostileSummons.AllowReplacementSummon.Value)
            return;

        foreach (var list in spawnInfo.SpawnedByPrefab.Values)
        {
            if (list.Count < spawnInfo.MaxSpawned)
                continue;

            if (list[0].GetOwner() == data.m_senderPeerID &&
                ZNetScene.InActiveArea(list[0].GetSector(), _lastSummoningPlayer.PlayerZDO.GetSector()))
            {
                RPC.Damage(list[0], new(float.MaxValue) { m_attacker = _lastSummoningPlayer.PlayerZDO.m_uid });
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
        if (zdo.PrefabInfo.Humanoid is not { Humanoid.m_faction: Character.Faction.Players or Character.Faction.PlayerSpawned })
            return false;

        if (!_spawnedStates.TryGetValue(zdo, out var state))
        {
            _spawnedStates.Add(zdo, state = new());
            zdo.Destroyed += x => _spawnedStates.Remove(x);

            if (zdo.PrefabInfo.Tameable is null)
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

        if ((Config.HostileSummons.FollowSummoner.Value || _makeFriendlyChance > -1) && state.FollowTarget is null)
        {
            var playerName = zdo.Vars.GetFollow();
            if (!string.IsNullOrEmpty(playerName))
                state.FollowTarget = Instance<PlayerProcessor>().Players.Values.FirstOrDefault(x => x.Vars.GetPlayerName() == playerName);
            state.FollowTarget ??= _lastSummoningPlayer?.PlayerZDO;
            if (state.FollowTarget is not null && string.IsNullOrEmpty(playerName))
                zdo.Vars.SetFollow(playerName = state.FollowTarget.Vars.GetPlayerName());
        }

        var tamed = zdo.Vars.GetTamed();

        if (!state.ChancesEvaluated)
        {
            state.ChancesEvaluated = true;
            var randomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(zdo.Vars.GetSeed());
            try
            {
                if (_makeFriendlyChance > -1 && !tamed)
                {
                    if (_makeFriendlyChance < 100 && UnityEngine.Random.Range(0, 100) > _makeFriendlyChance)
                        return false;

                    UnregisterZdoProcessor = false;
                    RPC.SetTamed(zdo, true);
                    zdo.Vars.SetTamed(true);
                }

                if (_levelUpChance > -1)
                {
                    var level = 1;
                    while (level < Config.Skills.BloodmagicSummonsMaxLevel.Value && UnityEngine.Random.Range(0f, 100f) <= _levelUpChance)
                        level++;
                    if (level != zdo.Vars.GetLevel())
                    {
                        zdo.Vars.SetLevel(level);
                        RecreateZdo = true;
                    }
                }

                if (_tolerateLavaChance > -1 && zdo.PrefabInfo.Humanoid is { Humanoid.m_tolerateFire: false } &&
                    zdo.Fields<Humanoid>().UpdateValue(static () => x => x.m_tolerateFire, UnityEngine.Random.Range(0, 100) <= _tolerateLavaChance))
                {
                    RecreateZdo = true;
                }
            }
            finally { UnityEngine.Random.state = randomState; }
        }

        var follow = Config.HostileSummons.FollowSummoner.Value || (_makeFriendlyChance > -1 && tamed);
        if (follow && zdo.PrefabInfo.Tameable is null)
        {
            var cfg = Config.Advanced.HostileSummons.FollowSummoners;
            var fields = zdo.Fields<MonsterAI>();
            if (fields.UpdateValue(static () => x => x.m_randomMoveInterval, cfg.MoveInterval))
                RecreateZdo = true;
            if (fields.UpdateValue(static () => x => x.m_randomMoveRange, cfg.MaxDistance))
                RecreateZdo = true;
            if (RecreateZdo)
                return false;

            UnregisterZdoProcessor = false;
            if (state.FollowTarget is not null &&
                DateTimeOffset.UtcNow is { } now && now > state.NextPatrolPointUpdate &&
                Utils.DistanceXZ(zdo.GetPosition(), state.FollowTarget.GetPosition()) > cfg.MaxDistance / 2)
            {
                state.NextPatrolPointUpdate = now.AddSeconds(cfg.MoveInterval / 2);
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
