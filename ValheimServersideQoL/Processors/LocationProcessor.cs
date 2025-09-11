using System.Text.RegularExpressions;

namespace Valheim.ServersideQoL.Processors;

sealed class LocationProcessor : Processor
{
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
            if (peers.Any(x => Utils.DistanceXZ(x.m_refPos, zdo.GetPosition()) < 4))
            {
                DestroyObject(zdo);
                _zdosByBeacon.Remove(zdo);
                zdo2.Vars.SetBeaconFound(true);
            }
            return false;
        }

        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.LocationProxy is null)
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
        if ((!Config.Wishbone.FindDungeons.Value || prefab.GetComponentInChildren<Teleport>() is null)
            && (!Config.Wishbone.FindVegvisir.Value || prefab.GetComponentInChildren<Vegvisir>() is null)
            && (_regex is null || !prefab.GetComponentsInChildren<ZNetView>().Any(x => _regex.IsMatch(Utils.GetPrefabName(x.gameObject)))))
            return false;

        var pos = zdo.GetPosition();
        pos.y -= 4;
        var beacon = PlaceObject(pos, Prefabs.MountainRemainsBuried, 0);
        beacon.Fields<Beacon>(true).Set(x => x.m_range, Minimap.instance.m_exploreRadius);
        _zdosByBeacon.Add(beacon, zdo);

        Logger.DevLog($"Beacon added for {prefab.name} at {pos} with range {Minimap.instance.m_exploreRadius}");
        return false;
    }
}
