using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ServersideQoL.SuperWishbone;

[Processor(Id)]
public sealed class Processor : Processor<Processor.PrefabInfo>
{
  public const string Id = "82b4ec36-75f1-4924-a202-2934d4144915";

  public sealed record PrefabInfo : ProcessorPrefabInfo
  {
    public LocationProxy? LocationProxy { get; private set; }
    public bool IsBeacon { get; private set; }
    public ItemDrop? ItemDrop { get; private set; }
    public Character? Character { get; private set; }
    public Container? Container { get; private set; }
    public bool CanMove { get; private set; }

    public override bool IsValid
    {
      get
      {
        LocationProxy = PrefabInfo.GetComponent<LocationProxy>();
        IsBeacon = PrefabInfo.PrefabHash == BeaconPrefabHash;
        ItemDrop = PrefabInfo.GetComponent<ItemDrop>();
        Character = PrefabInfo.GetComponent<Character>();
        Container = PrefabInfo.GetComponent<Container>();
        CanMove = PrefabInfo.GetComponent<ZSyncTransform>() is { m_syncPosition: true };
        return true;
      }
    }
  }

  static int BeaconPrefabHash => Prefabs.MountainRemainsBuried;
  readonly Dictionary<GameObject, Config.AdvancedConfig.Entry> _entryByItemPrefab = [];
  readonly Dictionary<int, Config.AdvancedConfig.Entry> _entryByPrefabHash = [];
  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _zdosByBeacon = [];
  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _beaconsByZdo = [];

  static readonly ServerVar<bool> __beaconFoundVar = SuperWishbonePlugin.RegisterServerVar<bool>("BeaconState");

  protected override void Initialize()
  {
    Config.Instance.Advanced.ValueChanged -= OnAdvancedConfigChanged;

    foreach (var zdo in _zdosByBeacon.Keys)
      DestroyObject(zdo);
    _zdosByBeacon.Clear();

    _entryByPrefabHash.Clear();
    _entryByItemPrefab.Clear();

    if (Config.Instance.Advanced.Value.Entries is { } entries)
    {
      foreach (var entry in entries)
      {
        if (entry is not { PrefabNamePattern.Length: > 0, Enabled: true })
          continue;

        var pattern = ConvertToRegexPattern(entry.PrefabNamePattern);

        foreach (var go in ObjectDB.instance.m_items)
        {
          if (go.GetComponent<ItemDrop>() is null || !Regex.IsMatch(go.name, pattern))
            continue;

          if (!_entryByItemPrefab.TryAdd(go, entry) && entry != _entryByItemPrefab[go])
            Logger.LogDiagnosticWarning($"Item '{go.name}' matches multiple entries. Only the first is used.");
          Logger.LogDiagnosticInfo($"Placing beacons for item {go.name} (range: {GetRange(entry.Range)})");
        }

        foreach (var go in ZNetScene.instance.m_prefabs)
        {
          GameObject? item = null;
          if (!Regex.IsMatch(go.name, pattern))
          {
            if (_entryByItemPrefab.Count is 0)
              continue;

            item =
              go.GetComponentInChildren<CharacterDrop>()?.m_drops.FirstOrDefault(x => x.m_prefab is not null && _entryByItemPrefab.ContainsKey(x.m_prefab))?.m_prefab ??
              go.GetComponentInChildren<DropOnDestroyed>()?.m_dropWhenDestroyed.m_drops.FirstOrDefault(x => x.m_item is not null && _entryByItemPrefab.ContainsKey(x.m_item)).m_item ??
              (go.GetComponentInChildren<Pickable>()?.m_itemPrefab is { } x && _entryByItemPrefab.ContainsKey(x) ? x : null);
            if (item is null)
              continue;
          }

          var hash = go.name.GetStableHashCode();
          if (!_entryByPrefabHash.TryAdd(hash, entry) && entry != _entryByPrefabHash[hash])
            Logger.LogDiagnosticWarning($"Prefab '{go.name}' matches multiple entries. Only the first is used.");
          Logger.LogDiagnosticInfo($"Placing beacons for prefab {go.name} (range: {GetRange(entry.Range)}, item: {item?.name})");
        }
      }
    }

    Config.Instance.Advanced.ValueChanged += OnAdvancedConfigChanged;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (prefabInfo.IsBeacon && _zdosByBeacon.TryGetValue(zdo, out var zdo2))
    {
      if (peers.Any(x => Utils.DistanceXZ(x.RefPos, zdo.ZDO.GetPosition()) < 2))
      {
        DestroyObject(zdo);
        _zdosByBeacon.Remove(zdo);
        __beaconFoundVar.Set(zdo2, true);
      }
      return ScheduleReprocessing(0.1f);
    }
    else if (_beaconsByZdo.TryGetValue(zdo, out var beacon))
    {
      if (Utils.DistanceXZ(zdo.ZDO.GetPosition(), beacon.ZDO.GetPosition()) > 4)
      {
        beacon.ZDO.SetPosition(GetBeaconPos(zdo.ZDO.GetPosition(), false));
        ZDOMan.instance.ForceSendZDO(beacon.ZDO.m_uid);
      }
      return default;
    }
    else if (_entryByPrefabHash.TryGetValue(zdo.ZDO.GetPrefab(), out var cfg))
    {
      if (cfg.MinLevel > 1 && prefabInfo.Character is not null)
      {
        if (zdo.Vars.GetLevel() < cfg.MinLevel)
          return ProcessResult.UnregisterProcessor;
      }
      else if (cfg.MinQuality > 1 && prefabInfo.ItemDrop is not null)
      {
        var data = prefabInfo.ItemDrop.m_itemData.Clone();
        ItemDrop.LoadFromZDO(data, zdo.ZDO);
        if (data.m_quality < cfg.MinQuality)
          return ProcessResult.UnregisterProcessor;
      }

      return PlaceBeacon(zdo, prefabInfo, cfg.Range);
    }
    else if (prefabInfo.LocationProxy is not null)
    {
      if (Config.Instance is { FindDungeons.Value: false, FindVegvisir.Value: false })
        return ProcessResult.UnregisterProcessor;

      if (__beaconFoundVar.Get(zdo))
        return ProcessResult.UnregisterProcessor;

      var hash = zdo.Vars.GetLocation();
      if (hash is 0)
        return default;

      using var loc = ZoneSystem.instance.GetAndLoadLocationByHash(hash);
      if (!loc.IsValid)
        return ProcessResult.UnregisterProcessor;

      if (loc.Prefab is not { } prefab)
        return ScheduleReprocessing();

      List<RandomSpawn>? activeRandomSpawns = null;
      List<(Vector3, bool)>? beaconPositions = null;
      HashSet<GameObject>? objs = null;
      if (Config.Instance.FindDungeons.Value)
      {
        foreach (var c in prefab.GetComponentsInChildren<Teleport>())
        {
          if ((objs ??= []).Add(c.gameObject))
            AddBeaconPosition(ref beaconPositions, true, c, ref activeRandomSpawns, prefab, zdo);
        }
      }
      if (Config.Instance.FindVegvisir.Value)
      {
        foreach (var c in prefab.GetComponentsInChildren<Vegvisir>())
        {
          if ((objs ??= []).Add(c.gameObject))
            AddBeaconPosition(ref beaconPositions, false, c, ref activeRandomSpawns, prefab, zdo);
        }
      }

      if (beaconPositions is not { Count: > 0 })
        return ProcessResult.UnregisterProcessor;

      foreach (var (pos, isLocation) in beaconPositions)
      {
        beacon = PlaceObject(GetBeaconPos(pos, isLocation), BeaconPrefabHash, 0);
        beacon.Fields<Beacon>().Set(static () => x => x.m_range, GetRange(Config.Instance.Range.Value));
        _zdosByBeacon.Add(beacon, zdo);
      }
      return ProcessResult.UnregisterProcessor;
    }
    else if (prefabInfo.Container is { m_privacy: not Container.PrivacySetting.Private } && _entryByItemPrefab.Count is not 0)
    {
      var inventory = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo.Container).GetInventory();
      foreach (var item in inventory.Items)
      {
        if (_entryByItemPrefab.TryGetValue(item.m_dropPrefab, out cfg))
          return PlaceBeacon(zdo, prefabInfo, cfg.Range);
      }
      return ProcessResult.UnregisterProcessor;
    }
    else
    {
      return ProcessResult.UnregisterProcessor;
    }

    static void AddBeaconPosition(ref List<(Vector3, bool)>? positions, bool isLocation, Component? component, ref List<RandomSpawn>? activeRandomSpawns, GameObject location, ServersideQoLZDO zdo)
    {
      /// <see cref="ZoneSystem.SpawnProxyLocation"/>
      if (component is null)
        return;

      if (component.GetComponent<RandomSpawn>() is not { } randomSpawn)
      {
        var pos = zdo.ZDO.GetPosition() + zdo.ZDO.GetRotation() * component.gameObject.transform.position;
        (positions ??= []).Add((pos, isLocation));
        return;
      }

      if (activeRandomSpawns is null)
      {
        activeRandomSpawns = [];
        var randomSpawns = Utils.GetEnabledComponentsInChildren<RandomSpawn>(location);
        var state = UnityEngine.Random.state;
        UnityEngine.Random.InitState(zdo.Vars.GetSeed());
        Location? loc = null;
        foreach (var rs in randomSpawns)
        {
          var pos = rs.gameObject.transform.position;
          pos = zdo.ZDO.GetPosition() + zdo.ZDO.GetRotation() * pos;
          rs.Prepare();
          rs.Randomize(pos, loc ??= location.GetComponent<Location>());
          if (rs.gameObject.activeSelf)
            activeRandomSpawns.Add(rs);
          rs.Reset();
          rs.GetComponent<ZNetView>()?.gameObject.SetActive(true);
        }
        UnityEngine.Random.state = state;
      }

      if (activeRandomSpawns.Contains(randomSpawn))
      {
        var pos = zdo.ZDO.GetPosition() + zdo.ZDO.GetRotation() * randomSpawn.gameObject.transform.position;
        (positions ??= []).Add((pos, isLocation));
      }
    }
  }

  static float GetRange(float range)
  {
    if (range > 0)
      return range;
    return ZNet.instance.GetSyncedSimulationDistance().NearSimulationDistance * ZoneSystem.c_ZoneSize;// * Mathf.Sqrt(2);
  }

  static Vector3 GetBeaconPos(Vector3 pos, bool isLocation)
  {
    if (isLocation || Character.InInterior(pos) || pos.y < ZoneSystem.c_WaterLevel - 2)
      pos.y -= 4;
    else
      pos.y = GetHeight(pos) - 2;
    return pos;
  }

  ProcessResult PlaceBeacon(ServersideQoLZDO zdo, PrefabInfo prefabInfo, float range)
  {
    if (!prefabInfo.CanMove && __beaconFoundVar.Get(zdo))
      return ProcessResult.UnregisterProcessor;

    var beacon = PlaceObject(GetBeaconPos(zdo.ZDO.GetPosition(), false), BeaconPrefabHash, 0);
    beacon.Fields<Beacon>().Set(static () => x => x.m_range, GetRange(range));
    if (!prefabInfo.CanMove)
    {
      _zdosByBeacon.Add(beacon, zdo);
      return ProcessResult.UnregisterProcessor;
    }
    else
    {
      _beaconsByZdo.Add(zdo, beacon);
      zdo.Destroyed += zdo =>
      {
        if (_beaconsByZdo.Remove(zdo, out var beacon))
          DestroyObject(beacon);
      };
      return default;
    }
  }

  void OnAdvancedConfigChanged(ConfigBase.YamlConfigEntry<Config.AdvancedConfig> cfg)
  {
    Config.Instance.Advanced.ValueChanged -= OnAdvancedConfigChanged;
    RequestReinitialization();
  }
}
