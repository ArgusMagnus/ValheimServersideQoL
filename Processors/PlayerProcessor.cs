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
        public DateTimeOffset LastUpdated { get; set; }
        public float ResetStamina { get; set; } = float.NaN;
    }

    public override void Initialize()
    {
        base.Initialize();
        RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _playerData.TryRemove(zdo, out _);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Player is null)
            return false;

        if ((Config.Players.InfiniteBuildingStamina.Value || Config.Players.InfiniteFarmingStamina.Value) && Game.m_staminaRate > 0)
        {
            var setInfinite = false;
            var rightItem = zdo.Vars.GetRightItem();
            if (Config.Players.InfiniteBuildingStamina.Value && (rightItem == _hammerPrefab || rightItem == _hoePrefab))
                setInfinite = true;
            else if (Config.Players.InfiniteFarmingStamina.Value && (rightItem == _cultivatorPrefab || rightItem == _hoePrefab || rightItem == _scythePrefab))
                setInfinite = true;

            if (setInfinite)
            {
                var playerData = _playerData.GetOrAdd(zdo, static _ => new());
                if (DateTimeOffset.UtcNow - playerData.LastUpdated > TimeSpan.FromSeconds(2))
                {
                    if (float.IsNaN(playerData.ResetStamina))
                        playerData.ResetStamina = zdo.Vars.GetStamina();
                    RPC.UseStamina(zdo, -99000f);
                    playerData.LastUpdated = DateTimeOffset.UtcNow;
                }
            }
            else if (_playerData.TryGetValue(zdo, out var playerData) && !float.IsNaN(playerData.ResetStamina))
            {
                var stamina = zdo.Vars.GetStamina();
                var diff = stamina - playerData.ResetStamina;
                playerData.ResetStamina = float.NaN;
                if (diff > 0)
                    RPC.UseStamina(zdo, diff);
            }
        }

        if (!Config.Tames.TeleportFollow.Value)
            return false;

        if (Character.InInterior(zdo.GetPosition()))
            return false;

        var playerName = zdo.Vars.GetPlayerName();
        var playerZone = ZoneSystem.GetZone(zdo.GetPosition());

        foreach (var tameZdo in Instance<TameableProcessor>().Tames)
        {
            if (tameZdo.Vars.GetFollow() != playerName)
                continue;

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

        return false;
    }
}
