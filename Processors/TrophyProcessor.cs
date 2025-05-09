using BepInEx.Logging;
using System;
using UnityEngine;
using static ClutterSystem;
using static UnityEngine.Random;

namespace Valheim.ServersideQoL.Processors;

sealed class TrophyProcessor : Processor
{
    TimeSpan _activationDelay;
    readonly Dictionary<ExtendedZDO, TrophyState> _stateByTrophy = [];
    readonly Dictionary<ExtendedZDO, TrophyState> _stateBySpawned = [];
    readonly TimeSpan _textDuration = TimeSpan.FromSeconds(DamageText.instance.m_textDuration * 2);
    readonly List<ZDO> _sectorZdos = [];

    sealed class TrophyState(ExtendedZDO trophy, Character trophyCharacter)
    {
        public ExtendedZDO Trophy { get; } = trophy;
        public int CharacterPrefab { get; } = trophyCharacter.name.GetStableHashCode();
        public DateTimeOffset TrophySpawnTime { get; } = trophy.Vars.GetSpawnTime();
        public DateTimeOffset LastMessage { get; set; }
        public DateTimeOffset LastSpawned { get; set; } = trophy.Vars.GetLastSpawnedTime();
        public TimeSpan SpawnInterval { get; set; }
        public int MaxLevel { get; set; }
        public ExtendedZDO? Spawned { get; set; }
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        _activationDelay = TimeSpan.FromSeconds(Config.TrophySpawner.ActivationDelay.Value);
    }

    void SetupSpawned(ExtendedZDO zdo, TrophyState state)
    {
        state.Spawned = zdo;
        _stateBySpawned.Add(zdo, state);
        zdo.Recreated += OnZdoRecreated;
        zdo.Destroyed += OnSpawnedDestroyed;
    }

    void OnZdoRecreated(ExtendedZDO oldZdo, ExtendedZDO newZdo)
    {
        if (_stateBySpawned.Remove(oldZdo, out var state))
        {
            state.Spawned = newZdo;
            _stateBySpawned.Add(newZdo, state);
        }
    }

    void OnTrophyDestroyed(ExtendedZDO zdo)
    {
        if (_stateByTrophy.Remove(zdo, out var state) && state.Spawned is not null)
            _stateBySpawned.Remove(state.Spawned);
    }

    void OnSpawnedDestroyed(ExtendedZDO zdo)
    {
        if (_stateBySpawned.Remove(zdo, out var state))
            state.Spawned = null;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        var itemDrop = zdo.PrefabInfo.ItemDrop?.ItemDrop;
        if (!Config.TrophySpawner.Enable.Value || itemDrop is null || !SharedProcessorState.CharacterByTrophy.TryGetValue(itemDrop.m_itemData.m_shared, out var trophyCharacter))
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (!_stateByTrophy.TryGetValue(zdo, out var state))
        {
            _stateByTrophy.Add(zdo, state = new(zdo, trophyCharacter));
            var spawnedId = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);
            if (spawnedId != ZDOID.None)
            {
                state.Spawned = ZDOMan.instance.GetExtendedZDO(spawnedId);
                if (state.Spawned is not null)
                    SetupSpawned(state.Spawned, state);
            }
            zdo.Destroyed += OnTrophyDestroyed;
        }

        var progress = zdo.GetTimeSinceSpawned().TotalSeconds / _activationDelay.TotalSeconds;
        if (progress < 1)
        {
            if (DateTimeOffset.UtcNow - state.LastMessage > _textDuration)
            {
                state.LastMessage = DateTimeOffset.UtcNow;
                RPC.ShowInWorldText(peers, DamageText.TextType.Normal, zdo.GetPosition(), $"Attracting {trophyCharacter.m_name}... {progress:P0}");
            }
            return false;
        }

        if (state.MaxLevel is 0)
        {
            var item = new ItemDrop.ItemData { m_shared = itemDrop.m_itemData.m_shared };
            PrivateAccessor.LoadFromZDO(item, zdo);
            var f = (float)item.m_stack / item.m_shared.m_maxStackSize;
            state.MaxLevel = Mathf.RoundToInt(Utils.Lerp(1, Config.TrophySpawner.MaxLevel.Value, f));
            state.SpawnInterval = TimeSpan.FromSeconds(Utils.Lerp(Config.TrophySpawner.MaxRespawnDelay.Value, Config.TrophySpawner.MinRespawnDelay.Value, f));
        }

        if (state.Spawned is null && DateTimeOffset.UtcNow - state.LastSpawned > state.SpawnInterval)
        {

            var level = 1;
            var levelUpChance = Config.TrophySpawner.LevelUpChanceOverride.Value < 0 ? 0 : SpawnSystem.GetLevelUpChance(Config.TrophySpawner.LevelUpChanceOverride.Value);
            if (levelUpChance > 0)
            {
                for (; level < state.MaxLevel; level++)
                {
                    if (UnityEngine.Random.Range(0, 100) > levelUpChance)
                        break;
                }
            }

            var zone = ZoneSystem.GetZone(zdo.GetPosition());
            for (int i = 0; i < 10 && state.Spawned is null; i++)
            {
                // Search for farthest away position in the active area. Could be done with math, but this is good enough for now
                var direction = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0) * Vector3.forward;
                direction *= ZoneSystem.c_ZoneSize / 8;
                var pos = zdo.GetPosition();
                var dstZone = zone;
                while (dstZone == zone)
                    dstZone = ZoneSystem.GetZone(pos = pos + direction);
                Logger.LogInfo($"Trophy zone: {zone}, spawned zone: {dstZone}");
                while (true)
                {
                    var next = pos + direction;
                    if (ZoneSystem.GetZone(next) != dstZone)
                        break;
                    pos = next;
                }

                var spawn = true;

                // Check pos not in base (todo: optimize)
                ZDOMan.instance.FindSectorObjects(dstZone, 1, 0, _sectorZdos);
                foreach (var sectorZdo in _sectorZdos.Cast<ExtendedZDO>())
                {
                    if (sectorZdo.PrefabInfo.EffectArea is null || !sectorZdo.PrefabInfo.EffectArea.m_type.HasFlag(EffectArea.Type.PlayerBase))
                        continue;

                    var radius = sectorZdo.PrefabInfo.EffectArea.GetRadius();
                    Logger.LogInfo($"Effect area radius: {radius}");
                    if (Utils.DistanceXZ(sectorZdo.GetPosition(), pos) < radius)
                    {
                        Logger.LogWarning($"{nameof(TrophyProcessor)}: Finding spawning position failed");
                        spawn = false;
                        break;
                    }
                }
                _sectorZdos.Clear();

                if (!spawn)
                    continue;

                pos.y = ZoneSystem.instance.GetGroundHeight(pos) + 12;

                Logger.LogInfo($"{nameof(TrophyProcessor)}: Spawning {trophyCharacter.name} at {pos} ({Mathf.Round(Vector3.Distance(pos, zdo.GetPosition()))}m away)");
                state.Spawned = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, state.CharacterPrefab);
                state.Spawned.SetPrefab(state.CharacterPrefab);
                state.Spawned.Persistent = true;
                state.Spawned.Distant = false;
                state.Spawned.Type = ZDO.ObjectType.Default;
                state.Spawned.Vars.SetLevel(level);
                /// <see cref="BaseAI.IdleMovement"/>
                state.Spawned.Vars.SetSpawnPoint(zdo.GetPosition());
                //state.Spawned.Vars.SetPatrolPoint(zdo.GetPosition());
                //state.Spawned.Vars.SetPatrol(true);
                zdo.Vars.SetLastSpawnedTime(state.LastSpawned = DateTimeOffset.UtcNow);
                SetupSpawned(state.Spawned, state);
            }
        }

        if (DateTimeOffset.UtcNow - state.LastMessage > _textDuration)
        {
            state.LastMessage = DateTimeOffset.UtcNow;
            RPC.ShowInWorldText(peers, DamageText.TextType.Weak, zdo.GetPosition(), $"Attracting {trophyCharacter.m_name}");
        }

        return false;
    }
}
