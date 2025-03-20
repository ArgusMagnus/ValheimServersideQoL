﻿using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class PlayerProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
	protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        if (zdo.PrefabInfo.Player is null || !Config.Tames.TeleportFollow.Value)
            return false;

        if (zdo.GetPosition() is { y: > 1000 })
            return false; // player in dungeon

        var playerName = zdo.GetString(ZDOVars.s_playerName);

        if (!SharedProcessorState.FollowingTamesByPlayerName.TryGetValue(playerName, out var tames))
            return false;

        var playerZone = ZoneSystem.GetZone(zdo.GetPosition());

        foreach (var tameZdoId in tames)
        {
            var tameZdo = (ExtendedZDO)ZDOMan.instance.GetZDO(tameZdoId);
            if (!tameZdo.IsValid() || tameZdo.GetString(ZDOVars.s_follow) != playerName)
            {
                tames.Remove(tameZdoId);
                continue;
            }

            var tameZone = ZoneSystem.GetZone(tameZdo.GetPosition());
            if (Math.Max(Math.Abs(tameZone.x - playerZone.x), Math.Abs(tameZone.y - playerZone.y)) <= ZoneSystem.instance.m_activeArea)
                continue;

            /// Maybe take inspiration from <see cref="TeleportWorld.Teleport"/> for better placement
            var targetPos = zdo.GetPosition();
            targetPos.x += UnityEngine.Random.Range(-4, 4);
            targetPos.z += UnityEngine.Random.Range(-4, 4);
            targetPos.y += UnityEngine.Random.Range(0, 3);
            var owner = tameZdo.GetOwner();
            tameZdo.ClaimOwnershipInternal();
            tameZdo.SetPosition(targetPos);
            tameZdo.SetOwnerInternal(owner);
        }

        if (tames is { Count: 0 })
            SharedProcessorState.FollowingTamesByPlayerName.TryRemove(playerName, out _);

        return false;
    }
}
