using System.Reflection;

namespace Valheim.ServersideQoL.Processors;

[Processor(Priority = int.MaxValue)]
sealed class ManageOwnerProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("9178b9fe-ef1a-4e75-b492-ae392aa6b557");

    readonly Dictionary<Vector2i, (PlayerProcessor.IPeerInfo BestOwner, HashSet<long> ValidOwners)> _zoneData = [];
    readonly Stack<HashSet<long>> _hashsetCache = [];
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
        foreach (var (_, set) in _zoneData.Values)
        {
            set.Clear();
            _hashsetCache.Push(set);
        }
        _zoneData.Clear();
        _maxOwnerTimestamp = DateTimeOffset.UtcNow.AddSeconds(-2);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (!zdo.Persistent)
            return false;

        UnregisterZdoProcessor = true;
        PlayerProcessor.IPeerInfo? owner = null;
        PlayerProcessor.IPeerInfo? newOwner = null;

        if (Config.Networking.MeasurePing.Value && Config.Networking.ReassignOwnershipBasedOnConnectionQuality.Value)
        {
            UnregisterZdoProcessor = false;

            if (zdo.OwnerTimestamp < _maxOwnerTimestamp)
            {
                owner = zdo.OwnerPeerInfo;
                if (!_zoneData.TryGetValue(zdo.GetSector(), out var data))
                {
                    if (!_hashsetCache.TryPop(out data.ValidOwners))
                        data.ValidOwners = [];
                    var min = float.MaxValue;
                    foreach (var peer in peers.AsEnumerable())
                    {
                        data.ValidOwners.Add(peer.m_uid);
                        if (Instance<PlayerProcessor>().GetPeerInfo(peer.m_uid) is not { } peerInfo)
                            continue;
                        if (peerInfo.ConnectionQuality < min)
                        {
                            min = peerInfo.ConnectionQuality;
                            data.BestOwner = peerInfo;
                        }
                    }

                    _zoneData.Add(zdo.GetSector(), data);
                }

                if (data.BestOwner is not null && (owner is null || !data.ValidOwners.Contains(owner.Owner) || SwitchOwner(owner, data.BestOwner)))
                    newOwner = data.BestOwner;
            }
        }

        if (peers.Count > 1 && (
            (Config.Networking.AssignInteractablesToClosestPlayer.Value && zdo.PrefabInfo is not { Smelter: null, CookingStation: null }) ||
            (Config.Networking.AssignMobsToClosestPlayer.Value && zdo.PrefabInfo.Humanoid is { MonsterAI.Value: not null } && !zdo.Vars.GetTamed())))
        {
            UnregisterZdoProcessor = false;
            if (zdo.OwnerTimestamp < _maxOwnerTimestamp)
            {
                PlayerProcessor.IPeerInfo? closestOwner = null;
                var minDistSqr = float.MaxValue;
                foreach (var peer in peers.AsEnumerable())
                {
                    if (Instance<PlayerProcessor>().GetPeerInfo(peer.m_uid) is not { } peerInfo)
                        continue;
                    var distSqr = Utils.DistanceSqr(zdo.GetPosition(), peerInfo.PlayerZDO.GetPosition());
                    if (distSqr < minDistSqr)
                    {
                        minDistSqr = distSqr;
                        closestOwner = peerInfo;
                    }
                }

                if (closestOwner is not null)
                    newOwner = closestOwner;
            }
        }

        if (newOwner is not null && !ReferenceEquals(owner ??= zdo.OwnerPeerInfo, newOwner))
            zdo.SetOwner(newOwner.Owner);

        return false;
    }

    bool SwitchOwner(PlayerProcessor.IPeerInfo currentOwner, PlayerProcessor.IPeerInfo bestCandidate)
    {
        var minDiff = Config.Networking.ReassignOwnershipConnectionQualityHysteresis.Value;
        if (minDiff <= 0f)
            return true;

        if (minDiff < 1f)
            minDiff = currentOwner.ConnectionQuality * minDiff;
        return currentOwner.ConnectionQuality - bestCandidate.ConnectionQuality >= minDiff;
    }
}
