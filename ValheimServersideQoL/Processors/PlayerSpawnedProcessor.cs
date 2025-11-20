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
    bool _canMakeFriendly;
    bool _canLevelUp;
    bool _canTolerateLava;
    bool _modifyHpRegen;

    public bool SetsFedDuration => _modifyHpRegen;

    sealed class SpawnedState
    {
        public PlayerProcessor.IPeerInfo? Summoner { get; set; }
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

        _canMakeFriendly = Config.HostileSummons.MakeFriendly.Value ? true :
            Math.Min(Config.Skills.BloodmagicMakeHostileSummonsFriendlyMinChance.Value, Config.Skills.BloodmagicMakeHostileSummonsFriendlyMaxChance.Value) >= 0;

        _canLevelUp = Math.Min(Config.Skills.BloodmagicSummonsMinLevelUpChance.Value, Config.Skills.BloodmagicSummonsMaxLevelUpChance.Value) >= 0;
        _canTolerateLava = Math.Min(Config.Skills.BloodmagicMakeSummonsTolerateLavaMinChance.Value, Config.Skills.BloodmagicMakeSummonsTolerateLavaMaxChance.Value) >= 0;
        _modifyHpRegen =
            (Config.Skills.BloodmagicSummonsHPRegenMinMultiplier.Value, Config.Skills.BloodmagicSummonsHPRegenMaxMultiplier.Value)
            is { Item1: >= 0, Item2: >= 0 } and not { Item1: 1f, Item2: 1f };

        if (_spawnInfo.Count is 0)
        {
            if (Config.HostileSummons.AllowReplacementSummon.Value || _canMakeFriendly)
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
            Config.HostileSummons.AllowReplacementSummon.Value || _canMakeFriendly);

        if (!firstTime)
            return;

        _spawnedStates.Clear();
        _lastSummoningPlayer = null;
    }

    static void SortBySpawnTime(List<ExtendedZDO> list)
    {
        list.Sort(static (a, b) => Math.Sign(a.Vars.GetSpawnTime().Ticks - b.Vars.GetSpawnTime().Ticks));
    }

    void EvaluateChances(ExtendedZDO zdo, SpawnedState state, bool tamed)
    {
        if (state.ChancesEvaluated)
            return;
        state.ChancesEvaluated = true;
        if (state.Summoner is null)
            return;

        var randomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(zdo.Vars.GetSeed());
        try
        {
            float? skill = null;
            if (!tamed)
            {
                int makeFriendlyChance = -1;
                if (Config.HostileSummons.MakeFriendly.Value)
                    makeFriendlyChance = 100;
                else if (_canMakeFriendly)
                {
                    if (skill is null)
                    {
                        skill = state.Summoner.GetEstimatedSkillLevel(SkillType.BloodMagic);
                        if (float.IsNaN(skill.Value))
                            skill = 0;
                    }
                    makeFriendlyChance = Mathf.RoundToInt(Utils.Lerp(Config.Skills.BloodmagicMakeHostileSummonsFriendlyMinChance.Value, Config.Skills.BloodmagicMakeHostileSummonsFriendlyMaxChance.Value, skill.Value));
                }
                if (makeFriendlyChance >= 0 && (makeFriendlyChance is 100 || UnityEngine.Random.Range(0, 100) <= makeFriendlyChance))
                {
                    UnregisterZdoProcessor = false;
                    RPC.SetTamed(zdo, true);
                    zdo.Vars.SetTamed(true);
                }
            }

            if (_canLevelUp)
            {
                if (skill is null)
                {
                    skill = state.Summoner.GetEstimatedSkillLevel(SkillType.BloodMagic);
                    if (float.IsNaN(skill.Value))
                        skill = 0;
                }
                var levelUpChance = Mathf.RoundToInt(Utils.Lerp(Config.Skills.BloodmagicSummonsMinLevelUpChance.Value, Config.Skills.BloodmagicSummonsMaxLevelUpChance.Value, skill.Value));

                var level = 1;
                while (level < Config.Skills.BloodmagicSummonsMaxLevel.Value && UnityEngine.Random.Range(0f, 100f) <= levelUpChance)
                    level++;
                if (level != zdo.Vars.GetLevel())
                {
                    zdo.Vars.SetLevel(level);
                    RecreateZdo = true;
                }
            }

            if (_canTolerateLava)
            {
                if (skill is null)
                {
                    skill = state.Summoner.GetEstimatedSkillLevel(SkillType.BloodMagic);
                    if (float.IsNaN(skill.Value))
                        skill = 0;
                }
                var tolerateLavaChance = Mathf.RoundToInt(Utils.Lerp(Config.Skills.BloodmagicMakeSummonsTolerateLavaMinChance.Value, Config.Skills.BloodmagicMakeSummonsTolerateLavaMaxChance.Value, skill.Value));
                
                if (zdo.PrefabInfo.Humanoid is { Humanoid.m_tolerateFire: false } &&
                    zdo.Fields<Humanoid>().UpdateValue(static () => x => x.m_tolerateFire, UnityEngine.Random.Range(0, 100) <= tolerateLavaChance))
                {
                    RecreateZdo = true;
                }
            }

            if (_modifyHpRegen)
            {
                if (skill is null)
                {
                    skill = state.Summoner.GetEstimatedSkillLevel(SkillType.BloodMagic);
                    if (float.IsNaN(skill.Value))
                        skill = 0;
                }
                var hpRegenMultiplier = Utils.Lerp(Config.Skills.BloodmagicSummonsHPRegenMinMultiplier.Value, Config.Skills.BloodmagicSummonsHPRegenMaxMultiplier.Value, skill.Value);

                if (zdo.Fields<Humanoid>().UpdateValue(static () => x => x.m_regenAllHPTime, zdo.PrefabInfo.Humanoid!.Value.Humanoid.m_regenAllHPTime / hpRegenMultiplier))
                    RecreateZdo = true;
                if (zdo.PrefabInfo.Tameable is not null && zdo.Fields<Tameable>().UpdateValue(static () => x => x.m_fedDuration, float.PositiveInfinity))
                    RecreateZdo = true;
            }
        }
        finally { UnityEngine.Random.state = randomState; }
    }

    /// <see cref="ZSyncAnimation.SetTrigger(string)"/>
    void OnZSyncAnimationSetTrigger(ZRoutedRpc.RoutedRPCData data, string name)
    {
        if (!_spawnInfo.TryGetValue(name, out var spawnInfo) || (_lastSummoningPlayer = Instance<PlayerProcessor>().GetPeerInfo(data.m_senderPeerID)) is null)
            return;

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

        if (state.Summoner is null)
        {
            var playerName = zdo.Vars.GetFollow();
            if (!string.IsNullOrEmpty(playerName))
                state.Summoner = Instance<PlayerProcessor>().PeerInfos.FirstOrDefault(x => x.PlayerName == playerName);
            state.Summoner ??= _lastSummoningPlayer;
            if (state.Summoner is not null && string.IsNullOrEmpty(playerName))
                zdo.Vars.SetFollow(playerName = state.Summoner.PlayerName);
        }

        var tamed = zdo.Vars.GetTamed();

        EvaluateChances(zdo, state, tamed);

        var follow = Config.HostileSummons.FollowSummoner.Value || (_canMakeFriendly && tamed);
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
            if (state.Summoner is not null &&
                DateTimeOffset.UtcNow is { } now && now > state.NextPatrolPointUpdate &&
                Utils.DistanceXZ(zdo.GetPosition(), state.Summoner.PlayerZDO.GetPosition()) > cfg.MaxDistance / 2)
            {
                state.NextPatrolPointUpdate = now.AddSeconds(cfg.MoveInterval / 2);
                var rev = zdo.DataRevision;
                zdo.Vars.SetSpawnPoint(state.Summoner.PlayerZDO.GetPosition());
                zdo.Vars.SetPatrol(true);
                zdo.Vars.SetPatrolPoint(state.Summoner.PlayerZDO.GetPosition());
                if (rev != zdo.DataRevision) // values changed
                    zdo.DataRevision += 100;
            }
        }

        return false;
    }
}
