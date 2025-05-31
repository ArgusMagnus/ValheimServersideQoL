using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class PlayerProcessor : Processor
{
    sealed class PlayerState
    {
        public int LastEmoteId { get; set; } = -1;
    }

    readonly Dictionary<ExtendedZDO, PlayerState> _playerStates = [];

    readonly Dictionary<ZDOID, ExtendedZDO> _players = [];
    public IReadOnlyDictionary<ZDOID, ExtendedZDO> Players => _players;
    public event Action<ExtendedZDO>? PlayerDestroyed;

    public ExtendedZDO? GetPeerCharacter(long peerID)
    {
        var id = peerID == ZDOMan.GetSessionID() ? Player.m_localPlayer?.GetZDOID() : ZNet.instance.GetPeer(peerID)?.m_characterID;
        return id is not null && _players.TryGetValue(id.Value, out var zdo) ? zdo : null;
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        UpdateRpcSubscription("SetTrigger", OnZSyncAnimationSetTrigger,
            (Config.Players.InfiniteBuildingStamina.Value || Config.Players.InfiniteFarmingStamina.Value || Config.Players.InfiniteMiningStamina.Value || Config.Players.InfiniteWoodCuttingStamina.Value)
            && Game.m_staminaRate > 0);

        //UpdateRpcSubscription("Say", OnTalkerSay, true);
        UpdateRpcSubscription("RPC_AnimateLever", RPC_AnimateLever,
            Config.Players.CanSacrificeCryptKey.Value ||
            Config.Players.CanSacrificeWishbone.Value ||
            Config.Players.CanSacrificeTornSpirit.Value);
    }

    void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _playerStates.Remove(zdo);
        if (_players.Remove(zdo.m_uid))
            PlayerDestroyed?.Invoke(zdo);
    }

    /// <see cref="ZSyncAnimation.SetTrigger(string)"/>
    void OnZSyncAnimationSetTrigger(ZRoutedRpc.RoutedRPCData data, string name)
    {
        if (!_players.TryGetValue(data.m_targetZDO, out var zdo))
            return;

        //Logger.DevLog($"ZDO {data.m_targetZDO}: SetTrigger: {name}");

        static bool CheckStamina(string triggerName, ModConfig.PlayersConfig cfg)
        {
            switch (triggerName)
            {
                case "swing_pickaxe":
                    return cfg.InfiniteMiningStamina.Value;
                case "swing_hammer":
                    return cfg.InfiniteBuildingStamina.Value;
                case "swing_hoe":
                case "scything":
                    return cfg.InfiniteFarmingStamina.Value;
                case "swing_axe0":
                case "battleaxe_attack0":
                case "dualaxes0":
                    return cfg.InfiniteWoodCuttingStamina.Value;
                default:
                    return false;
            }
        }

        if (!CheckStamina(name, Config.Players))
            return;

        var rightItem = ObjectDB.instance.GetItemPrefab(zdo.Vars.GetRightItem()).GetComponent<ItemDrop>();
        var requiredStamina = rightItem.m_itemData.m_shared.m_attack.m_attackStamina;
        if (zdo.Vars.GetStamina() < 2 * requiredStamina)
            RPC.UseStamina(zdo, -requiredStamina);
    }

    /// <see cref="Talker.Say(Talker.Type, string)"/>
    //void OnTalkerSay(ZRoutedRpc.RoutedRPCData data, int ctype, UserInfo user, string text)
    //{
    //    var type = (Talker.Type)ctype;
    //}

    void RPC_AnimateLever(ExtendedZDO zdo, ZRoutedRpc.RoutedRPCData data)
    {
        if (zdo.PrefabInfo.Container is not { Incinerator.Value: not null } || zdo.Inventory.TeleportTag is not null)
            return;

        ExtendedZDO? player = null;
        if (Config.Players.CanSacrificeCryptKey.Value && zdo.Inventory.Items.Any(x => x.m_dropPrefab?.name is PrefabNames.CryptKey))
        {
            player ??= GetPeerCharacter(data.m_senderPeerID);
            if (player is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedCryptKey(player.Vars.GetPlayerID(), true);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, "You were permanently granted the ability to open sunken crypt doors");
            }
        }
        if (Config.Players.CanSacrificeWishbone.Value && zdo.Inventory.Items.Any(x => x.m_dropPrefab?.name is PrefabNames.Wishbone))
        {
            player ??= GetPeerCharacter(data.m_senderPeerID);
            if (player is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedWishbone(player.Vars.GetPlayerID(), true);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, "You were permanently granted the ability to sense hidden objects");
                RPC.AddStatusEffect(player, StatusEffects.Wishbone);
            }
        }
        if (Config.Players.CanSacrificeTornSpirit.Value && zdo.Inventory.Items.Any(x => x.m_dropPrefab?.name is PrefabNames.TornSpirit))
        {
            player ??= GetPeerCharacter(data.m_senderPeerID);
            if (player is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedTornSpirit(player.Vars.GetPlayerID(), true);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, "You were permanently granted a wisp companion");
                RPC.AddStatusEffect(player, StatusEffects.Demister);
            }
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        if (zdo.PrefabInfo.Player is null)
            return false;

        if (_players.TryAdd(zdo.m_uid, zdo))
        {
            zdo.Destroyed += OnZdoDestroyed;
            var playerID = zdo.Vars.GetPlayerID();
            if (Config.Players.CanSacrificeWishbone.Value && DataZDO.Vars.GetSacrifiedWishbone(playerID))
                RPC.AddStatusEffect(zdo, StatusEffects.Wishbone);
            if (Config.Players.CanSacrificeTornSpirit.Value && DataZDO.Vars.GetSacrifiedTornSpirit(playerID))
                RPC.AddStatusEffect(zdo, StatusEffects.Demister);
        }

        if (Config.Players.InfiniteEncumberedStamina.Value && zdo.Vars.GetIsEncumbered() && zdo.Vars.GetStamina() < zdo.PrefabInfo.Player.m_encumberedStaminaDrain)
        {
            RPC.UseStamina(zdo, -zdo.PrefabInfo.Player.m_encumberedStaminaDrain);
        }

        if (Config.Players.StackInventoryIntoContainersEmote.Value is not ModConfig.PlayersConfig.DisabledEmote)
        {
            if (!_playerStates.TryGetValue(zdo, out var state))
                _playerStates.Add(zdo, state = new());
            /// <see cref="Emote.DoEmote(Emotes)"/> <see cref="Player.StartEmote(string, bool)"/>
            if (zdo.Vars.GetEmoteID() is var emoteId && emoteId != state.LastEmoteId)
            {
                state.LastEmoteId = emoteId;
                if (Config.Players.StackInventoryIntoContainersEmote.Value is ModConfig.PlayersConfig.AnyEmote || zdo.Vars.GetEmote() == Config.Players.StackInventoryIntoContainersEmote.Value)
                {
                    foreach (var containerZdo in Instance<ContainerProcessor>().Containers)
                    {
                        //if (containerZdo.Vars.GetInUse() || !CheckMinDistance(peers, containerZdo))
                        //    continue; // in use or player to close

                        var pickupRangeSqr = containerZdo.Inventory.PickupRange ?? Config.Containers.AutoPickupRange.Value;
                        pickupRangeSqr *= pickupRangeSqr;

                        if (pickupRangeSqr is 0f || Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) > pickupRangeSqr)
                            continue;

                        if (containerZdo.PrefabInfo.Container!.Value.Container.m_privacy is Container.PrivacySetting.Private && containerZdo.Vars.GetCreator() != zdo.Vars.GetPlayerID())
                            continue; // private container
                        
                        RPC.RequestStack(containerZdo, zdo);
                    }
                }
            }
        }

        if (!Config.Tames.TeleportFollow.Value)
            return false;

        if (Character.InInterior(zdo.GetPosition()))
            return false;

        var playerName = zdo.Vars.GetPlayerName();
        var playerZone = ZoneSystem.GetZone(zdo.GetPosition());

        foreach (var tameState in Instance<TameableProcessor>().Tames)
        {
            if (!tameState.IsTamed || tameState.ZDO.Vars.GetFollow() != playerName)
                continue;

            var tameZone = ZoneSystem.GetZone(tameState.ZDO.GetPosition());
            if (ZNetScene.InActiveArea(tameZone, playerZone))
                continue;

            /// <see cref="TeleportWorld.Teleport"/>
            var direction = zdo.GetRotation() * Vector3.forward;
            direction = Quaternion.Euler(0, UnityEngine.Random.Range(-45f, 45f), 0) * direction * UnityEngine.Random.Range(1f, 4f);
            var targetPos = zdo.GetPosition() + direction;
            targetPos.y += UnityEngine.Random.Range(1, 3);
            var owner = tameState.ZDO.GetOwner();
            tameState.ZDO.ClaimOwnershipInternal();
            tameState.ZDO.SetPosition(targetPos);
            tameState.ZDO.SetOwnerInternal(owner);
        }

        return false;
    }
}
