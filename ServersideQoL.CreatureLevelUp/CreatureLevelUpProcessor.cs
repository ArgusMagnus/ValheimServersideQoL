using Microsoft.CodeAnalysis;
using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using static Heightmap;
using static ServersideQoL.CreatureLevelUp.Config;

namespace ServersideQoL.CreatureLevelUp;

[Processor(Id)]
public sealed class CreatureLevelUpProcessor : Processor<CreatureLevelUpProcessor.PrefabInfo>
{
  public const string Id = "5ab42c92-d2fd-4efe-8904-720a46ac7f5b";
  public sealed record PrefabInfo(Character? Character, CreatureSpawner? CreatureSpawner, SpawnArea? SpawnArea, LocationProxy? LocationProxy) : ProcessorPrefabInfo
  {
    public TriggerPersistentEventOnDestroy? TriggerPersistentEventOnDestroy { get; private set; }

    public override bool IsValid
    {
      get
      {
        if (SpawnArea is not null && PrefabInfo.GetComponent<TriggerPersistentEventOnDestroy>() is { _stopEvent: true } ev)
          TriggerPersistentEventOnDestroy = ev;
        return base.IsValid;
      }
    }
  }

  readonly Dictionary<Biome, int> _levelIncreasePerBiome = [];
  readonly Dictionary<Vector2s, SectorState> _sectorStates = [];
  readonly Dictionary<(Biome, int Prefab), List<SpawnSystemData>> _spawnData = [];
  readonly Dictionary<string, EventInfo> _spawnDataByEvent = [];
  readonly SectorDictionary<ServersideQoLZDO> _locations = new(ZoneSystem.c_ZoneSize);
  readonly HashSet<ServersideQoLZDO> _creatureSpawners = [];
  readonly Dictionary<string, Biome> _persistentEventBiomes = [];
  PersistentEventInfoSearcher? _persistentEventInfoSearcher;
  bool _persistentEventSearchDone;

  record SpawnData(int Prefab, int MinLevel, int MaxLevel, float LevelUpChance);

  sealed record SpawnSystemData(SpawnSystem.SpawnData Data, Biome? BiomeOverwrite) : SpawnData(Data.m_prefab.name.GetStableHashCode(), Data.m_minLevel, Data.m_maxLevel, Data.m_overrideLevelupChance);

  static readonly ServerVar<int> __initialLevelVar = CreatureLevelUpPlugin.RegisterServerVar<int>("InitialLevel");

  protected override void Initialize()
  {
    _sectorStates.Clear();
    _spawnData.Clear();
    _locations.Clear();
    _creatureSpawners.Clear();

    foreach (var zdo in ZDOMan.instance.m_objectsByID.Values.Select(static x => x.ServersideQoLZDO))
    {
      if (GetProcessorPrefabInfo(zdo)?.CreatureSpawner is not { } creatureSpawner)
        continue;

      var sector = zdo.ZDO.GetSector();
      if (!_sectorStates.TryGetValue(sector, out var state))
        _sectorStates.Add(sector, state = new());

      var prefab = creatureSpawner.m_creaturePrefab.name.GetStableHashCode();
      if (!state.CreatureSpawnersBySpawned.TryGetValue(prefab, out var set))
        state.CreatureSpawnersBySpawned.Add(prefab, set = []);

      if (!set.Contains(zdo))
      {
        set.Add(zdo);
        zdo.Destroyed += zdo => set.Remove(zdo);
      }
    }

    ServersideQoLPlugin.Instance.GlobalKeysChanged -= InitializeData;
    if (Config.Instance.MaxLevelIncrease.Value > 0 || Config.Instance.MaxLevelIncreasePerDefeatedBoss.Value > 0)
    {
      InitializeData();
      ServersideQoLPlugin.Instance.GlobalKeysChanged += InitializeData;
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    var result = ProcessResult.UnregisterProcessor;

    switch (prefabInfo)
    {
      case { LocationProxy: not null }:
        _locations.Add(zdo.ZDO.GetPosition(), zdo);
        break;
      case { CreatureSpawner: not null }:
        if (!TryGetLocation(zdo.ZDO.GetPosition(), out var locZdo, out _) || _creatureSpawners.Add(zdo))
          result = ScheduleReprocessing(Config.Instance.Advanced.Value.CreatureSpawnerProcessingDelaySeconds); // give time for locations to be processed
        else if (_creatureSpawners.Remove(zdo))
        {          
          result |= LevelUpSpawner(zdo, prefabInfo.CreatureSpawner, locZdo?.ZDO.GetPosition()) | ProcessResult.ReregisterOnRecreated;
          //if ((result & ProcessResult.RecreateZDO) is not 0)
          {
            var sector = zdo.ZDO.GetSector();
            if (!_sectorStates.TryGetValue(sector, out var state))
              _sectorStates.Add(sector, state = new());

            var prefab = prefabInfo.CreatureSpawner.m_creaturePrefab.name.GetStableHashCode();
            if (!state.CreatureSpawnersBySpawned.TryGetValue(prefab, out var set))
              state.CreatureSpawnersBySpawned.Add(prefab, set = []);

            if (!set.Contains(zdo))
            {
              set.Add(zdo);
              zdo.Destroyed += zdo => set.Remove(zdo);
            }
          }
        }
        break;

      case { SpawnArea: not null }:
        {
          var biome = (Biome)zdo.Vars.GetLevel((int)Biome.None);
          if (biome is 0)
          {
            if (prefabInfo.TriggerPersistentEventOnDestroy is not null)
            {
              if (_persistentEventBiomes.TryGetValue(prefabInfo.TriggerPersistentEventOnDestroy._eventInternalName, out biome))
                zdo.Vars.SetLevel((int)biome);
              else
              {
                if (_persistentEventSearchDone)
                  Logger.LogWarning($"Biome for persistent event {prefabInfo.TriggerPersistentEventOnDestroy._eventInternalName} not found");
                else
                {
                  Logger.LogDiagnosticInfo($"Biome for persistent event {prefabInfo.TriggerPersistentEventOnDestroy._eventInternalName} not (yet) found");
                  result = ScheduleReprocessing(Config.Instance.Advanced.Value.PersistentEventReprocessingDelaySeconds);
                  _persistentEventInfoSearcher ??= new(_persistentEventBiomes, Logger);
                  if (_persistentEventSearchDone = _persistentEventInfoSearcher.Search())
                    _persistentEventInfoSearcher = null;
                }
                break;
              }
            }
            else
            {
              biome = GetBiome(zdo.ZDO.GetPosition());
              if (RandEventSystem.instance.m_randomEvent is { } currentEvent &&
                  GetEventInfo(currentEvent, out var eventInfo) &&
                  eventInfo.SpawnAreas.Contains(zdo.ZDO.GetPrefab()))
              {
                var minEventZone = ZoneSystem.GetZone(currentEvent.m_pos - new Vector3(currentEvent.m_eventRange, 0, currentEvent.m_eventRange)) - new Vector2s(1, 1);
                var maxEventZone = ZoneSystem.GetZone(currentEvent.m_pos + new Vector3(currentEvent.m_eventRange, 0, currentEvent.m_eventRange)) + new Vector2s(1, 1);
                var zone = zdo.ZDO.GetSector();
                if (zone.x >= minEventZone.x && zone.x <= maxEventZone.x &&
                    zone.y >= minEventZone.y && zone.y <= maxEventZone.y)
                {
                  biome = eventInfo.Biome;
                  zdo.Vars.SetLevel((int)biome);
                  //Logger.DevLog($"{GetPrefabInfo(zdo).PrefabName}: Event spawner: {biome}");
                }
              }
            }
          }

          var minZone = ZoneSystem.GetZone(zdo.ZDO.GetPosition() - new Vector3(prefabInfo.SpawnArea.m_spawnRadius, 0, prefabInfo.SpawnArea.m_spawnRadius));
          var maxZone = ZoneSystem.GetZone(zdo.ZDO.GetPosition() + new Vector3(prefabInfo.SpawnArea.m_spawnRadius, 0, prefabInfo.SpawnArea.m_spawnRadius));
          for (var x = minZone.x; x <= maxZone.x; x++)
          {
            for (var y = minZone.y; y <= maxZone.y; y++)
            {
              var sector = new Vector2s(x, y);
              if (!_sectorStates.TryGetValue(sector, out var state))
                _sectorStates.Add(sector, state = new());

              foreach (var data in prefabInfo.SpawnArea.m_prefabs)
              {
                var prefab = data.m_prefab.name.GetStableHashCode();
                if (!state.SpawnAreasBySpawned.TryGetValue(prefab, out var list))
                  state.SpawnAreasBySpawned.Add(prefab, list = []);
                list.Add(new(zdo.ZDO.m_uid, zdo.ZDO.GetPosition(), biome, prefabInfo.SpawnArea.m_spawnRadius, prefab, data.m_minLevel, data.m_maxLevel, prefabInfo.SpawnArea.m_levelupChance));
                zdo.Destroyed += x => list.RemoveAll(y => y.ID == x.ZDO.m_uid);
              }
            }
          }
        }
        break;

      case { Character.m_faction: not Character.Faction.PlayerSpawned }:
        result |= LevelUpCharacter(zdo, prefabInfo.Character);
        break;
    }

    return result;
  }

  void InitializeData()
  {
    _levelIncreasePerBiome.Clear();
    Dictionary<string, Biome> biomePerBossKey = [];
    if (Config.Instance.MaxLevelIncreasePerDefeatedBoss.Value > 0)
    {
      var increase = 0;
      foreach (var (boss, biome) in BossesInOrder.Reverse())
      {
        if (ZoneSystem.instance.GetGlobalKey(boss.m_defeatSetGlobalKey))
          increase += Config.Instance.MaxLevelIncreasePerDefeatedBoss.Value;
        _levelIncreasePerBiome.Add(biome, increase);
        biomePerBossKey.Add(boss.m_defeatSetGlobalKey, biome);
      }
      if (_levelIncreasePerBiome.TryGetValue(Config.Instance.TreatOceanAs.Value, out var oceanIncrease))
        _levelIncreasePerBiome.Add(Biome.Ocean, oceanIncrease);
    }

    _spawnData.Clear();
    foreach (var spawner in ZoneSystem.instance.m_zoneCtrlPrefab.GetComponent<SpawnSystem>().m_spawnLists.SelectMany(static x => x.m_spawners))
    {
      if (!spawner.m_enabled || spawner.m_prefab.GetComponent<Character>() is null)
        continue;

      if (!string.IsNullOrEmpty(spawner.m_requiredGlobalKey) && !ZoneSystem.instance.GetGlobalKey(spawner.m_requiredGlobalKey))
        continue;

      foreach (var biome in ConfigBase.AcceptableEnum<Biome>.Default.AcceptableValues)
      {
        if (!spawner.m_biome.HasFlag(biome))
          continue;

        var prefab = spawner.m_prefab.name.GetStableHashCode();
        if (!_spawnData.TryGetValue((biome, prefab), out var list))
          _spawnData.Add((biome, prefab), list = []);
        list.Add(new(spawner, biomePerBossKey.TryGetValue(spawner.m_requiredGlobalKey ?? "", out var b) ? b : null));
      }
    }

    foreach (var list in _spawnData.Values)
      list.Sort(static (a, b) => b.MaxLevel - a.MaxLevel);
  }

  ProcessResult LevelUpSpawner(ServersideQoLZDO zdo, CreatureSpawner creatureSpawner, Vector3? refPos)
  {
    var result = ProcessResult.Default;
    var fields = zdo.Fields<CreatureSpawner>();

    Biome? biome = null;

    if (creatureSpawner.m_respawnTimeMinuts <= 0)
    {
      var respawnTime = Config.Instance.RespawnOneTimeSpawnsAfterMinutes.Value;
      if (respawnTime > 0)
      {
        if (Config.Instance.RespawnOneTimeSpawnsCondition.Value is RespawnOneTimeSpawnsConditions.Never)
          respawnTime = 0;
        else if (Config.Instance.RespawnOneTimeSpawnsCondition.Value is RespawnOneTimeSpawnsConditions.AfterBossDefeated)
        {
          biome ??= GetBiome(refPos ?? zdo.ZDO.GetPosition());
          if (!BossesByBiome.TryGetValue(biome.Value, out var boss) || !ZoneSystem.instance.GetGlobalKey(boss.m_defeatSetGlobalKey))
            respawnTime = 0;
        }
      }

      if (fields.UpdateValue(static () => x => x.m_respawnTimeMinuts, respawnTime))
        result |= ProcessResult.RecreateZDO;
    }

    /// todo: check <see cref="Location.m_excludeEnemyLevelOverrideGroups"/>, <see cref="Location.m_enemyMinLevelOverride"/>, etc.
    ///       <see cref="CreatureSpawner.Spawn"/>

    var increase = Config.Instance.MaxLevelIncrease.Value;
    if (Config.Instance.MaxLevelIncreasePerDefeatedBoss.Value > 0)
    {
      biome ??= GetBiome(refPos ?? zdo.ZDO.GetPosition());
      if (_levelIncreasePerBiome.TryGetValue(biome.Value, out var value))
        increase += value;
    }

    var originalMaxLevel = Math.Max(1, creatureSpawner.m_maxLevel);

    // Some CreatureSpawner, namely Spawner_Draugr/Spawner_Draugr_Ranged, have MinLevel > MaxLevel
    var minLevel = Math.Max(1, Math.Min(creatureSpawner.m_minLevel, originalMaxLevel));
    if (fields.UpdateValue(static () => x => x.m_minLevel, minLevel))
      result |= ProcessResult.RecreateZDO;

    var maxLevel = originalMaxLevel + increase;
    if (Config.Instance.MaxLevelCap.Value > 0)
      maxLevel = Math.Min(maxLevel, Config.Instance.MaxLevelCap.Value);
    if (fields.UpdateValue(static () => x => x.m_maxLevel, maxLevel))
      result |= ProcessResult.RecreateZDO;

    var chance = creatureSpawner.m_levelupChance;
    var steps = maxLevel - minLevel;
    if (steps > 0)
    {
      chance /= 100f;
      chance = Mathf.Pow(chance, Mathf.Max(1f, originalMaxLevel - minLevel) / steps) * 100f;
      if (fields.UpdateValue(static () => x => x.m_levelupChance, chance))
        result |= ProcessResult.RecreateZDO;
    }

    //Logger.DevLog($"{GetPrefabInfo(zdo).PrefabName}: max: {maxLevel} (+{increase}), chance: {chance:F2}%");
    return result;
  }

  ProcessResult LevelUpCharacter(ServersideQoLZDO zdo, Character character)
  {
    var result = ProcessResult.Default;
    if (character is Player)
      return result;

    var initialLevel = __initialLevelVar.Get(zdo);
    if (initialLevel is not 0)
    {
      if (initialLevel > 0 && Config.Instance.MaxLevelIncrease.Value is 0 && Config.Instance.MaxLevelIncreasePerDefeatedBoss.Value is 0)
      {
        zdo.Vars.SetLevel(initialLevel);
        __initialLevelVar.Remove(zdo);
      }
      return result;
    }

    if (Config.Instance.MaxLevelIncrease.Value is 0 && Config.Instance.MaxLevelIncreasePerDefeatedBoss.Value is 0)
      return result;

    if (zdo.Vars.GetTamed())
      return result;

    if (_sectorStates.TryGetValue(zdo.ZDO.GetSector(), out var state) &&
        state.CreatureSpawnersBySpawned.TryGetValue(zdo.ZDO.GetPrefab(), out var set) &&
        set.Any(x => x.ZDO.GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned) == zdo.ZDO.m_uid))
    {
      initialLevel = -1;
    }
    else
    {
      initialLevel = zdo.Vars.GetLevel();
    }

    __initialLevelVar.Set(zdo, initialLevel);

    if (initialLevel <= 0)
      return result;

    var pos = zdo.Vars.GetSpawnPoint(zdo.ZDO.GetPosition());

    var increase = Config.Instance.MaxLevelIncrease.Value;
    SpawnData spawnData;
    Biome biome;

    if (character.m_boss)
    {
      if (!Config.Instance.LevelUpBosses.Value)
        return result;
      spawnData = new(zdo.ZDO.GetPrefab(), 1, 1, 0);
      if (_levelIncreasePerBiome.TryGetValue(biome = GetBiome(pos), out var value))
        increase += value;
    }
    else if (zdo.Vars.GetEventCreature())
    {
      if (RandEventSystem.instance.m_randomEvent is not { } currentEvent)
      {
        Logger.LogWarning($"{GetPrefabInfo(zdo).PrefabName} is an event creature, but no active event was found");
        return result;
      }

      if (!GetEventInfo(currentEvent, out var eventInfo))
        return result;

      if (!eventInfo.SpawnData.TryGetValue(zdo.ZDO.GetPrefab(), out var spawnSystemData))
      {
        Logger.LogWarning($"{GetPrefabInfo(zdo).PrefabName}: Spawn source not found in event {currentEvent.m_name}");
        return result;
      }

      spawnData = spawnSystemData;
      if (_levelIncreasePerBiome.TryGetValue(biome = eventInfo.Biome, out var value))
        increase += value;
    }
    else if (state is not null && state.SpawnAreasBySpawned.TryGetValue(zdo.ZDO.GetPrefab(), out var spawnAreas) &&
        spawnAreas.FirstOrDefault(x => Vector3.Distance(x.Position, pos) <= x.Radius) is { } spawnAreaData)
    {
      spawnData = spawnAreaData;
      if (_levelIncreasePerBiome.TryGetValue(biome = spawnAreaData.Biome, out var value))
        increase += value;
    }
    else
    {
      biome = GetBiome(pos);
      float? distanceFromCenter = null;
      if (!_spawnData.TryGetValue((biome, zdo.ZDO.GetPrefab()), out var spawnDataList) ||
          spawnDataList.FirstOrDefault(x => IsValidSpawnData(x, distanceFromCenter ??= Utils.LengthXZ(pos))) is not { } spawnSystemData)
      {
        var spawnListStr = spawnDataList is null ? "" : string.Join($"{Environment.NewLine}  ", spawnDataList.Select(static x =>
        $"{x.Data.m_prefab.name} ({x.Prefab}): {x.Data.m_biome}, day: {x.Data.m_spawnAtDay}, night: {x.Data.m_spawnAtNight}").Prepend(""));
        Logger.LogDiagnosticInfo($"{GetPrefabInfo(zdo).PrefabName} ({zdo.ZDO.GetPrefab()}): Spawn source not found in {biome}, day: {EnvMan.IsDay()}, night: {EnvMan.IsNight()}{spawnListStr}");
        return result;
      }

      spawnData = spawnSystemData;
      if (spawnSystemData.BiomeOverwrite is not null)
        biome = spawnSystemData.BiomeOverwrite.Value;
      if (_levelIncreasePerBiome.TryGetValue(biome, out var value))
        increase += value;
    }

    if (increase <= 0)
      return result;

    var originalMaxLevel = Math.Max(1, spawnData.MaxLevel);
    var minLevel = Math.Max(1, Math.Min(spawnData.MinLevel, originalMaxLevel)); // Some SpawnArea, namely Spawner_CharredStone_event, have MinLevel > MaxLevel
    var maxLevel = originalMaxLevel + increase;
    if (Config.Instance.MaxLevelCap.Value > 0)
      maxLevel = Math.Min(maxLevel, Config.Instance.MaxLevelCap.Value);
    var chance = SpawnSystem.GetLevelUpChance(zdo.ZDO.GetPosition(), spawnData.LevelUpChance);
    var steps = maxLevel - minLevel;
    if (steps is not 0)
    {
      chance /= 100f;
      chance = Mathf.Pow(chance, Mathf.Max(1f, originalMaxLevel - minLevel) / steps) * 100f;
    }

    var level = minLevel;
    while (level < maxLevel && UnityEngine.Random.Range(0f, 100f) <= chance)
      level++;

    if (level == initialLevel)
      return result;

    //Logger.DevLog($"{GetPrefabInfo(zdo).PrefabName}: Set level {initialLevel} -> {level} (min: {minLevel}, max: {maxLevel} (+{increase} {biome}), chance: {chance:F2}%)");
    zdo.Vars.SetLevel(level);
    //zdo.Vars.RemoveMaxHealth();
    /// <see cref="Character.SetupMaxHealth"/>
    var baseMaxHealth = zdo.Fields<Character>().GetFloat(static () => x => x.m_health);
    if (Game.m_worldLevel > 0)
      baseMaxHealth *= Game.m_worldLevel * Game.instance.m_worldLevelEnemyHPMultiplier;
    zdo.Vars.SetMaxHealth(level * baseMaxHealth);
    return result | ProcessResult.RecreateZDO;
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
      if (!Config.Instance.Advanced.Value.EventBiomeOverrides.TryGetValue(currentEvent.m_name, out var biome))
      {
        biome = Biome.None;
        foreach (var (boss, b) in BossesInOrder.Enumerate())
        {
          if (biome is Biome.None)
          {
            if (currentEvent.m_requiredGlobalKeys.Contains(boss.m_defeatSetGlobalKey))
              biome = b;
          }
          else // one biome higher than the boss that enabled the event
          {
            biome = b;
            break;
          }
        }
      }

      if (biome is Biome.None)
      {
        Logger.LogWarning($"Associated boss for event {currentEvent.m_name} not found");
        return false;
      }

      //Logger.DevLog($"Event {currentEvent.m_name}: {biome}");
      eventInfo = new(biome);
      _spawnDataByEvent.Add(currentEvent.m_name, eventInfo);
      foreach (var data in currentEvent.m_spawn)
      {
        if (data.m_prefab.GetComponent<Character>() is not null)
          eventInfo.SpawnData.Add(data.m_prefab.name.GetStableHashCode(), new(data, biome));
        else if (data.m_prefab.GetComponent<SpawnArea>() is not null)
          eventInfo.SpawnAreas.Add(data.m_prefab.name.GetStableHashCode());
      }
    }
    return true;
  }

  bool TryGetLocation(Vector3 position, out ServersideQoLZDO? locationZdo, out Location? location)
  {
    location = null;
    locationZdo = null;
    var result = true;
    /// <see cref="Location.GetLocation"/>
    Vector2s? zone = null;
    foreach (var locZdo in _locations.EnumerateAdjacent(position))
    {
      using var loc = ZoneSystem.instance.GetAndLoadLocationByHash(locZdo.Vars.GetLocation());
      if (!loc.IsValid)
        continue;
      if (loc.Prefab is not { } prefab)
      {
        result = false;
        continue;
      }

      locationZdo = locZdo;
      location = loc.Prefab.GetComponent<Location>();
      if (Character.InInterior(position))
      {
        zone ??= ZoneSystem.GetZone(position);
        if (zone == ZoneSystem.GetZone(locZdo.ZDO.GetPosition()))
          return true;
      }
      else
      {
        loc.Prefab.transform.SetLocalPositionAndRotation(locZdo.ZDO.GetPosition(), locZdo.ZDO.GetRotation());
        if (location.IsInside(position, 0f))
          return true;
      }
    }
    return result;
  }

  sealed class SectorState
  {
    public Dictionary<int, HashSet<ServersideQoLZDO>> CreatureSpawnersBySpawned { get; } = [];
    public Dictionary<int, List<SpawnAreaData>> SpawnAreasBySpawned { get; } = [];

    public sealed record SpawnAreaData(ZDOID ID, Vector3 Position, Biome Biome, float Radius, int Prefab, int MinLevel, int MaxLevel, float LevelUpChance)
        : SpawnData(Prefab, MinLevel, MaxLevel, LevelUpChance);
  }

  sealed record EventInfo(Biome Biome)
  {
    public Dictionary<int, SpawnSystemData> SpawnData { get; } = [];
    public HashSet<int> SpawnAreas { get; } = [];
  }

  sealed class PersistentEventInfoSearcher
  {
    readonly HashSet<string> _toLoad = [];
    readonly HashSet<string> _events = [];
    readonly Stack<ZoneSystem.ZoneLocation> _locations = new(ZoneSystem.instance.m_locations);
    readonly Dictionary<string, Biome> _biomeByEventName;
    readonly Logger _logger;

    public PersistentEventInfoSearcher(Dictionary<string, Biome> biomeByEventName, Logger logger)
    {
      _biomeByEventName = biomeByEventName;
      _logger = logger;
      foreach (var prefab in ZNetScene.instance.m_prefabs)
      {
        if (prefab.GetComponent<TriggerPersistentEventOnDestroy>() is not { _stopEvent: false } trigger)
          continue;
        _toLoad.Add(prefab.name);
        _events.Add(trigger._eventInternalName);
      }
    }

    public bool Search()
    {
      while (_events.Count > 0 && _toLoad.Count > 0 && _locations.TryPop(out var loc))
      {
        using var location = new Utilities.Location(loc);
        if (!location.IsValid)
          continue;

        if (location.Prefab is not { } prefab)
        {
          _locations.Push(loc);
          return false;
        }

        if (Utils.GetEnabledComponentsInChildren<DungeonGenerator>(prefab).FirstOrDefault() is { } dungeonGenerator)
        {
          var rooms = dungeonGenerator.GetAvailableRoomPrefabs();
          foreach (var room in rooms)
          {
            if (!room.IsValid)
              continue;
            room.Load();
            if (room.Asset is not { } roomPrefab)
              continue;
            foreach (var trigger in roomPrefab.GetComponentsInChildren<TriggerPersistentEventOnDestroy>())
            {
              if (!_toLoad.Remove(trigger.gameObject.name) || !_events.Remove(trigger._eventInternalName))
                continue;
                _biomeByEventName.Add(trigger._eventInternalName, loc.m_biome);
              _logger.LogDiagnosticInfo($"Found trigger for {trigger._eventInternalName} in {loc.m_biome} ({prefab.name})");
            }
          }
        }
        else
        {
          foreach (var trigger in prefab.GetComponentsInChildren<TriggerPersistentEventOnDestroy>())
          {
            if (!_toLoad.Remove(trigger.gameObject.name) || !_events.Remove(trigger._eventInternalName))
              continue;
            _biomeByEventName.Add(trigger._eventInternalName, loc.m_biome);
            _logger.LogDiagnosticInfo($"Found trigger for {trigger._eventInternalName} in {loc.m_biome} ({prefab.name})");
          }
        }
      }

      return true;
    }
  }
}
