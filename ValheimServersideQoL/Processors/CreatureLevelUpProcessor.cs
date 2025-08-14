using System.Diagnostics.CodeAnalysis;
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
    readonly Dictionary<string, EventInfo> _spawnDataByEvent = [];

    record SpawnData(int Prefab, int MinLevel, int MaxLevel, float LevelUpChance);

    sealed record SpawnSystemData(SpawnSystem.SpawnData Data) : SpawnData(Data.m_prefab.name.GetStableHashCode(), Data.m_minLevel, Data.m_maxLevel, Data.m_overrideLevelupChance);

    sealed class SectorState
    {
        public Dictionary<int, List<ExtendedZDO>> CreatureSpawnersBySpawned { get; } = [];
        public Dictionary<int, List<SpawnAreaData>> SpawnAreasBySpawned { get; } = [];

        public sealed record SpawnAreaData(ZDOID ID, Vector3 Position, Biome Biome, float Radius, int Prefab, int MinLevel, int MaxLevel, float LevelUpChance)
            : SpawnData(Prefab, MinLevel, MaxLevel, LevelUpChance);
    }

    sealed record EventInfo(Biome Biome)
    {
        public Dictionary<int, SpawnSystemData> SpawnData { get; } = [];
        public HashSet<int> SpawnAreas { get; } = [];
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        if (firstTime)
        {
            _sectorStates.Clear();
            _spawnData.Clear();
        }

        ZoneSystemSendGlobalKeys.GlobalKeysChanged -= InitializeData;
        if (Config.Creatures.MaxLevelIncrease.Value > 0 || Config.Creatures.MaxLevelIncreasePerDefeatedBoss.Value > 0)
        {
            InitializeData();
            ZoneSystemSendGlobalKeys.GlobalKeysChanged += InitializeData;
        }
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
            if (!spawner.m_enabled || spawner.m_prefab.GetComponent<Character>() is null)
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

        foreach (var list in _spawnData.Values)
            list.Sort(static (a, b) => b.MaxLevel - a.MaxLevel);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;

        switch (zdo.PrefabInfo)
        {
            case { CreatureSpawner: not null }:
                LevelUpSpawner(zdo);
                if (!RecreateZdo)
                {
                    var sector = zdo.GetSector();
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
                var biome = (Biome)zdo.Vars.GetLevel();
                if (biome is 0)
                {
                    biome = WorldGenerator.instance.GetBiome(zdo.GetPosition());
                    if (RandEventSystem.instance.GetCurrentEvent() is { } currentEvent &&
                        GetEventInfo(currentEvent, out var eventInfo) &&
                        eventInfo.SpawnAreas.Contains(zdo.GetPrefab()))
                    {
                        var minEventZone = ZoneSystem.GetZone(currentEvent.m_pos - new Vector3(currentEvent.m_eventRange, 0, currentEvent.m_eventRange)) - new Vector2i(1, 1);
                        var maxEventZone = ZoneSystem.GetZone(currentEvent.m_pos + new Vector3(currentEvent.m_eventRange, 0, currentEvent.m_eventRange)) + new Vector2i(1, 1);
                        var zone = zdo.GetSector();
                        if (zone.x >= minEventZone.x && zone.x <= maxEventZone.x &&
                            zone.y >= minEventZone.y && zone.y <= maxEventZone.y)
                        {
                            biome = eventInfo.Biome;
                            zdo.Vars.SetLevel((int)biome);
                            Logger.DevLog($"{zdo.PrefabInfo.PrefabName}: Event spawner: {biome}");
                        }
                    }
                }

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
                            list.Add(new(zdo.m_uid, zdo.GetPosition(), biome, zdo.PrefabInfo.SpawnArea.m_spawnRadius, prefab, data.m_minLevel, data.m_maxLevel, zdo.PrefabInfo.SpawnArea.m_levelupChance));
                            zdo.Destroyed += x => list.RemoveAll(y => y.ID == x.m_uid);
                        }
                    }
                }
                break;

            case { Humanoid: not null and { Humanoid.m_faction: not Character.Faction.PlayerSpawned } }:
                LevelUpCharacter<Humanoid>(zdo);
                break;

            case { Character: not null and { Character.m_faction: not Character.Faction.PlayerSpawned } }:
                LevelUpCharacter<Character>(zdo);
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
        if (steps is 0)
            return;

        var chance = zdo.PrefabInfo.CreatureSpawner.m_levelupChance / 100f;
        if (zdo.PrefabInfo.CreatureSpawner.m_maxLevel > zdo.PrefabInfo.CreatureSpawner.m_minLevel)
            chance = Mathf.Pow(chance, zdo.PrefabInfo.CreatureSpawner.m_maxLevel - zdo.PrefabInfo.CreatureSpawner.m_minLevel);
        chance = Mathf.Pow(chance, 1f / steps) * 100f;
        if (fields.SetIfChanged(static x => x.m_levelupChance, chance))
            RecreateZdo = true;
    }

    void LevelUpCharacter<T>(ExtendedZDO zdo) where T : Character
    {
        var initialLevel = zdo.Vars.GetInitialLevel();
        if (initialLevel is not 0)
        {
            if (initialLevel > 0 && Config.Creatures.MaxLevelIncrease.Value is 0 && Config.Creatures.MaxLevelIncreasePerDefeatedBoss.Value is 0)
            {
                zdo.Vars.SetLevel(initialLevel);
                zdo.Vars.RemoveInitialLevel();
            }
            return;
        }

        if (Config.Creatures.MaxLevelIncrease.Value is 0 && Config.Creatures.MaxLevelIncreasePerDefeatedBoss.Value is 0)
            return;

        if (zdo.Vars.GetTamed() || zdo.Vars.GetSpawnedByTrophy())
            return;

        if (_sectorStates.TryGetValue(zdo.GetSector(), out var state) &&
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

        var increase = Config.Creatures.MaxLevelIncrease.Value;
        SpawnData spawnData;

        if (zdo.PrefabInfo.Humanoid is { Humanoid.m_boss: true })
        {
            if (!Config.Creatures.LevelUpBosses.Value)
                return;
            spawnData = new(zdo.GetPrefab(), 1, 1, 0);
            if (_levelIncreasePerBiome.TryGetValue(WorldGenerator.instance.GetBiome(zdo.GetPosition()), out var value))
                increase += value;
        }
        else if (zdo.Vars.GetEventCreature())
        {
            if (RandEventSystem.instance.GetCurrentEvent() is not { } currentEvent)
            {
                Logger.LogWarning($"{zdo.PrefabInfo.PrefabName} is an event creature, but no active event was found");
                return;
            }

            if (!GetEventInfo(currentEvent, out var eventInfo))
                return;

            if (!eventInfo.SpawnData.TryGetValue(zdo.GetPrefab(), out var spawnSystemData))
            {
                Logger.LogWarning($"{zdo.PrefabInfo.PrefabName}: Spawn source not found in event {currentEvent.m_name}");
                return;
            }

            spawnData = spawnSystemData;
            if (_levelIncreasePerBiome.TryGetValue(eventInfo.Biome, out var value))
                increase += value;
        }
        else if (state is not null && state.SpawnAreasBySpawned.TryGetValue(zdo.GetPrefab(), out var spawnAreas) &&
            spawnAreas.FirstOrDefault(x => Vector3.Distance(x.Position, zdo.GetPosition()) <= x.Radius) is { } spawnAreaData)
        {
            spawnData = spawnAreaData;
            if (_levelIncreasePerBiome.TryGetValue(spawnAreaData.Biome, out var value))
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
                var spawnListStr = spawnDataList is null ? "" : string.Join($"{Environment.NewLine}  ", spawnDataList.Select(static x =>
                $"{x.Data.m_prefab.name} ({x.Prefab}): {x.Data.m_biome} ({x.Data.m_biomeArea}), day: {x.Data.m_spawnAtDay}, night: {x.Data.m_spawnAtNight}").Prepend(""));
                Logger.LogWarning($"{zdo.PrefabInfo.PrefabName} ({zdo.GetPrefab()}): Spawn source not found in {biome} ({biomeArea}), day: {EnvMan.IsDay()}, night: {EnvMan.IsNight()}){spawnListStr}");
                return;
            }

            spawnData = spawnSystemData;
            if (_levelIncreasePerBiome.TryGetValue(biome, out var value))
                increase += value;
        }

        if (increase <= 0)
            return;

        var maxLevel = spawnData.MaxLevel + increase;
        var chance = SpawnSystem.GetLevelUpChance(spawnData.LevelUpChance);
        var steps = maxLevel - spawnData.MinLevel;
        if (steps is not 0)
        {
            chance /= 100f;
            if (spawnData.MaxLevel > spawnData.MinLevel)
                chance = Mathf.Pow(chance, spawnData.MaxLevel - spawnData.MinLevel);
            chance = Mathf.Pow(chance, 1f / steps) * 100f;
        }

        var level = spawnData.MinLevel;
        while (level < maxLevel && UnityEngine.Random.Range(0f, 100f) <= chance)
            level++;

        if (level == initialLevel)
            return;

        Logger.DevLog($"{zdo.PrefabInfo.PrefabName}: Set level {initialLevel} -> {level} (max: {maxLevel} (+{increase}), chance: {chance:F2}%)");
        zdo.Vars.SetLevel(level);
        RecreateZdo = true;
    }

    static bool IsValidSpawnData(SpawnSystemData data, float distanceFromCenter)
    {
        if (!data.Data.m_spawnAtDay && EnvMan.IsDay())
            return false;
        if (!data.Data.m_spawnAtNight && EnvMan.IsNight())
            return false;
        if (data.Data.m_minDistanceFromCenter > 0 && data.Data.m_minDistanceFromCenter > distanceFromCenter)
            return false;
        if (data.Data.m_maxDistanceFromCenter > 0 && data.Data.m_maxDistanceFromCenter < distanceFromCenter)
            return false;
        return true;
    }

    bool GetEventInfo(RandomEvent currentEvent, [NotNullWhen(true)] out EventInfo? eventInfo)
    {
        if (!_spawnDataByEvent.TryGetValue(currentEvent.m_name, out eventInfo))
        {
            var biome = SharedProcessorState.BossesByBiome.FirstOrDefault(x => currentEvent.m_notRequiredGlobalKeys.Contains(x.Value.m_defeatSetGlobalKey)).Key;
            if (biome == default)
                biome = SharedProcessorState.BossesByBiome.FirstOrDefault(x => currentEvent.m_requiredGlobalKeys.Contains(x.Value.m_defeatSetGlobalKey)).Key;
            if (biome == default)
            {
                Logger.LogWarning($"Associated boss for event {currentEvent.m_name} not found");
                return false;
            }
            Logger.DevLog($"Event {currentEvent.m_name}: {biome}");
            eventInfo = new(biome);
            _spawnDataByEvent.Add(currentEvent.m_name, eventInfo);
            foreach (var data in currentEvent.m_spawn)
            {
                if (data.m_prefab.GetComponent<Character>() is not null)
                    eventInfo.SpawnData.Add(data.m_prefab.name.GetStableHashCode(), new(data));
                else if (data.m_prefab.GetComponent<SpawnArea>() is not null)
                    eventInfo.SpawnAreas.Add(data.m_prefab.name.GetStableHashCode());
            }
        }
        return true;
    }
}
