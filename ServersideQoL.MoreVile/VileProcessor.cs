using UnityEngine;

namespace ServersideQoL.MoreVile;

/// <summary>
/// Natural spawns are rolled by the clients' <see cref="SpawnSystem"/>, which a serverside mod cannot change.
/// Instead, this processor rolls the additional spawn chance on the server, using the vanilla spawn data of Viles.
/// </summary>
[Processor(Id)]
public sealed class VileProcessor : Processor<VileProcessor.PrefabInfo>
{
  public const string Id = "a8c4a115-80bb-4288-82d6-6e08a1c57910";
  const string VilePrefabName = "Unbjorn";
  static readonly int __vilePrefab = VilePrefabName.GetStableHashCode();

  // Vanilla defaults used by SpawnSystem when m_spawnRadiusMin/Max are not set
  const float DefaultSpawnRadiusMin = 40f;
  const float DefaultSpawnRadiusMax = 80f;
  const int MaxSpawnPointTries = 10;

  readonly List<SpawnSystem.SpawnData> _spawnData = [];
  readonly Dictionary<int, float> _noSpawnAreaRadiusByPrefab = [];
  readonly Dictionary<(long PeerID, int SpawnDataIndex), float> _nextRollTime = [];
  readonly List<ZDO> _sectorObjects = [];

  public sealed record PrefabInfo(Character Character) : ProcessorPrefabInfo
  {
    public override bool IsValid => PrefabInfo.PrefabHash == __vilePrefab;
  }

  protected override void Initialize()
  {
    _spawnData.Clear();
    _nextRollTime.Clear();
    foreach (var data in ZoneSystem.instance.m_zoneCtrlPrefab.GetComponent<SpawnSystem>().m_spawnLists.SelectMany(static x => x.m_spawners))
    {
      if (data.m_enabled && !data.m_devDisabled && data.m_prefab is not null && data.m_prefab.name is VilePrefabName)
        _spawnData.Add(data);
    }

    if (_spawnData.Count is 0)
      Logger.LogWarning($"No natural spawn data found for {VilePrefabName}");

    _noSpawnAreaRadiusByPrefab.Clear();
    foreach (var prefab in ZNetScene.instance.m_prefabs)
    {
      foreach (var area in prefab.GetComponentsInChildren<EffectArea>(true))
      {
        if ((area.m_type & (EffectArea.Type.PlayerBase | EffectArea.Type.NoMonsters)) is 0 || area.GetComponent<SphereCollider>() is not { } collider)
          continue;
        var scale = area.transform.lossyScale;
        var radius = collider.radius * Mathf.Max(scale.x, scale.z);
        var hash = prefab.name.GetStableHashCode();
        _noSpawnAreaRadiusByPrefab[hash] = Mathf.Max(radius, _noSpawnAreaRadiusByPrefab.TryGetValue(hash, out var r) ? r : 0);
      }
    }
  }

  protected override void PreProcess(PeersEnumerable peers)
  {
    var extraChance = Config.Instance.SpawnChanceMultiplier.Value - 1f;
    if (extraChance <= 0 || _spawnData.Count is 0)
      return;

    var now = Time.time;
    foreach (var peer in peers)
    {
      var playerPos = peer.RefPos;
      if (playerPos.y > 3000) // in a dungeon
        continue;

      var sectorObjectsLoaded = false;
      for (int i = 0; i < _spawnData.Count; i++)
      {
        var key = (peer.ZNetPeer.m_uid, i);
        if (_nextRollTime.TryGetValue(key, out var next) && now < next)
          continue;
        var data = _spawnData[i];
        _nextRollTime[key] = now + data.m_spawnInterval;

        if (!CanSpawnNow(data))
          continue;

        // Same roll as SpawnSystem, but only for the additional chance
        var rolls = 0;
        for (var chance = data.m_spawnChance * extraChance; chance > 0; chance -= 100f)
        {
          if (UnityEngine.Random.Range(0f, 100f) <= chance)
            rolls++;
        }
        if (rolls is 0)
          continue;

        if (!sectorObjectsLoaded)
        {
          _sectorObjects.Clear();
          ZDOMan.instance.FindSectorObjects(peer.GetSector(), ZNet.instance.GetSyncedSimulationDistance(), _sectorObjects);
          sectorObjectsLoaded = true;
        }

        for (int r = 0; r < rolls; r++)
        {
          var maxSpawned = Mathf.RoundToInt(data.m_maxSpawned * Config.Instance.MaxSpawnedMultiplier.Value);
          if (_sectorObjects.Count(static x => x.GetPrefab() == __vilePrefab) >= maxSpawned)
            break;
          if (FindSpawnPoint(data, playerPos, peers) is not { } spawnPoint)
            break;
          SpawnGroup(data, spawnPoint);
        }
      }
    }
    _sectorObjects.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
    => ProcessResult.UnregisterProcessor;

  static bool CanSpawnNow(SpawnSystem.SpawnData data)
  {
    if (!string.IsNullOrEmpty(data.m_requiredGlobalKey) && !ZoneSystem.instance.GetGlobalKey(data.m_requiredGlobalKey))
      return false;
    if (!string.IsNullOrEmpty(data.m_requiredPersistentEvent)) // not tracked by this mod, don't spawn to be safe
      return false;
    if (!data.m_spawnAtDay && EnvMan.IsDay())
      return false;
    if (!data.m_spawnAtNight && EnvMan.IsNight())
      return false;
    // m_requiredEnvironments is ignored: weather is simulated by the clients and not known reliably on the server
    return true;
  }

  Vector3? FindSpawnPoint(SpawnSystem.SpawnData data, Vector3 playerPos, PeersEnumerable peers)
  {
    var radiusMin = data.m_spawnRadiusMin > 0 ? data.m_spawnRadiusMin : DefaultSpawnRadiusMin;
    var radiusMax = data.m_spawnRadiusMax > 0 ? data.m_spawnRadiusMax : DefaultSpawnRadiusMax;
    for (int i = 0; i < MaxSpawnPointTries; i++)
    {
      var dir = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(radiusMin, radiusMax);
      var pos = playerPos + new Vector3(dir.x, 0, dir.y);
      pos.y = GetHeight(pos);
      if (IsSpawnPointGood(data, pos, radiusMin, peers))
        return pos;
    }
    return null;
  }

  bool IsSpawnPointGood(SpawnSystem.SpawnData data, Vector3 pos, float minPlayerDistance, PeersEnumerable peers)
  {
    if ((data.m_biome & GetBiome(pos)) is 0)
      return false;
    if ((data.m_biomeArea & WorldGenerator.instance.GetBiomeArea(pos)) is 0)
      return false;

    var waterLevel = ZoneSystem.instance.m_waterLevel;
    var altitude = pos.y - waterLevel;
    if (altitude < data.m_minAltitude || altitude > data.m_maxAltitude)
      return false;
    if (data.m_minOceanDepth != data.m_maxOceanDepth && (-altitude < data.m_minOceanDepth || -altitude > data.m_maxOceanDepth))
      return false;

    var distanceFromCenter = Utils.LengthXZ(pos);
    if (data.m_minDistanceFromCenter > 0 && distanceFromCenter < data.m_minDistanceFromCenter)
      return false;
    if (data.m_maxDistanceFromCenter > 0 && distanceFromCenter > data.m_maxDistanceFromCenter)
      return false;

    var inForest = WorldGenerator.InForest(pos);
    if ((inForest && !data.m_inForest) || (!inForest && !data.m_outsideForest))
      return false;

    var inLava = GetHeightmap(pos).IsLava(pos, 0.6f);
    if ((inLava && !data.m_inLava) || (!inLava && !data.m_outsideLava))
      return false;

    if (!data.m_canSpawnCloseToPlayer)
    {
      foreach (var peer in peers)
      {
        if (Utils.DistanceXZ(peer.RefPos, pos) < minPlayerDistance)
          return false;
      }
    }

    foreach (var zdo in _sectorObjects)
    {
      var prefab = zdo.GetPrefab();
      if (prefab == __vilePrefab)
      {
        if (data.m_spawnDistance > 0 && Utils.DistanceXZ(zdo.GetPosition(), pos) < data.m_spawnDistance)
          return false;
      }
      else if (!data.m_insidePlayerBase && _noSpawnAreaRadiusByPrefab.TryGetValue(prefab, out var radius) &&
        Utils.DistanceXZ(zdo.GetPosition(), pos) < radius)
      {
        return false;
      }
    }

    return true;
  }

  void SpawnGroup(SpawnSystem.SpawnData data, Vector3 spawnPoint)
  {
    var count = UnityEngine.Random.Range(data.m_groupSizeMin, data.m_groupSizeMax + 1);
    for (int i = 0; i < count; i++)
    {
      var pos = spawnPoint;
      if (i > 0)
      {
        var offset = UnityEngine.Random.insideUnitCircle * data.m_groupRadius;
        pos += new Vector3(offset.x, 0, offset.y);
        pos.y = GetHeight(pos);
      }
      pos.y += data.m_groundOffset + UnityEngine.Random.Range(0f, data.m_groundOffsetRandom);

      var level = data.m_minLevel;
      var levelUpChance = SpawnSystem.GetLevelUpChance(pos, data);
      while (level < data.m_maxLevel && UnityEngine.Random.Range(0f, 100f) <= levelUpChance)
        level++;

      var zdo = Spawn(__vilePrefab, pos, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0));
      if (level > 1)
        zdo.Vars.SetLevel(level);
      if (data.m_huntPlayer)
        zdo.ZDO.Set(ZDOVars.s_huntPlayer, true);
      _sectorObjects.Add(zdo.ZDO);
    }
    Logger.DevLog($"Spawned {count} {VilePrefabName} at {spawnPoint}");
  }
}
