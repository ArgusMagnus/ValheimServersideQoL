using BepInEx.Logging;
using System.Collections.Concurrent;

namespace Valheim.ServersideQoL.Processors;

sealed class PlayerProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly int _hammerPrefab = "Hammer".GetStableHashCode();
    readonly int _hoePrefab = "Hoe".GetStableHashCode();
    readonly int _cultivatorPrefab = "Cultivator".GetStableHashCode();
    readonly int _scythePrefab = "Scythe".GetStableHashCode();

    readonly ConcurrentDictionary<ExtendedZDO, PlayerData> _playerData = new();

    sealed class PlayerData
    {
        public float MaxStamina { get; set; }
        public float UpdateStaminaThreshold { get; set; }
        public float ResetStamina { get; set; } = float.NaN;
    }

    public override void PreProcess()
    {
        base.PreProcess();
        foreach (var zdo in _playerData.Keys)
        {
            if (!zdo.IsValid() || zdo.PrefabInfo.Player is null)
                _playerData.TryRemove(zdo, out _);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        if (zdo.PrefabInfo.Player is null)
            return false;

        if ((Config.Players.InfiniteBuildingStamina.Value || Config.Players.InfiniteFarmingStamina.Value) && Game.m_staminaRate > 0)
        {
            var setInfinite = false;
            var rightItem = zdo.GetInt(ZDOVars.s_rightItem);
            if (Config.Players.InfiniteBuildingStamina.Value && (rightItem == _hammerPrefab || rightItem == _hoePrefab))
                setInfinite = true;
            else if (Config.Players.InfiniteFarmingStamina.Value && (rightItem == _cultivatorPrefab || rightItem == _hoePrefab || rightItem == _scythePrefab))
                setInfinite = true;

            PlayerData? playerData = null;
            if (setInfinite)
            {
                var stamina = zdo.GetFloat(ZDOVars.s_stamina);
                if (!float.IsPositiveInfinity(stamina))
                {
                    playerData ??= _playerData.GetOrAdd(zdo, static _ => new());
                    playerData.MaxStamina = Math.Max(stamina, playerData.MaxStamina);
                    if (stamina < playerData.MaxStamina * 0.9 && stamina > playerData.UpdateStaminaThreshold)
                    {
                        playerData.ResetStamina = stamina;
                        playerData.UpdateStaminaThreshold = stamina;
                        RPC.UseStamina(zdo, float.NegativeInfinity);
                    }
                    else if (stamina > playerData.UpdateStaminaThreshold)
                        playerData.UpdateStaminaThreshold = 0;
                }
            }
            else if (!float.IsNaN((playerData ??= _playerData.GetOrAdd(zdo, static _ => new())).ResetStamina))
            {
                var stamina = zdo.GetFloat(ZDOVars.s_stamina);
                var diff = stamina - playerData.ResetStamina;
                playerData.ResetStamina = float.NaN;
                playerData.UpdateStaminaThreshold = 0;
                if (diff > 0)
                    RPC.UseStamina(zdo, diff);
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
