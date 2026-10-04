using ServersideQoL.Utilities;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ServersideQoL.SuperWishbone;

[Processor(Id)]
public sealed class LocationProxyProcessor : Processor<LocationProxyProcessor.PrefabInfo>
{
  public const string Id = "82b4ec36-75f1-4924-a202-2934d4144915";

  public sealed record PrefabInfo : ProcessorPrefabInfo
  {
    public LocationProxy? LocationProxy { get; private set; }
    public bool IsBeacon { get; private set; }
    public ItemDrop? ItemDrop { get; private set; }
    public Character? Character { get; private set; }
    public override bool IsValid
    {
      get
      {
        LocationProxy = PrefabInfo.GetComponent<LocationProxy>();
        IsBeacon = PrefabInfo.PrefabHash == BeaconPrefabHash;
        ItemDrop = PrefabInfo.GetComponent<ItemDrop>();
        Character = PrefabInfo.GetComponent<Character>();
        return true;
      }
    }
  }

  static int BeaconPrefabHash => Prefabs.MountainRemainsBuried;
  readonly Dictionary<int, Config.AdvancedConfig.Entry> _advancedConfigEntries = [];
  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _zdosByBeacon = [];
  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _beaconsByZdo = [];

  static readonly ServerVar<bool> __beaconFoundVar = SuperWishbonePlugin.RegisterServerVar<bool>("BeaconState");

  protected override void Initialize()
  {
    Config.Instance.Advanced.ValueChanged -= OnAdvancedConfigChanged;

    foreach (var zdo in _zdosByBeacon.Keys)
      DestroyObject(zdo);
    _zdosByBeacon.Clear();

    _advancedConfigEntries.Clear();

    if (Config.Instance.Advanced.Value.Entries is { } entries)
    {
      foreach (var entry in entries)
      {
        if (entry is not { PrefabNamePattern.Length: > 0, Enabled: true })
          continue;

        var pattern = ConvertToRegexPattern(entry.PrefabNamePattern);
        foreach (var go in ZNetScene.instance.m_prefabs)
        {
          if (!Regex.IsMatch(go.name, pattern))
            continue;

          var hash = go.name.GetStableHashCode();
          if (!_advancedConfigEntries.TryAdd(hash, entry))
            Logger.LogWarning($"Prefab '{go.name}' matches multiple entries. Only the first is used.");
          Logger.LogInfo($"Placing beacons for '{go.name}'");
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
      beacon.ZDO.SetPosition(GetBeaconPos(zdo.ZDO.GetPosition()));
      ZDOMan.instance.ForceSendZDO(beacon.ZDO.m_uid);
      return default;
    }
    else if (_advancedConfigEntries.TryGetValue(zdo.ZDO.GetPrefab(), out var cfg))
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

      var canMove = prefabInfo.PrefabInfo.GetComponent<ZSyncTransform>() is { m_syncPosition: true };
      if (!canMove && __beaconFoundVar.Get(zdo))
        return ProcessResult.UnregisterProcessor;

      beacon = PlaceObject(GetBeaconPos(zdo.ZDO.GetPosition()), BeaconPrefabHash, 0);
      beacon.Fields<Beacon>().Set(static () => x => x.m_range, cfg.Range);
      if (!canMove)
      {
        _zdosByBeacon.Add(beacon, zdo);
        return ProcessResult.UnregisterProcessor;
      }
      else
      {
        _beaconsByZdo.Add(zdo, beacon);
        zdo.Destroyed += zdo =>
        {
          if (_beaconsByZdo.TryGetValue(zdo, out var beacon))
            beacon.Destroy();
        };
        return default;
      }
    }
    else if (prefabInfo.LocationProxy is not null)
    {
      if (Config.Instance.Range.Value <= 0)
        return ProcessResult.UnregisterProcessor;

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
      List<Vector3>? beaconPositions = null;
      HashSet<GameObject>? objs = null;
      if (Config.Instance.FindDungeons.Value)
      {
        foreach (var c in prefab.GetComponentsInChildren<Teleport>())
        {
          if ((objs ??= []).Add(c.gameObject))
            AddBeaconPosition(ref beaconPositions, c, ref activeRandomSpawns, prefab, zdo);
        }
      }
      if (Config.Instance.FindVegvisir.Value)
      {
        foreach (var c in prefab.GetComponentsInChildren<Vegvisir>())
        {
          if ((objs ??= []).Add(c.gameObject))
            AddBeaconPosition(ref beaconPositions, c, ref activeRandomSpawns, prefab, zdo);
        }
      }

      if (beaconPositions is not { Count: > 0 })
        return ProcessResult.UnregisterProcessor;

      foreach (var pos in beaconPositions)
      {
        beacon = PlaceObject(GetBeaconPos(pos), BeaconPrefabHash, 0);
        beacon.Fields<Beacon>().Set(static () => x => x.m_range, Config.Instance.Range.Value);
        _zdosByBeacon.Add(beacon, zdo);
      }
      return ProcessResult.UnregisterProcessor;
    }
    else
    {
      return ProcessResult.UnregisterProcessor;
    }

    static Vector3 GetBeaconPos(Vector3 pos)
    {
      if (Character.InInterior(pos) || pos.y < ZoneSystem.c_WaterLevel - 2)
        pos.y -= 4;
      else
        pos.y = GetHeight(pos) - 2;
      return pos;
    }

    static void AddBeaconPosition(ref List<Vector3>? positions, Component? component, ref List<RandomSpawn>? activeRandomSpawns, GameObject location, ServersideQoLZDO zdo)
    {
      /// <see cref="ZoneSystem.SpawnProxyLocation"/>
      if (component is null)
        return;

      if (component.GetComponent<RandomSpawn>() is not { } randomSpawn)
      {
        var pos = zdo.ZDO.GetPosition() + zdo.ZDO.GetRotation() * component.gameObject.transform.position;
        (positions ??= []).Add(pos);
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
        (positions ??= []).Add(pos);
      }
    }
  }

  void OnAdvancedConfigChanged(ConfigBase.YamlConfigEntry<Config.AdvancedConfig> cfg)
  {
    if (!Config.Instance.Enabled.Value)
    {
      Config.Instance.Advanced.ValueChanged -= OnAdvancedConfigChanged;
      return;
    }

    RequestReinitialization();
  }
}
