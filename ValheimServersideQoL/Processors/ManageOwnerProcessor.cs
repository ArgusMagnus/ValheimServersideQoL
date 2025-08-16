using System.Reflection;

namespace Valheim.ServersideQoL.Processors;

[Processor(Priority = int.MaxValue)]
sealed class ManageOwnerProcessor : Processor
{
    readonly Dictionary<Vector2i, long> _bestOwners = [];
    readonly MethodInfo _releaseZDOSMethod = typeof(ZDOMan).GetMethod("ReleaseZDOS", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    readonly MethodInfo _releaseZDOSPrefix = ((Delegate)ReleaseZDOSPrefix).Method;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        Main.HarmonyInstance.Unpatch(_releaseZDOSMethod, _releaseZDOSPrefix);
        if (Config.Networking.ReassignOwnershipBasedOnConnectionQuality.Value && !Config.Networking.MeasurePing.Value)
        {
            var def = Config.Networking.ReassignOwnershipBasedOnConnectionQuality.Definition;
            var def2 = Config.Networking.MeasurePing.Definition;
            Logger.LogWarning($"Config option [{def.Section}].[{def.Key}] requires [{def.Section}].[{def.Key}] to be true, it does nothing otherwhise");
        }
        else if (Config.Networking.ReassignOwnershipBasedOnConnectionQuality.Value)
        {
            Logger.DevLog("Disabling ZDOMan.ReleaseZDOS");
            Main.HarmonyInstance.Patch(_releaseZDOSMethod, prefix: new(_releaseZDOSPrefix));
        }

        if (!firstTime)
            return;
    }

    static bool ReleaseZDOSPrefix() => false;

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        _bestOwners.Clear();
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        long? newOwner = null;

        if (Config.Networking.MeasurePing.Value && Config.Networking.ReassignOwnershipBasedOnConnectionQuality.Value &&
            zdo.Persistent && zdo.PrefabInfo is { Player: null })
        {
            UnregisterZdoProcessor = false;
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

            if ((bestOwner is not 0 && zdo.PreviousSetOwner == zdo.GetOwner()) || !HasValidOwner(zdo, peers))
                newOwner = bestOwner;

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

        if (peers.Count > 1 &&
            (Config.Networking.AssignInteractablesToClosestPlayer.Value && zdo.PrefabInfo is not { Smelter: null, CookingStation: null }) ||
            (Config.Networking.AssignMobsToClosestPlayer.Value && zdo.PrefabInfo.Humanoid is { MonsterAI.Value: not null } && !zdo.Vars.GetTamed()))
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

        if (newOwner is not null)
            zdo.SetOwner(newOwner.Value);

        return false;

    }
}
