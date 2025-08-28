using System.Reflection;

namespace Valheim.ServersideQoL.Processors;

[Processor(Priority = int.MaxValue)]
sealed class ManageOwnerProcessor : Processor
{
    readonly Dictionary<Vector2i, long> _bestOwners = [];
    readonly MethodInfo _releaseNearbyZDOSMethod = typeof(ZDOMan).GetMethod("ReleaseNearbyZDOS", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    readonly MethodInfo _releaseNearbyZDOSPrefix = ((Delegate)ReleaseNearbyZDOSPrefix).Method;
    DateTimeOffset _maxOwnerTimestamp;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        Main.HarmonyInstance.Unpatch(_releaseNearbyZDOSMethod, _releaseNearbyZDOSPrefix);
        if (Config.Networking.ReassignOwnershipBasedOnConnectionQuality.Value && !Config.Networking.MeasurePing.Value)
        {
            var def = Config.Networking.ReassignOwnershipBasedOnConnectionQuality.Definition;
            var def2 = Config.Networking.MeasurePing.Definition;
            Logger.LogWarning($"Config option [{def.Section}].[{def.Key}] requires [{def.Section}].[{def.Key}] to be true, it does nothing otherwhise");
        }
        else if (Config.Networking.ReassignOwnershipBasedOnConnectionQuality.Value)
        {
            Main.HarmonyInstance.Patch(_releaseNearbyZDOSMethod, prefix: new(_releaseNearbyZDOSPrefix));
        }

        if (!firstTime)
            return;
    }

    static bool ReleaseNearbyZDOSPrefix() => false;

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        _bestOwners.Clear();
        _maxOwnerTimestamp = DateTimeOffset.UtcNow.AddSeconds(-2);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (!zdo.Persistent)
            return false;

        UnregisterZdoProcessor = true;
        long? owner = null;
        long? newOwner = null;

        if (Config.Networking.MeasurePing.Value && Config.Networking.ReassignOwnershipBasedOnConnectionQuality.Value)
        {
            UnregisterZdoProcessor = false;
            if (zdo.OwnerTimestamp < _maxOwnerTimestamp)
            {
                if (!_bestOwners.TryGetValue(zdo.GetSector(), out var bestOwner))
                {
                    var min = float.MaxValue;
                    foreach (var peer in peers)
                    {
                        if (Instance<PlayerProcessor>().GetPeerInfo(peer.m_uid) is not { } peerInfo)
                            continue;
                        if (peerInfo.ConnectionQuality < min)
                        {
                            min = peerInfo.ConnectionQuality;
                            bestOwner = peer.m_uid;
                        }
                    }
                    _bestOwners.Add(zdo.GetSector(), bestOwner);
                }

                owner = zdo.GetOwner();
                if (bestOwner is not 0 || !HasValidOwner(owner.Value, peers))
                    newOwner = bestOwner;
            }
        }

        if (peers.Count > 1 && (
            (Config.Networking.AssignInteractablesToClosestPlayer.Value && zdo.PrefabInfo is not { Smelter: null, CookingStation: null }) ||
            (Config.Networking.AssignMobsToClosestPlayer.Value && zdo.PrefabInfo.Humanoid is { MonsterAI.Value: not null } && !zdo.Vars.GetTamed())))
        {
            UnregisterZdoProcessor = false;
            long closestOwner = 0;
            var minDistSqr = float.MaxValue;
            foreach (var peer in peers)
            {
                if (Instance<PlayerProcessor>().GetPeerInfo(peer.m_uid) is not { } peerInfo)
                    continue;
                var distSqr = Utils.DistanceSqr(zdo.GetPosition(), peerInfo.PlayerZDO.GetPosition());
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    closestOwner = peer.m_uid;
                }
            }

            if (closestOwner is not 0)
                newOwner = closestOwner;
        }

        if (newOwner is not null && (owner ??= zdo.GetOwner()) != newOwner.Value)
            zdo.SetOwner(newOwner.Value);

        return false;
    }

    static bool HasValidOwner(long owner, IReadOnlyList<Peer> peers)
    {
        if (owner is 0)
            return false;
        foreach (var peer in peers)
        {
            if (owner == peer.m_uid)
                return true;
        }
        return false;
    }
}
