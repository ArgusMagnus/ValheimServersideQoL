using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ServersideQoL.SuperWishbone;

public sealed class LocationProxyProcessor : Processor<LocationProxyProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(LocationProxy? LocationProxy, Beacon? Beacon) : ProcessorPrefabInfo
  {
    public override bool IsValid => LocationProxy is not null || PrefabInfo.PrefabHash == BeaconPrefabHash;
  }

  static int BeaconPrefabHash => Prefabs.MountainRemainsBuried;
  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _zdosByBeacon = [];

  Regex? _regex;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (_zdosByBeacon.TryGetValue(zdo, out var zdo2))
    {
      if (peers.Any(x => Utils.DistanceXZ(x.RefPos, zdo.ZDO.GetPosition()) < 2))
      {
        DestroyObject(zdo);
        _zdosByBeacon.Remove(zdo);
        SetBeaconFound(zdo2, true);
      }
      return ProcessResult.ScheduleReprocessing;
    }


    if (prefabInfo.LocationProxy is null || Config.Instance.Range.Value <= 0)
      return ProcessResult.UnregisterProcessor;

    if (!Config.Instance.FindDungeons.Value && !Config.Instance.FindVegvisir.Value && _regex is null)
      return ProcessResult.UnregisterProcessor;

    if (GetBeaconFound(zdo))
      return ProcessResult.UnregisterProcessor;

    var hash = zdo.Vars.GetLocation();
    if (hash is 0)
      return default;

    if (!ZoneSystem.instance.GetLocationsByHash().TryGetValue(hash, out var location) || !location.m_prefab.IsValid)
      return ProcessResult.UnregisterProcessor;

    if (!location.m_prefab.IsLoaded)
    {
      if (!location.m_prefab.IsLoading)
        location.m_prefab.LoadAsync();
      return ProcessResult.ScheduleReprocessing;
    }

    var prefab = location.m_prefab.Asset;
    var position = prefab.gameObject.transform.position;
    var rotation = prefab.gameObject.transform.rotation;
    prefab.gameObject.transform.position = Vector3.zero;
    prefab.gameObject.transform.rotation = Quaternion.identity;

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
    if (_regex is not null)
    {
      foreach (var c in prefab.GetComponentsInChildren<Component>())
      {
        if ((objs ??= []).Add(c.gameObject) && _regex.IsMatch(Utils.GetPrefabName(c.gameObject)))
          AddBeaconPosition(ref beaconPositions, c, ref activeRandomSpawns, prefab, zdo);
      }
    }

    prefab.gameObject.transform.position = position;
    prefab.gameObject.transform.rotation = rotation;

    if (beaconPositions is not { Count: > 0 })
      return ProcessResult.UnregisterProcessor;

    foreach (var pos in beaconPositions)
    {
      var p = pos;
      p.y -= 4;
      var beacon = PlaceObject(p, BeaconPrefabHash, 0);
      beacon.Fields<Beacon>().Set(static () => x => x.m_range, Config.Instance.Range.Value);
      _zdosByBeacon.Add(beacon, zdo);
    }

    return ProcessResult.UnregisterProcessor;

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

  static int __beaconFound = SuperWishbonePlugin.RegisterServerVar("BeaconState");
  static bool GetBeaconFound(ServersideQoLZDO zdo, bool defaultValue = default) => zdo.ZDO.GetBool(__beaconFound, defaultValue);
  static void SetBeaconFound(ServersideQoLZDO zdo, bool value) => zdo.ZDO.Set(__beaconFound, value);
}
