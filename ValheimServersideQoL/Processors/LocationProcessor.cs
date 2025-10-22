using System.Text.RegularExpressions;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class LocationProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("fbacfa56-3fd4-408c-aac7-cd39663d4ea2");

    readonly Dictionary<ExtendedZDO, ExtendedZDO> _zdosByBeacon = [];
    Regex? _regex;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        _regex = null;
        var pattern = Config.Wishbone.FindLocationObjectRegex.Value.Trim();
        if (!string.IsNullOrEmpty(pattern))
        {
            try { _regex = new(pattern); }
            catch (Exception ex)
            {
                Logger.LogError($"Invalid regex pattern: {pattern}");
                Logger.LogError(ex);
            }
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (_zdosByBeacon.TryGetValue(zdo, out var zdo2))
        {
            if (peers.Any(x => Utils.DistanceXZ(x.m_refPos, zdo.GetPosition()) < 2))
            {
                DestroyObject(zdo);
                _zdosByBeacon.Remove(zdo);
                zdo2.Vars.SetBeaconFound(true);
            }
            return false;
        }

        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.LocationProxy is null || Config.Wishbone.Range.Value <= 0)
            return false;

        if (!Config.Wishbone.FindDungeons.Value && !Config.Wishbone.FindVegvisir.Value && _regex is null)
            return false;

        if (zdo.Vars.GetBeaconFound())
            return false;

        var hash = zdo.Vars.GetLocation();
        if (hash is 0)
        {
            UnregisterZdoProcessor = false;
            return true;
        }

        if (!ZoneSystem.instance.GetLocationsByHash().TryGetValue(hash, out var location) || !location.m_prefab.IsValid)
            return false;

        if (!location.m_prefab.IsLoaded)
        {
            if (!location.m_prefab.IsLoading)
                location.m_prefab.LoadAsync();
            UnregisterZdoProcessor = false;
            return false;
        }

        var prefab = location.m_prefab.Asset;
        var position = prefab.gameObject.transform.position;
        var rotation = prefab.gameObject.transform.rotation;
        prefab.gameObject.transform.position = Vector3.zero;
        prefab.gameObject.transform.rotation = Quaternion.identity;

        List<RandomSpawn>? activeRandomSpawns = null;
        List<Vector3>? beaconPositions = null;
        HashSet<GameObject>? objs = null;
        if (Config.Wishbone.FindDungeons.Value)
        {
            foreach (var c in prefab.GetComponentsInChildren<Teleport>())
            {
                if ((objs ??= []).Add(c.gameObject))
                    AddBeaconPosition(ref beaconPositions, c, ref activeRandomSpawns, prefab, zdo);
            }
        }
        if (Config.Wishbone.FindVegvisir.Value)
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
            return false;

        foreach (var pos in beaconPositions)
        {
            var p = pos;
            p.y -= 4;
            var beacon = PlaceObject(p, Prefabs.MountainRemainsBuried, 0);
            beacon.Fields<Beacon>(true).Set(x => x.m_range, Config.Wishbone.Range.Value);
            _zdosByBeacon.Add(beacon, zdo);
        }

        return false;

        static void AddBeaconPosition(ref List<Vector3>? positions, Component? component, ref List<RandomSpawn>? activeRandomSpawns, GameObject location, ExtendedZDO zdo)
        {
            /// <see cref="ZoneSystem.SpawnProxyLocation"/>
            if (component is null)
                return;

            if (component.GetComponent<RandomSpawn>() is not { } randomSpawn)
            {
                var pos = zdo.GetPosition() + zdo.GetRotation() * component.gameObject.transform.position;
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
                    pos = zdo.GetPosition() + zdo.GetRotation() * pos;
                    rs.Prepare();
                    rs.Randomize(pos, loc ??= location.GetComponent<Location>());
                    //Main.Instance.Logger.DevLog($"Random spawn: {rs.name}, active: {rs.gameObject.activeSelf}");
                    if (rs.gameObject.activeSelf)
                        activeRandomSpawns.Add(rs);
                    rs.Reset();
                    rs.GetComponent<ZNetView>()?.gameObject.SetActive(true);
                }
                UnityEngine.Random.state = state;
            }

            if (activeRandomSpawns.Contains(randomSpawn))
            {
                var pos = zdo.GetPosition() + zdo.GetRotation() * randomSpawn.gameObject.transform.position;
                Main.Instance.Logger.DevLog($"Add random spawn beacon: {component.name}, {pos}");
                (positions ??= []).Add(pos);
            }
        }
    }

}
