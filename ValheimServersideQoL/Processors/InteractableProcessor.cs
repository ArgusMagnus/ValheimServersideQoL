
namespace Valheim.ServersideQoL.Processors;

sealed class InteractableProcessor : Processor
{
    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (!Config.World.AssignInteractableOwnershipToClosestPeer.Value || zdo.PrefabInfo is not { Smelter: not null } and not { CookingStation: not null })
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (peers.Count <= 1)
            return false;

        Peer closestPeer = default;
        var minDistSqr = Config.General.MinPlayerDistance.Value * Config.General.MinPlayerDistance.Value;
        foreach (var peer in peers)
        {
            var distSqr = Utils.DistanceSqr(zdo.GetPosition(), peer.m_refPos);
            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                closestPeer = peer;
            }
        }

        if (closestPeer.IsDefault)
            return false;

#if DEBUG
        if (zdo.GetOwner() == closestPeer.m_uid)
            return false;
        Logger.DevLog($"Setting owner of {zdo.PrefabInfo.PrefabName} {zdo.m_uid} to {closestPeer.m_uid}");
#endif

        zdo.SetOwner(closestPeer.m_uid);
        return false;
    }
}
