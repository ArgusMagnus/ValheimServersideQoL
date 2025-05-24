using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class TrophyProcessor : Processor
{
    TimeSpan _activationDelay;
    TimeSpan _respawnDelay;
    readonly Dictionary<ExtendedZDO, TrophyState> _stateByTrophy = [];
    readonly TimeSpan _textDuration = TimeSpan.FromSeconds(DamageText.instance.m_textDuration * 2);
    readonly List<ZDO> _sectorZdos = [];
    //readonly Vector3 _dropOffset = new(0, -1000, 0);

    sealed class TrophyState(ExtendedZDO trophy, Character trophyCharacter)
    {
        public ExtendedZDO Trophy { get; } = trophy;
        public Character TrophyCharacter { get; } = trophyCharacter;
        public int CharacterPrefab { get; } = trophyCharacter.name.GetStableHashCode();
        public DateTimeOffset TrophySpawnTime { get; } = trophy.Vars.GetSpawnTime();
        public DateTimeOffset LastMessage { get; set; }
        public DateTimeOffset LastSpawned { get; set; } = trophy.Vars.GetLastSpawnedTime();
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        _activationDelay = TimeSpan.FromSeconds(Config.TrophySpawner.ActivationDelay.Value);
        _respawnDelay = TimeSpan.FromSeconds(Config.TrophySpawner.RespawnDelay.Value);
    }

    public bool IsAttracting(ExtendedZDO zdo)
        => _stateByTrophy.TryGetValue(zdo, out var state) && state.LastSpawned != default;

    void OnTrophyDestroyed(ExtendedZDO zdo)
    {
        _stateByTrophy.Remove(zdo);
    }

    bool ShouldAttemptSpawn(TrophyState state)
    {
        // skip one attempt if it's the first time this trophy is processed
        if (state.LastSpawned == default)
        {
            state.LastSpawned = DateTimeOffset.UtcNow - _respawnDelay + TimeSpan.FromSeconds(10); // retry after 10 seconds
            return false;
        }

        if (DateTimeOffset.UtcNow - state.LastSpawned < _respawnDelay)
            return false;

        state.LastSpawned = DateTimeOffset.UtcNow - _respawnDelay + TimeSpan.FromSeconds(10); // retry after 10 seconds

        var zone = ZoneSystem.GetZone(state.Trophy.GetPosition());
        _sectorZdos.Clear();
        ZDOMan.instance.FindSectorObjects(zone, ZoneSystem.instance.GetLoadedArea(), 0, _sectorZdos);
        var spawnLimitReached = _sectorZdos.Count(x => x.GetPrefab() == state.CharacterPrefab);
        _sectorZdos.Clear();
        if (spawnLimitReached < Config.TrophySpawner.SpawnLimit.Value)
            return true;
        Logger.DevLog($"{nameof(TrophyProcessor)}: Spawn limit reached ({state.TrophyCharacter.name})", BepInEx.Logging.LogLevel.Warning);
        return false;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        var itemDrop = zdo.PrefabInfo.ItemDrop?.ItemDrop;
        if (!Config.TrophySpawner.Enable.Value || itemDrop is null || Character.InInterior(zdo.GetPosition()) || !SharedProcessorState.CharacterByTrophy.TryGetValue(itemDrop.name, out var trophyCharacter))
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (!_stateByTrophy.TryGetValue(zdo, out var state))
        {
            _stateByTrophy.Add(zdo, state = new(zdo, trophyCharacter));
            zdo.Destroyed += OnTrophyDestroyed;
        }

        var progress = zdo.GetTimeSinceSpawned().TotalSeconds / _activationDelay.TotalSeconds;
        if (progress < 1)
        {
            if (DateTimeOffset.UtcNow - state.LastMessage > _textDuration)
            {
                state.LastMessage = DateTimeOffset.UtcNow;
                ShowMessage(peers, zdo, $"Attracting {trophyCharacter.m_name}... {progress:P0}", Config.TrophySpawner.MessageType.Value);
            }
            return false;
        }

        if (zdo.Fields<ItemDrop>().SetIfChanged(x => x.m_autoPickup, false))
        {
            RecreateZdo = true;
            return false;
        }

        if (ShouldAttemptSpawn(state))
        {
            var level = 1;
            var levelUpChance = Config.TrophySpawner.LevelUpChanceOverride.Value < 0 ? 0 : SpawnSystem.GetLevelUpChance(Config.TrophySpawner.LevelUpChanceOverride.Value);
            if (levelUpChance > 0)
            {
                for (; level < Config.TrophySpawner.MaxLevel.Value; level++)
                {
                    if (UnityEngine.Random.Range(0, 100) > levelUpChance)
                        break;
                }
            }

            var zone = ZoneSystem.GetZone(zdo.GetPosition());
            for (int i = 0; i < 10; i++)
            {
                // Search for farthest away position in the active area. Could be done with math, but this is good enough for now
                var step = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0) * (Vector3.forward * ZoneSystem.c_ZoneSize * ZoneSystem.instance.GetActiveArea());
                var pos = zdo.GetPosition() + step;
                step /= 8 * ZoneSystem.instance.GetActiveArea();
                var dstZone = ZoneSystem.GetZone(pos);
                while (true)
                {
                    var next = pos + step;
                    var z = ZoneSystem.GetZone(next);
                    if (!ZNetScene.InActiveArea(z, zone))
                        break;
                    pos = next;
                    dstZone = z;
                }
                Logger.DevLog($"Trophy zone: {zone}, spawned zone: {dstZone}");

                var spawn = true;

                pos.y = ZoneSystem.instance.GetGroundHeight(pos) + 12;
                if (pos.y < ZoneSystem.c_WaterLevel && !trophyCharacter.m_canSwim && !trophyCharacter.m_flying)
                    spawn = false;
                else
                {
                    // Check pos not in base (todo: optimize)
                    _sectorZdos.Clear();
                    ZDOMan.instance.FindSectorObjects(dstZone, 1, 0, _sectorZdos);
                    foreach (var sectorZdo in _sectorZdos.Cast<ExtendedZDO>())
                    {
                        if (sectorZdo.PrefabInfo.EffectArea is null || !sectorZdo.PrefabInfo.EffectArea.m_type.HasFlag(EffectArea.Type.PlayerBase))
                            continue;

                        // throws NRE
                        //var radius = sectorZdo.PrefabInfo.EffectArea.GetRadius();
                        const float radius = 20f;
                        if (Utils.DistanceXZ(sectorZdo.GetPosition(), pos) < radius)
                        {
                            spawn = false;
                            break;
                        }
                    }
                    _sectorZdos.Clear();
                }

                if (!spawn)
                {
                    Logger.DevLog($"{nameof(TrophyProcessor)}: Finding spawning position failed", BepInEx.Logging.LogLevel.Warning);
                    continue;
                }

                Logger.DevLog($"{nameof(TrophyProcessor)}: Spawning {trophyCharacter.name} at {pos} ({Mathf.Round(Vector3.Distance(pos, zdo.GetPosition()))}m away)");
                var mob = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, state.CharacterPrefab);
                mob.SetPrefab(state.CharacterPrefab);
                mob.Persistent = true;
                mob.Distant = false;
                mob.Type = ZDO.ObjectType.Default;
                mob.Vars.SetLevel(level);
                /// <see cref="BaseAI.IdleMovement"/>
                mob.Vars.SetSpawnPoint(zdo.GetPosition());
                //mob.Vars.SetPatrolPoint(zdo.GetPosition());
                //mob.Vars.SetPatrol(true);

                // Disabling drops like that doesn't work, since most mobs spawn a ragdoll (separate prefab created via m_deathEffects) which then spawn the drops
                //mob.Fields<CharacterDrop>().Set(x => x.m_spawnOffset, _dropOffset);

                zdo.Vars.SetLastSpawnedTime(state.LastSpawned = DateTimeOffset.UtcNow);
                break;
            }
        }

        if (DateTimeOffset.UtcNow - state.LastMessage > _textDuration)
        {
            state.LastMessage = DateTimeOffset.UtcNow;
            ShowMessage(peers, zdo, $"<color=yellow>Attracting {trophyCharacter.m_name}", Config.TrophySpawner.MessageType.Value);
        }

        return false;
    }
}
