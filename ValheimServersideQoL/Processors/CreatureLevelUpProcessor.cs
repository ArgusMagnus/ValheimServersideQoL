using UnityEngine;
using Valheim.ServersideQoL.HarmonyPatches;
using static Heightmap;

namespace Valheim.ServersideQoL.Processors;

[Processor(Priority = 1) /// Process before <see cref="HumanoidLevelProcessor"/>
]
sealed class CreatureLevelUpProcessor : Processor
{
    readonly Dictionary<Biome, int> _levelIncreasePerBiome = [];
    readonly Dictionary<Vector2i, SectorState> _sectorStates = [];
    readonly Dictionary<(Biome, BiomeArea, int Prefab), List<SpawnSystemData>> _spawnData = [];

    abstract record SpawnData(int MinLevel, int MaxLevel, float LevelUpChance);

    sealed record SpawnSystemData(SpawnSystem.SpawnData Data) : SpawnData(Data.m_minLevel, Data.m_maxLevel, Data.m_overrideLevelupChance);

    sealed class SectorState
    {
        public Dictionary<int, List<ExtendedZDO>> CreatureSpawnersBySpawned { get; } = [];
        public Dictionary<int, List<SpawnAreaData>> SpawnAreasBySpawned { get; } = [];

        public sealed record SpawnAreaData(ZDOID ID, Vector3 Position, float Radius, int MinLevel, int MaxLevel, float LevelUpChance)
            : SpawnData(MinLevel, MaxLevel, LevelUpChance);
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        ZoneSystemSendGlobalKeys.GlobalKeysChanged -= InitializeData;
        if (Config.Creatures.MaxLevelChance.Value > 0 || Config.Creatures.MaxLevelIncrease.Value > 0 || Config.Creatures.MaxLevelIncreasePerDefeatedBoss.Value > 0)
        {
            InitializeData();
            ZoneSystemSendGlobalKeys.GlobalKeysChanged += InitializeData;
        }

        if (!firstTime)
            return;

        _sectorStates.Clear();
    }

    void InitializeData()
    {
        _levelIncreasePerBiome.Clear();
        if (Config.Creatures.MaxLevelIncreasePerDefeatedBoss.Value > 0)
        {
            var increase = 0;
            foreach (var (biome, boss) in SharedProcessorState.BossesByBiome.OrderByDescending(static x => x.Value.m_health))
            {
                if (ZoneSystem.instance.GetGlobalKey(boss.m_defeatSetGlobalKey))
                    increase += Config.Creatures.MaxLevelIncreasePerDefeatedBoss.Value;
                _levelIncreasePerBiome.Add(biome, increase);
            }
            if (_levelIncreasePerBiome.TryGetValue(Config.Creatures.TreatOceanAs.Value, out var oceanIncrease))
                _levelIncreasePerBiome.Add(Biome.Ocean, oceanIncrease);
        }

        _spawnData.Clear();
        foreach (var spawner in ZoneSystem.instance.m_zoneCtrlPrefab.GetComponent<SpawnSystem>().m_spawnLists.SelectMany(static x => x.m_spawners))
        {
            if (!spawner.m_enabled)
                continue;

            if (!string.IsNullOrEmpty(spawner.m_requiredGlobalKey) && !ZoneSystem.instance.GetGlobalKey(spawner.m_requiredGlobalKey))
                continue;

            foreach (var biome in ModConfig.AcceptableEnum<Biome>.Default.AcceptableValues)
            {
                if (!spawner.m_biome.HasFlag(biome))
                    continue;

                foreach (var biomeArea in ModConfig.AcceptableEnum<BiomeArea>.Default.AcceptableValues)
                {
                    if (!spawner.m_biomeArea.HasFlag(biomeArea))
                        continue;

                    var prefab = spawner.m_prefab.name.GetStableHashCode();
                    if (!_spawnData.TryGetValue((biome, biomeArea, prefab), out var list))
                        _spawnData.Add((biome, biomeArea, prefab), list = []);
                    list.Add(new(spawner));
                }
            }
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo is { CreatureSpawner: null } and { SpawnArea: null } and { Humanoid: null } or { Humanoid.Humanoid.m_faction: Character.Faction.PlayerSpawned })
            return false;

        switch (zdo.PrefabInfo)
        {
            case { CreatureSpawner: not null }:
                LevelUpSpawner(zdo);
                if (!RecreateZdo)
                {
                    var sector = ZoneSystem.GetZone(zdo.GetPosition());
                    if (!_sectorStates.TryGetValue(sector, out var state))
                        _sectorStates.Add(sector, state = new());

                    var prefab = zdo.PrefabInfo.CreatureSpawner.m_creaturePrefab.name.GetStableHashCode();
                    if (!state.CreatureSpawnersBySpawned.TryGetValue(prefab, out var list))
                        state.CreatureSpawnersBySpawned.Add(prefab, list = []);

                    if (!list.Contains(zdo))
                    {
                        list.Add(zdo);
                        zdo.Destroyed += x => list.Remove(x);
                    }
                }
                break;

            case { SpawnArea: not null }:
                var minZone = ZoneSystem.GetZone(zdo.GetPosition() - new Vector3(zdo.PrefabInfo.SpawnArea.m_spawnRadius, 0, zdo.PrefabInfo.SpawnArea.m_spawnRadius));
                var maxZone = ZoneSystem.GetZone(zdo.GetPosition() + new Vector3(zdo.PrefabInfo.SpawnArea.m_spawnRadius, 0, zdo.PrefabInfo.SpawnArea.m_spawnRadius));
                for (var x = minZone.x; x <= maxZone.x; x++)
                {
                    for (var y = minZone.y; y <= maxZone.y; y++)
                    {
                        var sector = new Vector2i(x, y);
                        if (!_sectorStates.TryGetValue(sector, out var state))
                            _sectorStates.Add(sector, state = new());

                        foreach (var data in zdo.PrefabInfo.SpawnArea.m_prefabs)
                        {
                            var prefab = data.m_prefab.name.GetStableHashCode();
                            if (!state.SpawnAreasBySpawned.TryGetValue(prefab, out var list))
                                state.SpawnAreasBySpawned.Add(prefab, list = []);
                            list.Add(new(zdo.m_uid, zdo.GetPosition(), zdo.PrefabInfo.SpawnArea.m_spawnRadius, data.m_minLevel, data.m_maxLevel, zdo.PrefabInfo.SpawnArea.m_levelupChance));
                            zdo.Destroyed += x => list.RemoveAll(y => y.ID == x.m_uid);
                        }
                    }
                }
                break;

            case { Humanoid: not null }:
                LevelUpHumanoid(zdo);
                break;
        }

        return false;
    }

    void LevelUpSpawner(ExtendedZDO zdo)
    {
        var increase = Config.Creatures.MaxLevelIncrease.Value;
        if (Config.Creatures.MaxLevelIncreasePerDefeatedBoss.Value > 0)
        {
            var biome = WorldGenerator.instance.GetBiome(zdo.GetPosition());
            if (_levelIncreasePerBiome.TryGetValue(biome, out var value))
                increase += value;
        }

        var fields = zdo.Fields<CreatureSpawner>();

        if (zdo.PrefabInfo.CreatureSpawner!.m_respawnTimeMinuts <= 0 && fields.SetIfChanged(static x => x.m_respawnTimeMinuts, Config.Creatures.RespawnOneTimeSpawnsAfter.Value))
            RecreateZdo = true;

        var maxLevel = zdo.PrefabInfo.CreatureSpawner.m_maxLevel + increase;
        if (fields.SetIfChanged(static x => x.m_maxLevel, maxLevel))
            RecreateZdo = true;

        var steps = maxLevel - zdo.PrefabInfo.CreatureSpawner.m_minLevel;
        if (Config.Creatures.MaxLevelChance.Value is 0 || steps is 0)
        {
            if (fields.ResetIfChanged(static x => x.m_levelupChance))
                RecreateZdo = true;
        }
        else
        {
            var chance = Config.Creatures.MaxLevelChance.Value / 100.0;
            chance = Math.Pow(chance, 1.0 / steps);
            chance *= 100.0;
            if (fields.SetIfChanged(static x => x.m_levelupChance, (float)chance))
                RecreateZdo = true;
        }
    }

    void LevelUpHumanoid(ExtendedZDO zdo)
    {
        var initialLevel = zdo.Vars.GetInitialLevel();
        if (initialLevel is not 0)
        {
            if (initialLevel > 0 && Config.Creatures.MaxLevelIncrease.Value is 0 && Config.Creatures.MaxLevelIncreasePerDefeatedBoss.Value is 0)
                zdo.Vars.SetLevel(initialLevel);
            return;
        }

        if (zdo.Vars.GetTamed() || zdo.Vars.GetSpawnedByTrophy())
            return;

        if (_sectorStates.TryGetValue(ZoneSystem.GetZone(zdo.GetPosition()), out var state) &&
            state.CreatureSpawnersBySpawned.TryGetValue(zdo.GetPrefab(), out var list) &&
            list.Any(x => x.GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned) == zdo.m_uid))
        {
            initialLevel = -1;
        }
        else
        {
            initialLevel = zdo.Vars.GetLevel();
        }

        zdo.Vars.SetInitialLevel(initialLevel);

        if (initialLevel <= 0)
            return;

        if (zdo.PrefabInfo.Humanoid is { Humanoid.m_boss: true } && !Config.Creatures.LevelUpBosses.Value)
            return;

        var increase = Config.Creatures.MaxLevelIncrease.Value;
        SpawnData spawnData;

        if (state is not null && state.SpawnAreasBySpawned.TryGetValue(zdo.GetPrefab(), out var spawnAreas) &&
            spawnAreas.FirstOrDefault(x => Vector3.Distance(x.Position, zdo.GetPosition()) <= x.Radius) is { } spawnAreaData)
        {
            spawnData = spawnAreaData;
            if (_levelIncreasePerBiome.TryGetValue(WorldGenerator.instance.GetBiome(spawnAreaData.Position), out var value))
                increase += value;
        }
        else
        {
            var biome = WorldGenerator.instance.GetBiome(zdo.GetPosition());
            var biomeArea = WorldGenerator.instance.GetBiomeArea(zdo.GetPosition());
            float? distanceFromCenter = null;
            if (!_spawnData.TryGetValue((biome, biomeArea, zdo.GetPrefab()), out var spawnDataList) ||
                spawnDataList.FirstOrDefault(x => IsValidSpawnData(x, distanceFromCenter ??= Utils.LengthXZ(zdo.GetPosition()))) is not { } spawnSystemData)
            {
                Logger.LogWarning($"{zdo.PrefabInfo.PrefabName}: Spawn source not found");
                return;
            }

            spawnData = spawnSystemData;
            if (_levelIncreasePerBiome.TryGetValue(biome, out var value))
                increase += value;
        }

        var chance = SpawnSystem.GetLevelUpChance(spawnData.LevelUpChance);
        var maxLevel = spawnData.MaxLevel + increase;
        var steps = maxLevel - spawnData.MinLevel;
        if (Config.Creatures.MaxLevelChance.Value > 0 && steps > 0)
        {
            var c = Config.Creatures.MaxLevelChance.Value / 100.0;
            c = Math.Pow(c, 1.0 / steps);
            c *= 100.0;
            chance = (float)c;
        }

        var level = spawnData.MinLevel;
        while (level < maxLevel && UnityEngine.Random.Range(0f, 100f) <= chance)
            level++;

        if (level == initialLevel)
            return;

        zdo.Vars.SetLevel(level);
        RecreateZdo = true;
    }

    static bool IsValidSpawnData(SpawnSystemData data, float distanceFromCenter)
    {
        if (data.Data.m_minDistanceFromCenter > 0 && data.Data.m_minDistanceFromCenter > distanceFromCenter)
            return false;
        if (data.Data.m_maxDistanceFromCenter > 0 && data.Data.m_maxDistanceFromCenter < distanceFromCenter)
            return false;
        if (data.Data.m_spawnAtDay && EnvMan.IsDay())
            return true;
        if (data.Data.m_spawnAtNight && EnvMan.IsNight())
            return true;
        return false;
    }
}
