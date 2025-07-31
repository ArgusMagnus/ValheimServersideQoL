using System.Reflection;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class PlayerProcessor : Processor
{
    sealed class PlayerState
    {
        public int LastEmoteId { get; set; } = 0; // Ignore first 'Sit' when logging in
        public Vector3? InitialInInteriorPosition { get; set; }
    }

    readonly Dictionary<ExtendedZDO, PlayerState> _playerStates = [];

    readonly Dictionary<ZDOID, ExtendedZDO> _players = [];
    public IReadOnlyDictionary<ZDOID, ExtendedZDO> Players => _players;
    readonly Dictionary<long, ExtendedZDO> _playersByID = [];
    public IReadOnlyDictionary<long, ExtendedZDO> PlayersByID => _playersByID;
    public event Action<ExtendedZDO>? PlayerDestroyed;

    sealed record StackContainerState(ExtendedZDO PlayerZDO)
    {
        public DateTimeOffset RemoveAfter { get; set; } = DateTimeOffset.UtcNow.AddSeconds(20);
        public bool Stacked { get; set; }
    }

    readonly Dictionary<ExtendedZDO, StackContainerState> _stackContainers = [];

    public ExtendedZDO? GetPeerCharacter(long peerID)
    {
        var id = peerID == ZDOMan.GetSessionID() ? Player.m_localPlayer?.GetZDOID() : ZNet.instance.GetPeer(peerID)?.m_characterID;
        return id is not null && _players.TryGetValue(id.Value, out var zdo) ? zdo : null;
    }

    static readonly MethodInfo __everybodyIsTryingToSleepMethod = typeof(Game).GetMethod("EverybodyIsTryingToSleep", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly MethodInfo __everybodyIsTryingToSleepPrefix = typeof(PlayerProcessor).GetMethod(nameof(EverybodyIsTryingToSleepPrefix), BindingFlags.NonPublic | BindingFlags.Static);

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        UpdateRpcSubscription("SetTrigger", OnZSyncAnimationSetTrigger,
            (Config.Players.InfiniteBuildingStamina.Value || Config.Players.InfiniteFarmingStamina.Value || Config.Players.InfiniteMiningStamina.Value || Config.Players.InfiniteWoodCuttingStamina.Value)
            && Game.m_staminaRate > 0);

        //UpdateRpcSubscription("Say", OnTalkerSay, true);
        UpdateRpcSubscription("RPC_AnimateLever", RPC_AnimateLever,
            Config.Players.CanSacrificeMegingjord.Value ||
            Config.Players.CanSacrificeCryptKey.Value ||
            Config.Players.CanSacrificeWishbone.Value ||
            Config.Players.CanSacrificeTornSpirit.Value);

        Main.HarmonyInstance.Unpatch(__everybodyIsTryingToSleepMethod, __everybodyIsTryingToSleepPrefix);
        if (Config.Sleeping.MinPlayersInBed.Value > 0)
            Main.HarmonyInstance.Patch(__everybodyIsTryingToSleepMethod, prefix: new(__everybodyIsTryingToSleepPrefix));

        if (!firstTime)
            return;

        _players.Clear();
        _playersByID.Clear();
        _playerStates.Clear();
    }

    void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _playerStates.Remove(zdo);
        if (_players.Remove(zdo.m_uid))
        {
            _playersByID.Remove(zdo.Vars.GetPlayerID());
            PlayerDestroyed?.Invoke(zdo);
        }
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
        if (zdo.PrefabInfo.Container is not { Incinerator.Value: not null } || zdo.Vars.GetIntTag() is not 0)
            return;

        ExtendedZDO? player = null;
        if (Config.Players.CanSacrificeMegingjord.Value && zdo.Inventory.Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.Megingjord))
        {
            player ??= GetPeerCharacter(data.m_senderPeerID);
            if (player is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedMegingjord(player.Vars.GetPlayerID(), true);
                RPC.AddStatusEffect(player, StatusEffects.Megingjord);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, "You were permanently granted increased carrying weight");
            }
        }
        if (Config.Players.CanSacrificeCryptKey.Value && zdo.Inventory.Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.CryptKey))
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
        if (Config.Players.CanSacrificeWishbone.Value && zdo.Inventory.Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.Wishbone))
        {
            player ??= GetPeerCharacter(data.m_senderPeerID);
            if (player is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedWishbone(player.Vars.GetPlayerID(), true);
                RPC.AddStatusEffect(player, StatusEffects.Wishbone);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, "You were permanently granted the ability to sense hidden objects");
            }
        }
        if (Config.Players.CanSacrificeTornSpirit.Value && zdo.Inventory.Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.TornSpirit))
        {
            player ??= GetPeerCharacter(data.m_senderPeerID);
            if (player is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedTornSpirit(player.Vars.GetPlayerID(), true);
                RPC.AddStatusEffect(player, StatusEffects.Demister);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, "You were permanently granted a wisp companion");
            }
        }
    }

    bool MoveItems(ExtendedZDO zdo, StackContainerState state, IEnumerable<Peer> peers)
    {
        var changed = false;
        HashSet<Vector2i>? usedSlots = null;
        for (int i = zdo.Inventory.Items.Count - 1; i >= 0; i--)
        {
            var item = zdo.Inventory.Items[i];
            if (!Instance<ContainerProcessor>().ContainersByItemName.TryGetValue(item.m_shared, out var containers))
                continue;

            foreach (var containerZdo in containers)
            {
                if (!containerZdo.IsValid() || containerZdo.PrefabInfo.Container is null)
                {
                    containers.Remove(containerZdo);
                    continue;
                }

                if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
                    continue; // in use or player to close

                var pickupRangeSqr = containerZdo.Inventory.PickupRange ?? Config.Containers.AutoPickupRange.Value;
                pickupRangeSqr *= pickupRangeSqr;

                if (pickupRangeSqr is 0f || Utils.DistanceSqr(state.PlayerZDO.GetPosition(), containerZdo.GetPosition()) > pickupRangeSqr)
                    continue;

                var stack = item.m_stack;
                usedSlots ??= [];
                usedSlots.Clear();

                var requestContainerOwn = false;

                ItemDrop.ItemData? containerItem = null;
                foreach (var slot in containerZdo.Inventory.Items)
                {
                    usedSlots.Add(slot.m_gridPos);
                    if (new ItemKey(item) != slot)
                        continue;

                    containerItem ??= slot;

                    var maxAmount = slot.m_shared.m_maxStackSize - slot.m_stack;
                    if (maxAmount <= 0)
                        continue;

                    if (!containerZdo.IsOwnerOrUnassigned())
                    {
                        requestContainerOwn = true;
                        break;
                    }

                    var amount = Math.Min(stack, maxAmount);
                    slot.m_stack += amount;
                    stack -= amount;
                    if (stack is 0)
                        break;
                }

                if (containerItem is null)
                {
                    containers.Remove(containerZdo);
                    if (containers is { Count: 0 })
                        Instance<ContainerProcessor>().ContainersByItemName.TryRemove(item.m_shared, out _);
                    continue;
                }

                for (var emptySlots = containerZdo.Inventory.Inventory.GetEmptySlots(); stack > 0 && emptySlots > 0; emptySlots--)
                {
                    if (!containerZdo.IsOwnerOrUnassigned())
                        requestContainerOwn = true;
                    if (requestContainerOwn)
                        break;

                    var amount = Math.Min(stack, item.m_shared.m_maxStackSize);

                    var slot = containerItem.Clone();
                    slot.m_stack = amount;
                    slot.m_gridPos.x = -1;
                    for (int x = 0; x < containerZdo.Inventory.Inventory.GetWidth() && slot.m_gridPos.x < 0; x++)
                    {
                        for (int y = 0; y < containerZdo.Inventory.Inventory.GetHeight(); y++)
                        {
                            if (usedSlots.Add(new(x, y)))
                            {
                                (slot.m_gridPos.x, slot.m_gridPos.y) = (x, y);
                                break;
                            }
                        }
                    }
                    containerZdo.Inventory.Items.Add(slot);
                    stack -= amount;
                }

                if (requestContainerOwn)
                {
                    Instance<ContainerProcessor>().RequestOwnership(containerZdo, state.PlayerZDO.Vars.GetPlayerID());
                    continue;
                }

                if (stack != item.m_stack)
                {
                    containerZdo.Inventory.Save();
                    (item.m_stack, stack) = (stack, item.m_stack);
                    changed = true;
                    ShowMessage(peers, containerZdo, $"{containerZdo.PrefabInfo.Container.Value.Piece.m_name}: $msg_added {item.m_shared.m_name} {stack}x", Config.Containers.PickedUpMessageType.Value);
                }

                if (item.m_stack is 0)
                {
                    zdo.Inventory.Items.RemoveAt(i);
                    break;
                }
            }
        }

        if (changed)
            zdo.Inventory.Save();
        return changed;
    }

    void OnStackContainerDestroyed(ExtendedZDO zdo) => _stackContainers.Remove(zdo);

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (_stackContainers.TryGetValue(zdo, out var stackContainerState))
        {
            if (zdo.Inventory.Items.Count is 0)
                DestroyPiece(zdo);
            else if (stackContainerState.Stacked)
            {
                if (stackContainerState.RemoveAfter < DateTimeOffset.UtcNow)
                    RPC.TakeAllResponse(zdo, true);
                else if(MoveItems(zdo, stackContainerState, peers))
                {
                    zdo.Destroyed -= OnStackContainerDestroyed;
                    _stackContainers.Remove(zdo);
                    if (zdo.Inventory.Items.Count is 0)
                        DestroyPiece(zdo);
                    else
                    {
                        _stackContainers.Add(zdo = RecreatePiece(zdo), stackContainerState);
                        zdo.Destroyed += OnStackContainerDestroyed;
                        // stackContainerState.RemoveAfter = DateTimeOffset.UtcNow;
                    }
                }
                return false;
            }
            else if (zdo.Inventory.Items.Any(static x => x is { m_gridPos.x: > 0 } or { m_stack: > 1 }))
            {
                for (int i = zdo.Inventory.Items.Count - 1; i >= 0; i--)
                {
                    var item = zdo.Inventory.Items[i];
                    if (item.m_gridPos.x is not 0)
                        continue;
                    if (--item.m_stack is 0)
                        zdo.Inventory.Items.RemoveAt(i);
                }
                zdo.Inventory.Save();
                stackContainerState.Stacked = true;
                stackContainerState.RemoveAfter = DateTimeOffset.UtcNow.AddSeconds(Config.Players.StackInventoryIntoContainersReturnDelay.Value);
                zdo.Destroyed -= OnStackContainerDestroyed;
                _stackContainers.Remove(zdo);
                _stackContainers.Add(zdo = RecreatePiece(zdo), stackContainerState);
                zdo.Destroyed += OnStackContainerDestroyed;
            }
            else if (stackContainerState.RemoveAfter < DateTimeOffset.UtcNow)
            {
                DestroyPiece(zdo);
            }
            else
            {
                RPC.StackResponse(zdo, true);
            }
            return true;
        }

        if (zdo.PrefabInfo.Player is null)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (!_playerStates.TryGetValue(zdo, out var state))
        {
            _playerStates.Add(zdo, state = new());
            zdo.Destroyed += x => _playerStates.Remove(x);

#if DEBUG
            RPC.AddStatusEffect(zdo, "Rested".GetStableHashCode());
#endif
        }

        if (_players.TryAdd(zdo.m_uid, zdo))
        {
            var playerID = zdo.Vars.GetPlayerID();
            _playersByID[playerID] = zdo;
            zdo.Destroyed += OnZdoDestroyed;
            if (Config.Players.CanSacrificeMegingjord.Value && DataZDO.Vars.GetSacrifiedMegingjord(playerID))
                RPC.AddStatusEffect(zdo, StatusEffects.Megingjord);
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
            /// <see cref="Emote.DoEmote(Emotes)"/> <see cref="Player.StartEmote(string, bool)"/>
            if (zdo.Vars.GetEmoteID() is var emoteId && emoteId != state.LastEmoteId)
            {
                state.LastEmoteId = emoteId;
                if (Config.Players.StackInventoryIntoContainersEmote.Value is ModConfig.PlayersConfig.AnyEmote || zdo.Vars.GetEmote() == Config.Players.StackInventoryIntoContainersEmote.Value)
                {
                    Dictionary<SharedItemDataKey, ItemDrop.ItemData>? items = null;
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

                        foreach (var item in containerZdo.Inventory.Items)
                            (items ??= []).TryAdd(item.m_shared, item);
                    }

                    if (items is not null)
                    {
                        var container = PlacePiece(zdo.GetPosition() with { y = -1000 }, Prefabs.WoodChest, 0);
                        var h = Math.Max(4, items.Count);
                        container.Fields<Container>().Set(static x => x.m_width, 8).Set(static x => x.m_height, h);
                        int y = 0;
                        foreach (var item in items.Values)
                        {
                            var clone = item.Clone();
                            clone.m_stack = 1;
                            clone.m_gridPos = new(0, y++);
                            container.Inventory.Items.Add(clone);
                        }
                        container.Inventory.Save();
                        container.SetOwner(zdo.GetOwner());
                        _stackContainers.Add(container, new(zdo));
                        container.Destroyed += OnStackContainerDestroyed;
                        RPC.StackResponse(container, true);
                    }
                }
            }
        }

        if (!Config.Tames.TeleportFollow.Value && !Config.Tames.TakeIntoDungeons.Value)
            return false;

        if (!Character.InInterior(zdo.GetPosition()))
            state.InitialInInteriorPosition = null;
        else if (state.InitialInInteriorPosition is null)
            state.InitialInInteriorPosition = zdo.GetPosition();

        var playerName = zdo.Vars.GetPlayerName();
        var playerZone = ZoneSystem.GetZone(zdo.GetPosition());

        foreach (var tameState in Instance<TameableProcessor>().Tames)
        {
            if (!tameState.IsTamed || tameState.ZDO.Vars.GetFollow() != playerName)
                continue;

            var tameZone = ZoneSystem.GetZone(tameState.ZDO.GetPosition());
            if (!ShouldTeleport(playerZone, tameZone, zdo, tameState.ZDO, state))
                continue;

            /// <see cref="TeleportWorld.Teleport"/>
            var targetPos = zdo.GetPosition();
            var direction = zdo.GetRotation() * Vector3.forward;
            var p = Config.Advanced.Tames.TeleportFollowPositioning;
            targetPos += Quaternion.Euler(0, UnityEngine.Random.Range(-p.HalfArcXZ, p.HalfArcXZ), 0) * direction * UnityEngine.Random.Range(p.MinDistXZ, p.MaxDistXZ);
            targetPos.y += UnityEngine.Random.Range(p.MinOffsetY, p.MaxOffsetY);
            tameState.ZDO.SetPosition(targetPos);
            tameState.ZDO.Recreate();
        }

        return false; 
    }

    bool ShouldTeleport(in Vector2i playerZone, in Vector2i tameZone, ExtendedZDO player, ExtendedZDO tame, PlayerState state)
    {
        if (Config.Tames.TakeIntoDungeons.Value && Character.InInterior(player.GetPosition()) != Character.InInterior(tame.GetPosition()))
        {
            if (Config.Advanced.Tames.TakeIntoDungeonExcluded.Contains(tame.GetPrefab()))
                return false;

            if (state.InitialInInteriorPosition is null)
                return true;
            // Workaround because the player position/rotation is not correctly updated until the player moves a bit after entering a dungeon
            if (Utils.DistanceXZ(state.InitialInInteriorPosition.Value, player.GetPosition()) > 0.5f)
                return true;
            return false;
        }

        if (Config.Tames.TeleportFollow.Value && !Character.InInterior(player.GetPosition()) && !ZNetScene.InActiveArea(tameZone, playerZone))
        {
            if (Config.Advanced.Tames.TeleportFollowExcluded.Contains(tame.GetPrefab()))
                return false;
            return true;
        }

        return false;
    }

    static bool EverybodyIsTryingToSleepPrefix(ref bool __result)
    {
        var instance = Instance<PlayerProcessor>();
        __result = instance.EverybodyIsTryingToSleep();
        instance.Logger.DevLog($"{nameof(EverybodyIsTryingToSleep)}: {__result}");
        return false;
    }

    bool EverybodyIsTryingToSleep()
    {
        if (_playerStates.Count is 0)
            return false;

        var inBed = 0;
        var sitting = 0;
        foreach (var player in _playerStates.Keys)
        {
            if (player.Vars.GetInBed())
                inBed++;
            else if (player.Vars.GetEmote() is Emotes.Sit)
                sitting++;
        }

        if (inBed == _playerStates.Count)
            return true;
        if (inBed < Config.Sleeping.MinPlayersInBed.Value)
            return false;

        var total = inBed + sitting;
        if (total * 100 / _playerStates.Count >= Config.Sleeping.RequiredPlayerPercentage.Value)
            return true;

        RPC.ShowMessage(ZRoutedRpc.Everybody, Config.Sleeping.SleepPromptMessageType.Value,
            $"{total} of {_playerStates.Count} players want to sleep.<br>Sit down if you want to sleep as well");

        return false;
    }
}
