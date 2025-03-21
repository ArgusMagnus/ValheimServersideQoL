using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class PlayerProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    static readonly int _hammerPrefab = "Hammer".GetStableHashCode();
    static readonly int _hoePrefab = "Hoe".GetStableHashCode();
    static readonly int _cultivatorPrefab = "Cultivator".GetStableHashCode();

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        if (zdo.PrefabInfo.Player is null)
            return false;

        if (Config.Players.InfiniteStamina.Value || Config.Players.InfiniteBuildingStamina.Value || Config.Players.InfiniteFarmingStamina.Value)
        {
            var setInfinite = Config.Players.InfiniteStamina.Value;
            if (!setInfinite)
            {
                var rightItem = zdo.GetInt(ZDOVars.s_rightItem);
                if (Config.Players.InfiniteBuildingStamina.Value && (rightItem == _hammerPrefab || rightItem == _hoePrefab))
                    setInfinite = true;
                else if (Config.Players.InfiniteFarmingStamina.Value && (rightItem == _cultivatorPrefab || rightItem == _hoePrefab))
                    setInfinite = true;
            }

            if (setInfinite)
            {
                var stamina = zdo.GetFloat(ZDOVars.s_stamina);
                if (!float.IsPositiveInfinity(stamina))
                {
                    zdo.PlayerData.MaxStamina = Math.Max(stamina, zdo.PlayerData.MaxStamina);
                    if (stamina < zdo.PlayerData.MaxStamina * 0.9 && stamina > zdo.PlayerData.UpdateStaminaThreshold)
                    {
                        if (!Config.Players.InfiniteStamina.Value && float.IsNaN(zdo.PlayerData.ResetStamina))
                            zdo.PlayerData.ResetStamina = stamina;
                        zdo.PlayerData.UpdateStaminaThreshold = stamina;
                        ZRoutedRpc.instance.InvokeRoutedRPC(zdo.GetOwner(), zdo.m_uid, "UseStamina", float.NegativeInfinity);
                    }
                    else if (stamina > zdo.PlayerData.UpdateStaminaThreshold)
                        zdo.PlayerData.UpdateStaminaThreshold = 0;
                }
            }
            else if (!float.IsNaN(zdo.PlayerData.ResetStamina))
            {
                var stamina = zdo.GetFloat(ZDOVars.s_stamina);
                var diff = stamina - zdo.PlayerData.ResetStamina;
                zdo.PlayerData.ResetStamina = float.NaN;
                zdo.PlayerData.UpdateStaminaThreshold = 0;
                if (diff > 0)
                    ZRoutedRpc.instance.InvokeRoutedRPC(zdo.GetOwner(), zdo.m_uid, "UseStamina", diff);
            }
        }

        if (!Config.Tames.TeleportFollow.Value)
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
