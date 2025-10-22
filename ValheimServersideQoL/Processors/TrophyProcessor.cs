using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class TrophyProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("e320985c-d7c9-4922-b778-2efee903ed4d");

    TimeSpan _activationDelay;
    TimeSpan _respawnDelay;
    readonly Dictionary<ExtendedZDO, TrophyState> _stateByTrophy = [];
    readonly List<(int Prefab, Vector3 Pos, Vector2i Zone, DateTimeOffset DiscardAfter)> _expectedRagdolls = [];
    readonly TimeSpan _textDuration = TimeSpan.FromSeconds(DamageText.instance.m_textDuration * 2);
    readonly List<ZDO> _sectorZdos = [];
    readonly Vector3 _dropOffset = new(0, -1000, 0);
    const float MaxRagdollDistance = ZoneSystem.c_ZoneHalfSize;

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

        foreach (var zdo in ZDOMan.instance.GetObjectsByID().Values.Cast<ExtendedZDO>())
        {
            if (zdo.Vars.GetSpawnedByTrophy())
            {
                zdo.Destroyed -= OnSpawnedDestroyed;
                if (Config.TrophySpawner.SuppressDrops.Value)
                    zdo.Destroyed += OnSpawnedDestroyed;
            }
        }

        if (!firstTime)
            return;

        _activationDelay = TimeSpan.FromSeconds(Config.TrophySpawner.ActivationDelay.Value);
        _respawnDelay = TimeSpan.FromSeconds(Config.TrophySpawner.RespawnDelay.Value);
        _stateByTrophy.Clear();
        _expectedRagdolls.Clear();
        _sectorZdos.Clear();
    }

    public bool IsAttracting(ExtendedZDO zdo)
        => _stateByTrophy.TryGetValue(zdo, out var state) && state.LastSpawned != default;

    void OnTrophyDestroyed(ExtendedZDO zdo)
    {
        _stateByTrophy.Remove(zdo);
    }

    void OnSpawnedDestroyed(ExtendedZDO zdo)
    {
        var effectPrefabs = (zdo.PrefabInfo.Humanoid?.Humanoid ?? zdo.PrefabInfo.Character?.Character!).m_deathEffects.m_effectPrefabs;

        foreach (var effectPrefab in effectPrefabs)
        {
            if (!effectPrefab.m_enabled || effectPrefab.m_prefab.GetComponent<Ragdoll>() is not { } ragdollComponent)
                continue;

            var prefab = effectPrefab.m_prefab.name.GetStableHashCode();

            var zone = zdo.GetSector();
            _sectorZdos.Clear();
            ZDOMan.instance.FindSectorObjects(zone, 0, 0, _sectorZdos);

            if (GetClosestRagdoll(_sectorZdos, prefab, zdo.GetPosition()) is not { } ragdoll)
            {
                //Logger.DevLog($"Ragdoll {effectPrefab.m_prefab.name} not found", BepInEx.Logging.LogLevel.Error);
                var discardAfter = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(ragdollComponent.m_ttl);
                var i = _expectedRagdolls.FindLastIndex(x => x.Zone == zone);
                if (i < 0)
                    _expectedRagdolls.Add((prefab, zdo.GetPosition(), zone, discardAfter));
                else
                    _expectedRagdolls.Insert(i + 1, (prefab, zdo.GetPosition(), zone, discardAfter));
            }
            else
            {
                //Logger.DevLog($"Ragdoll {effectPrefab.m_prefab.name} found", BepInEx.Logging.LogLevel.Warning);
                //ragdoll.Set(ZDOVars.s_drops, 0);
                //ragdoll.DataRevision += 120;
                ragdoll.Destroy();
            }
            _sectorZdos.Clear();
            break;
        }
    }

    static ExtendedZDO? GetClosestRagdoll(IReadOnlyList<ZDO> zdos, int prefab, Vector3 pos)
    {
        return zdos.Cast<ExtendedZDO>()
                .Where(x => x.GetPrefab() == prefab)
                .Select(x => (ZDO: x, Distance: Utils.DistanceXZ(x.GetPosition(), pos)))
                .Where(static x => x.Distance <= MaxRagdollDistance)
                .OrderBy(static x => x.Distance)
                .FirstOrDefault().ZDO;
    }

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        if (_expectedRagdolls.Count is 0)
            return;

        Vector2i lastZone = new(int.MaxValue, int.MaxValue);
        for (int i = _expectedRagdolls.Count - 1; i >= 0; i--)
        {
            var (prefab, pos, zone, discardAfter) = _expectedRagdolls[i];
            if (discardAfter < DateTimeOffset.UtcNow)
            {
                Logger.DevLog($"Discarding expected ragdoll {prefab} at {pos} in zone {zone}", BepInEx.Logging.LogLevel.Error);
                _expectedRagdolls.RemoveAt(i);
                continue;
            }

            if (zone != lastZone)
            {
                lastZone = zone;
                _sectorZdos.Clear();
                ZDOMan.instance.FindSectorObjects(zone, 1, 0, _sectorZdos);
            }
            if (GetClosestRagdoll(_sectorZdos, prefab, pos) is { } ragdoll)
            {
                //Logger.DevLog($"Found expected ragdoll {prefab} at {ragdoll.GetPosition()}", BepInEx.Logging.LogLevel.Warning);
                //ragdoll.Set(ZDOVars.s_drops, 0);
                //ragdoll.DataRevision += 120;
                ragdoll.Destroy();
                _expectedRagdolls.RemoveAt(i);
                _sectorZdos.Remove(ragdoll);
            }
        }
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

        state.LastSpawned = DateTimeOffset.UtcNow - _respawnDelay + TimeSpan.FromSeconds(Math.Min(10, _respawnDelay.TotalSeconds)); // retry after 10 seconds

        var zone = state.Trophy.GetSector();
        _sectorZdos.Clear();
        ZDOMan.instance.FindSectorObjects(zone, ZoneSystem.instance.GetLoadedArea(), 0, _sectorZdos);
        var spawnLimitReached = _sectorZdos.Cast<ExtendedZDO>().Count(x => x.GetPrefab() == state.CharacterPrefab && x.Vars.GetSpawnedByTrophy());
        _sectorZdos.Clear();
        if (spawnLimitReached < Config.TrophySpawner.SpawnLimit.Value)
            return true;
        Logger.DevLog($"{nameof(TrophyProcessor)}: Spawn limit reached ({state.TrophyCharacter.name})", BepInEx.Logging.LogLevel.Warning);
        return false;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
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

        if (zdo.Fields<ItemDrop>().SetIfChanged(static x => x.m_autoPickup, false))
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

            var zone = zdo.GetSector();
            for (int i = 0; i < 10; i++)
            {
                // Search for farthest away position in the active area. Could be done with math, but this is good enough for now
                var minStep = Mathf.Min(
                    Config.TrophySpawner.MinSpawnDistance.Value,
                    ZoneSystem.c_ZoneSize * ZoneSystem.instance.GetActiveArea());

                var dir = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0) * Vector3.forward;

                var minPos = zdo.GetPosition() + (dir * minStep);
                var pos = minPos;

                const float step = ZoneSystem.c_ZoneSize / 32;
                var maxStepCount = Mathf.CeilToInt((Config.TrophySpawner.MaxSpawnDistance.Value - minStep) / step);
                int stepCount;
                for (stepCount = 0; stepCount < maxStepCount; stepCount++)
                {
                    var next = pos + (dir * step);
                    var z = ZoneSystem.GetZone(next);
                    if (!ZNetScene.InActiveArea(z, zone))
                    {
                        stepCount--;
                        break;
                    }
                    pos = next;
                }

                if (Config.TrophySpawner.MinSpawnDistance.Value < Config.TrophySpawner.MaxSpawnDistance.Value)
                    pos = minPos + dir * step * UnityEngine.Random.Range(0f, stepCount);

                var dstZone = ZoneSystem.GetZone(pos);
                Logger.DevLog($"Trophy zone: {zone}, spawned zone: {dstZone}");

                var spawn = true;

                pos.y = ZoneSystem.instance.GetGroundHeight(pos);
                if (pos.y < ZoneSystem.c_WaterLevel && !trophyCharacter.m_canSwim && !trophyCharacter.m_flying)
                    spawn = false;
                else
                {
                    pos.y += 8;

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

                Logger.DevLog($"{nameof(TrophyProcessor)}: Spawning {trophyCharacter.name} at {pos} ({Mathf.Round(Utils.DistanceXZ(pos, zdo.GetPosition()))}m away)");
                var mob = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, state.CharacterPrefab);
                mob.SetPrefab(state.CharacterPrefab);
                mob.Persistent = true;
                mob.Distant = false;
                mob.Type = ZDO.ObjectType.Default;
                mob.Vars.SetLevel(level);
                /// <see cref="BaseAI.IdleMovement"/>
                mob.Vars.SetSpawnPoint(zdo.GetPosition());
                mob.Vars.SetSpawnedByTrophy(true);

                if (Config.TrophySpawner.SuppressDrops.Value)
                {
                    // Disabling drops like that doesn't work, since most mobs spawn a ragdoll (separate prefab created via m_deathEffects) which then spawn the drops
                    if (mob.PrefabInfo is { Humanoid.CharacterDrop.Value: not null } or { Character.CharacterDrop.Value: not null } )
                        mob.Fields<CharacterDrop>().Set(static x => x.m_spawnOffset, _dropOffset);
                    mob.Destroyed += OnSpawnedDestroyed;
                }

                zdo.Vars.SetLastSpawnedTime(state.LastSpawned = DateTimeOffset.UtcNow);
                break;
            }
        }

        if (DateTimeOffset.UtcNow - state.LastMessage > _textDuration)
        {
            state.LastMessage = DateTimeOffset.UtcNow;
            ShowMessage(peers, zdo, $"Attracting {trophyCharacter.m_name}", Config.TrophySpawner.MessageType.Value, DamageText.TextType.Weak);
        }

        return false;
    }
}
