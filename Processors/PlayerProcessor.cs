using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class PlayerProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    protected override void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.Player is null || !Config.Tames.TeleportFollow.Value)
            return;

        if (zdo.GetPosition() is { y: > 1000 })
            return; // player in dungeon

        var playerName = zdo.GetString(ZDOVars.s_playerName);

        if (!SharedState.FollowingTamesByPlayerName.TryGetValue(playerName, out var tames))
            return;

        var playerZone = ZoneSystem.GetZone(zdo.GetPosition());

        foreach (var tameZdoId in tames)
        {
            var tameZdo = ZDOMan.instance.GetZDO(tameZdoId);
            if (!tameZdo.IsValid())
            {
                tames.Remove(tameZdoId);
                continue;
            }

            var tameZone = ZoneSystem.GetZone(tameZdo.GetPosition());
            if (Math.Max(Math.Abs(tameZone.x - playerZone.x), Math.Abs(tameZone.y - playerZone.y)) <= ZoneSystem.instance.m_activeArea)
                continue;

            var targetPos = zdo.GetPosition();
            targetPos.x += UnityEngine.Random.Range(-4, 4);
            targetPos.z += UnityEngine.Random.Range(-4, 4);
            targetPos.y += UnityEngine.Random.Range(2, 4);
            var owner = tameZdo.GetOwner();
            tameZdo.ClaimOwnershipInternal();
            tameZdo.SetPosition(targetPos);
            tameZdo.SetOwnerInternal(owner);
            SharedState.DataRevisions[tameZdoId] = tameZdo.DataRevision;
        }

        if (tames is { Count: 0 })
            SharedState.FollowingTamesByPlayerName.TryRemove(playerName, out _);
    }
}