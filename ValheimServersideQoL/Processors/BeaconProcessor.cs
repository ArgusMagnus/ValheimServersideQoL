namespace Valheim.ServersideQoL.Processors;

//sealed class BeaconProcessor : Processor
//{
//    readonly Dictionary<ExtendedZDO, ExtendedZDO> _zdosByBeacon = [];

//    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
//    {
//        if (_zdosByBeacon.TryGetValue(zdo, out var zdo2))
//        {
//            if (peers.Any(x => Utils.DistanceXZ(x.m_refPos, zdo.GetPosition()) < 4))
//            {
//                DestroyObject(zdo);
//                zdo2.Vars.SetBeaconFound(true);
//            }                
//            return false;
//        }

//        UnregisterZdoProcessor = true;
//        if (zdo.PrefabInfo.LocationProxy is null || zdo.Vars.GetBeaconFound())
//            return false;

//        var hash = zdo.Vars.GetLocation();
//        if (hash is 0)
//        {
//            UnregisterZdoProcessor = false;
//            return true;
//        }

//        if (!ZoneSystem.instance.GetLocationsByHash().TryGetValue(hash, out var location) || !location.m_prefab.IsValid)
//            return false;

//        if (!location.m_prefab.IsLoaded)
//        {
//            if (!location.m_prefab.IsLoading)
//                location.m_prefab.LoadAsync();
//            UnregisterZdoProcessor = false;
//            return false;
//        }

//        if (location.m_prefab.Asset.GetComponentInChildren<Teleport>() is null)
//            return false;

//        var pos = zdo.GetPosition();
//        pos.y -= 2;
//        _zdosByBeacon.Add(PlaceObject(pos, Prefabs.MountainRemainsBuried, 0), zdo);
//        /// optional: set <see cref="Beacon.m_range"/>
//        return false;
//    }
//}
