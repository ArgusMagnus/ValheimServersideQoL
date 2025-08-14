namespace Valheim.ServersideQoL.Processors;

[Processor(Priority = int.MaxValue)]
sealed class ManageOwnerProcessor : Processor
{
    readonly Dictionary<Vector2i, long> _bestOwners = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        if (!firstTime)
            return;
    }

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        _bestOwners.Clear();
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (!Config.Networking.ReassignOwnershipBasedOnConnectionQuality.Value || !zdo.Persistent ||
            zdo.PrefabInfo is { Player: not null } or { Container: not null } or { ItemDrop: not null })
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (peers.Count < 2)
            return false;

        if (!_bestOwners.TryGetValue(zdo.GetSector(), out var bestOwner))
        {
            var min = TimeSpan.MaxValue;
            foreach (var peer in peers)
            {
                if (Instance<PlayerProcessor>().GetPeerInfo(peer.m_uid) is not { } peerInfo)
                    continue;
                var value = peerInfo.PingMean + peerInfo.PingStdDev;
                if (value < min)
                {
                    min = value;
                    bestOwner = peer.m_uid;
                }
            }
            _bestOwners.Add(zdo.GetSector(), bestOwner);
        }

        if (bestOwner is 0)
            return false;

        if (!HasValidOwner(zdo, peers))
            zdo.SetOwner(bestOwner);
        return false;

        static bool HasValidOwner(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
        {
            if (!zdo.HasOwner())
                return false;
            foreach (var peer in peers)
            {
                if (zdo.GetOwner() == peer.m_uid)
                    return true;
            }
            return false;
        }
    }
}
