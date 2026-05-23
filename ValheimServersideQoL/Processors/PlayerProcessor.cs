using BepInEx.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using Valheim.ServersideQoL.HarmonyPatches;
using static Skills;

namespace Valheim.ServersideQoL.Processors;

sealed partial class PlayerProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("159d939c-cb85-4314-ac30-f473d043fdc2");

    public BuildModifiers PossibleBuildModifiers { get; private set; }

    [Flags]
    public enum BuildModifiers : uint
    {
        None = 0,
        DisableRainDamage = 1 << 0,
        DisableSupportRequirements = 1 << 1,
        MakeIndestructible = 1 << 2,
        NoWorkbench = 1 << 3,
        DungeonBuild = 1 << 4,
        NoBuildCost = 1 << 5,
        AllPiecesUnlocked = 1 << 6
    }

    public enum LevelGroundModes
    {
        Default,
        FlattenMedium,
        FlattenLarge,
        Reset
    }

    readonly Dictionary<long, PlayerState> _playerStates = [];
    readonly Dictionary<ZRpc, PlayerState> _statesByRpc = [];

    readonly Dictionary<ZDOID, ExtendedZDO> _players = [];
    public IReadOnlyDictionary<ZDOID, ExtendedZDO> Players => _players;
    readonly Dictionary<long, ExtendedZDO> _playersByID = [];
    public IReadOnlyDictionary<long, ExtendedZDO> PlayersByID => _playersByID;
    public event Action<ExtendedZDO>? PlayerDestroyed;

    readonly Dictionary<Vector2s, ExtendedZDO> _zoneControls = [];
    readonly Dictionary<ExtendedZDO, PlayerState> _backpacks = [];
    int _backpackSlots;
    static TimeSpan OpenBackpackDelay => TimeSpan.FromMilliseconds(200);
    bool _estimateSkillLevels;
    double _emaTau;

    readonly int _numberOfLevelGroundModes = Enum.GetValues(typeof(LevelGroundModes)).Length;
    readonly int _mudRoadPrefab = "vfx_Place_mud_road".GetStableHashCode();
    //readonly int _terrainCompPrefab = GetHeightmap(default).m_terrainCompilerPrefab.name.GetStableHashCode();

    ZoneSystem.ZoneLocation DevGround1 => field ??= GetZoneLocation();
    ZoneSystem.ZoneLocation DevGround2 => field ??= GetZoneLocation();

    static ZoneSystem.ZoneLocation GetZoneLocation([CallerMemberName] string name = default!)
        => ZoneSystem.instance.GetLocationsByHash()[name.GetStableHashCode()];

    sealed record StackContainerState(ExtendedZDO PlayerZDO)
    {
        public DateTimeOffset RemoveAfter { get; set; } = DateTimeOffset.UtcNow.AddSeconds(20);
        public bool Stacked { get; set; }
    }

    readonly Dictionary<ExtendedZDO, StackContainerState> _stackContainers = [];

    public ExtendedZDO? GetPeerCharacter(long peerID) => _playerStates.TryGetValue(peerID, out var state) ? state.PlayerZDO : null;
    public IPeerInfo? GetPeerInfo(long peerID) => _playerStates.TryGetValue(peerID, out var state) ? state : null;
    public IPeerInfo? GetPeerInfoFromPlayerID(long playerID) => _playersByID.TryGetValue(playerID, out var zdo) && _playerStates.TryGetValue(zdo.GetOwner(), out var state) ? state : null;
    public IReadOnlyCollection<IPeerInfo> PeerInfos => _playerStates.Values;

    readonly MethodInfo _everybodyIsTryingToSleepMethod = typeof(Game).GetMethod("EverybodyIsTryingToSleep", BindingFlags.NonPublic | BindingFlags.Instance);
    readonly MethodInfo _everybodyIsTryingToSleepPrefix = ((Delegate)EverybodyIsTryingToSleepPrefix).Method;
    readonly MethodInfo _receivePingMethod = typeof(ZRpc).GetMethod("ReceivePing", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
    readonly MethodInfo _receivePingPefix = ((Delegate)PlayerState.ReceivePingPrefix).Method;
    readonly MethodInfo _sendPackageMethod = typeof(ZRpc).GetMethod("SendPackage", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
    readonly MethodInfo _sendPackagePrefix = ((Delegate)PlayerState.SendPackagePrefix).Method;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        _estimateSkillLevels = Config.Skills.AnyEnbaled;
        _emaTau = Config.Networking.PingEMAHalfLife.Value / Math.Log(2);

        var subscribeSetTrigger = _estimateSkillLevels;
        if (!subscribeSetTrigger && Game.m_staminaRate > 0)
            subscribeSetTrigger = Config.Players.InfiniteBuildingStamina.Value || Config.Players.InfiniteFarmingStamina.Value || Config.Players.InfiniteMiningStamina.Value || Config.Players.InfiniteWoodCuttingStamina.Value;
        UpdateRpcSubscription("SetTrigger", OnZSyncAnimationSetTrigger, subscribeSetTrigger);

        UpdateRpcSubscription("OnDeath", RPC_OnDeath, Config.Players.BackpackOnDeath.Value is not ModConfigBase.PlayersConfig.BackPackOnDeathOptions.Keep);

        //UpdateRpcSubscription("Say", OnTalkerSay, true);
        UpdateRpcSubscription("RPC_AnimateLever", RPC_AnimateLever,
            Config.Players.CanSacrificeMegingjord.Value ||
            Config.Players.CanSacrificeCryptKey.Value ||
            Config.Players.CanSacrificeWishbone.Value ||
            Config.Players.CanSacrificeTornSpirit.Value);

        Main.HarmonyInstance.Unpatch(_everybodyIsTryingToSleepMethod, _everybodyIsTryingToSleepPrefix);
        if (Config.Sleeping.MinPlayersInBed.Value > 0)
            Main.HarmonyInstance.Patch(_everybodyIsTryingToSleepMethod, prefix: new(_everybodyIsTryingToSleepPrefix));

        Main.HarmonyInstance.Unpatch(_receivePingMethod, _receivePingPefix);
        Main.HarmonyInstance.Unpatch(_sendPackageMethod, _sendPackagePrefix);
        if (Config.Networking.MeasurePing.Value)
        {
            Main.HarmonyInstance.Patch(_receivePingMethod, prefix: new(_receivePingPefix));
            Main.HarmonyInstance.Patch(_sendPackageMethod, prefix: new(_sendPackagePrefix));
        }

        void UpdateBackpackSlots()
        {
            _backpackSlots = Config.Players.InitialBackpackSlots.Value;
            if (Config.Players.AdditionalBackpackSlotsPerDefeatedBoss.Value is 0)
                return;
            _backpackSlots += Config.Players.AdditionalBackpackSlotsPerDefeatedBoss.Value * SharedProcessorState.BossesByBiome.Values
                .Count(static x => ZoneSystem.instance.GetGlobalKey(x.m_defeatSetGlobalKey));
            Logger.DevLog($"Backpack slots: {_backpackSlots}");
        }

        ZoneSystemSendGlobalKeys.GlobalKeysChanged -= UpdateBackpackSlots;
        if (Config.Players.OpenBackpackEmote.Value is ModConfigBase.DisabledEmote)
            _backpackSlots = 0;
        else
        {
            UpdateBackpackSlots();
            if (Config.Players.AdditionalBackpackSlotsPerDefeatedBoss.Value is not 0)
                ZoneSystemSendGlobalKeys.GlobalKeysChanged += UpdateBackpackSlots;
        }

        PossibleBuildModifiers = BuildModifiers.None;
        if (Config.Admins.ToggleDisableRainDamageEmote.Value is not ModConfigBase.DisabledEmote)
            PossibleBuildModifiers |= BuildModifiers.DisableRainDamage;
        if (Config.Admins.ToggleDisableSupportRequirements.Value is not ModConfigBase.DisabledEmote)
            PossibleBuildModifiers |= BuildModifiers.DisableSupportRequirements;
        if (Config.Admins.ToggleMakeIndestructible.Value is not ModConfigBase.DisabledEmote)
            PossibleBuildModifiers |= BuildModifiers.MakeIndestructible;
        if (Config.Admins.ToggleNoWorkbench.Value is not ModConfigBase.DisabledEmote)
            PossibleBuildModifiers |= BuildModifiers.NoWorkbench;
        if (Config.Admins.ToggleDungeonBuild.Value is not ModConfigBase.DisabledEmote)
            PossibleBuildModifiers |= BuildModifiers.DungeonBuild;
        if (Config.Admins.ToggleNoBuildCost.Value is not ModConfigBase.DisabledEmote)
            PossibleBuildModifiers |= BuildModifiers.NoBuildCost;
        if (Config.Admins.ToggleAllPiecesUnlocked.Value is not ModConfigBase.DisabledEmote)
            PossibleBuildModifiers |= BuildModifiers.AllPiecesUnlocked;

        Logger.DevLog($"Possible build modifiers: {PossibleBuildModifiers}");

        if (!firstTime)
            return;

        _players.Clear();
        _playersByID.Clear();
        _playerStates.Clear();
        _statesByRpc.Clear();
        _zoneControls.Clear();
        _backpacks.Clear();
    }

    void OnZdoDestroyed(ExtendedZDO zdo)
    {
        // zdo.GetOwner() is no longer valid here, so use zdo.m_uid.UserID instead
        if (!_playerStates.Remove(zdo.m_uid.UserID, out var state))
            return;

        if (state.Rpc is not null)
            _statesByRpc.Remove(state.Rpc);
        if (state.BackpackContainer is not null)
            _backpacks.Remove(state.BackpackContainer);
        _players.Remove(zdo.m_uid);
        if (_playersByID.Remove(state.PlayerID, out var zdo2) && zdo2 != zdo)
            _playersByID.Add(state.PlayerID, zdo2);
        PlayerDestroyed?.Invoke(zdo);
    }

    /// <see cref="ZSyncAnimation.SetTrigger(string)"/>
    void OnZSyncAnimationSetTrigger(ZRoutedRpc.RoutedRPCData data, string name)
    {
        if (!_players.TryGetValue(data.m_targetZDO, out var zdo) || !_playerStates.TryGetValue(zdo.GetOwner(), out var state))
            return;

        ItemDrop? rightItem = null;
        var prefab = zdo.Vars.GetRightItem();
        if (prefab is not 0)
        {
            rightItem = ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>();
            if (rightItem is null)
                Logger.LogWarning($"Player {state.PlayerName}: SetTrigger({name}): Right item prefab '{prefab}' not found");
        }

        ItemDrop? leftItem = null;
        if (rightItem is null && (prefab = zdo.Vars.GetLeftItem()) is not 0)
        {
            leftItem = ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>();
            if (leftItem is null)
                Logger.LogWarning($"Player {state.PlayerName}: SetTrigger({name}): Left item prefab '{prefab}' not found");
        }

        var item = rightItem ?? leftItem;

        //Logger.DevLog($"Trigger: {name}, Item: {item?.name}");

        if (item?.m_itemData.m_shared.m_attack is not { } attack)
            return;

        /// <see cref="Attack.Start"/>
        if (attack.m_attackChainLevels > 1 || attack.m_attackRandomAnimations >= 2)
        {
            if (Regex.IsMatch(name, $@"^{Regex.Escape(attack.m_attackAnimation)}\d+$"))
                state.LastUsedItem = item;
        }
        else if (name == attack.m_attackAnimation)
            state.LastUsedItem = item;

        //Logger.DevLog($"Trigger: {name}, Item: {item.name}, Last used: {state.LastUsedItem?.name}");

        static bool CheckStamina(string triggerName, ModConfigBase.PlayersConfig cfg)
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

        if (rightItem is not null && CheckStamina(name, Config.Players))
        {
            var requiredStamina = rightItem.m_itemData.m_shared.m_attack.m_attackStamina;
            if (zdo.Vars.GetStamina() < 2 * requiredStamina)
                RPC.UseStamina(zdo, -requiredStamina);
        }

        if (_estimateSkillLevels)
        {
            state.CheckSkillItem = null;
            if (item.m_itemData.m_shared is { m_attack.m_attackStamina: > 0 } and ({ m_skillType: not SkillType.Swords } or { m_damages.m_slash: > 0 }))
            {
                if (ReferenceEquals(item, state.LastUsedItem) &&
                    state.StaminaTimestamp < DateTimeOffset.UtcNow.AddSeconds(-1.5f * zdo.PrefabInfo.Player!.m_staminaRegenDelay))
                {
                    var stamina = zdo.Vars.GetStamina();
                    var floored = Mathf.FloorToInt(stamina);
                    if (floored != state.Stamina)
                    {
                        state.Stamina = floored;
                        state.StaminaTimestamp = DateTimeOffset.UtcNow;
                    }
                    else if (stamina >= 2 * item.m_itemData.m_shared.m_attack.m_attackStamina) // infinite stamina feature might interfere
                    {
                        state.CheckSkillStaminaEitr = stamina;
                        state.CheckSkillItem = item;
                    }
                }
            }
            else if (item.m_itemData.m_shared.m_attack.m_attackEitr > 0)
            {
                if (ReferenceEquals(item, state.LastUsedItem) &&
                    state.EitrTimestamp < DateTimeOffset.UtcNow.AddSeconds(-1.5f * zdo.PrefabInfo.Player!.m_eitrRegenDelay))
                {
                    var eitr = zdo.Vars.GetEitr();
                    var floored = Mathf.FloorToInt(eitr);
                    if (floored != state.Eitr)
                    {
                        state.Eitr = floored;
                        state.EitrTimestamp = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        state.CheckSkillStaminaEitr = eitr;
                        state.CheckSkillItem = item;
                    }
                }
            }
        }
    }

    /// <see cref="Talker.Say(Talker.Type, string)"/>
    //void OnTalkerSay(ZRoutedRpc.RoutedRPCData data, int ctype, UserInfo user, string text)
    //{
    //    var type = (Talker.Type)ctype;
    //}

    void DestroyBackpack(long peerID)
    {
        if (!_playerStates.TryGetValue(peerID, out var state) || state.BackpackContainer is not { } backpack)
            return;

        DestroyObject(backpack);
        Logger.LogInfo($"Backpack of player '{state.PlayerName}' destroyed on death");
    }

    void DropBackpackItem(ItemDrop.ItemData item, ExtendedZDO refPosZdo, long peerID)
    {
        var cfg = Config.Advanced.Players.BackpackOnDeathDropItems;
        var pos = refPosZdo.GetPosition();
        var scatter = UnityEngine.Random.insideUnitCircle * cfg.ScatterRadius;
        pos.x += scatter.x;
        pos.y += cfg.VerticalOffset;
        pos.z += scatter.y;
        var zdo = (ExtendedZDO)ItemDrop.DropItem(item, 0, pos, refPosZdo.GetRotation()).GetComponent<ZNetView>().GetZDO();
        zdo.Fields<ItemDrop>()
            .Set(static () => x => x.m_autoDestroy, !cfg.PreventAutoDestroy)
            .Set(static () => x => x.m_autoPickup, !cfg.PreventAutoPickup);
        zdo.SetOwnerInternal(peerID);
    }

    void DropBackpackItems(long peerID)
    {
        if (!_playerStates.TryGetValue(peerID, out var state) || state.BackpackContainer is not { } backpack)
            return;

        foreach (var item in backpack.GetInventory().Items)
            DropBackpackItem(item, state.PlayerZDO, peerID);
        DestroyObject(backpack);
        Logger.LogInfo($"Backpack items of player '{state.PlayerName}' dropped at death location.");
    }

    static int BackpackTombstonePrefab => Prefabs.TombStone;

    void DropBackback(long peerID)
    {
        if (!_playerStates.TryGetValue(peerID, out var state) || state.BackpackContainer is not { } backpack || backpack.GetInventory() is not { Items.Count: > 0 } backpackInventory)
            return;

        var pos = state.PlayerZDO.GetPosition();
        pos.y += Config.Advanced.Players.BackpackOnDeathDropTombStone.VerticalOffset;
        var zdo = Spawn(BackpackTombstonePrefab, pos, state.PlayerZDO.GetRotation(), owner: peerID);
        zdo.Vars.SetIsBackpack(true);
        /// <see cref="TombStone.Setup"/>
        zdo.Vars.SetOwner(state.PlayerID);
        zdo.Vars.SetOwnerName($"{state.PlayerName} - {Config.Localization.Players.Backpack.Name}");

        zdo.Fields<Container>()
            .Set(static () => x => x.m_width, backpackInventory.Inventory.GetWidth())
            .Set(static () => x => x.m_height, backpackInventory.Inventory.GetHeight());

        var inventory = zdo.GetInventory();
        foreach (var item in backpackInventory.Items)
            inventory.Items.Add(item);
        inventory.Save();

        DestroyObject(backpack);
        Logger.LogInfo($"Backpack of player '{state.PlayerName}' dropped at death location.");
    }

    void RPC_OnDeath(ZRoutedRpc.RoutedRPCData data)
    {
        switch (Config.Players.BackpackOnDeath.Value)
        {
            case ModConfigBase.PlayersConfig.BackPackOnDeathOptions.SameAsInventory:
                if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteItems) || ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteUnequipped))
                    DestroyBackpack(data.m_senderPeerID);
                else
                    DropBackback(data.m_senderPeerID);
                break;

            case ModConfigBase.PlayersConfig.BackPackOnDeathOptions.Destroy:
                DestroyBackpack(data.m_senderPeerID);
                break;

            case ModConfigBase.PlayersConfig.BackPackOnDeathOptions.DropTombStone:
                DropBackback(data.m_senderPeerID);
                break;

            case ModConfigBase.PlayersConfig.BackPackOnDeathOptions.DropItems:
                DropBackpackItems(data.m_senderPeerID);
                break;
        }
    }

    void RPC_AnimateLever(ExtendedZDO zdo, ZRoutedRpc.RoutedRPCData data)
    {
        if (zdo.PrefabInfo.Container is not { Incinerator.Value: not null } || zdo.Vars.GetIntTag() is not 0)
            return;

        IPeerInfo? peerInfo = null;
        IZDOInventory? inventory = null;
        if (Config.Players.CanSacrificeMegingjord.Value && (inventory ??= zdo.GetInventory()).Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.Megingjord))
        {
            peerInfo ??= GetPeerInfo(data.m_senderPeerID);
            if (peerInfo is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedMegingjord(peerInfo.PlayerID, true);
                RPC.AddStatusEffect(peerInfo.PlayerZDO, StatusEffects.Megingjord);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, Config.Localization.Players.SacrificedMegingjord);
            }
        }
        if (Config.Players.CanSacrificeCryptKey.Value && (inventory ??= zdo.GetInventory()).Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.CryptKey))
        {
            peerInfo ??= GetPeerInfo(data.m_senderPeerID);
            if (peerInfo is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedCryptKey(peerInfo.PlayerID, true);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, Config.Localization.Players.SacrificedCryptKey);
            }
        }
        if (Config.Players.CanSacrificeWishbone.Value && (inventory ??= zdo.GetInventory()).Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.Wishbone))
        {
            peerInfo ??= GetPeerInfo(data.m_senderPeerID);
            if (peerInfo is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedWishbone(peerInfo.PlayerID, true);
                RPC.AddStatusEffect(peerInfo.PlayerZDO, StatusEffects.Wishbone);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, Config.Localization.Players.SacrificedWishbone);
            }
        }
        if (Config.Players.CanSacrificeTornSpirit.Value && (inventory ??= zdo.GetInventory()).Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.TornSpirit))
        {
            peerInfo ??= GetPeerInfo(data.m_senderPeerID);
            if (peerInfo is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedTornSpirit(peerInfo.PlayerID, true);
                RPC.AddStatusEffect(peerInfo.PlayerZDO, StatusEffects.Demister);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, Config.Localization.Players.SacrificedTornSpirit);
            }
        }
    }

    bool MoveItems(ExtendedZDO zdo, StackContainerState state, IEnumerable<Peer> peers)
    {
        var changed = false;
        HashSet<Vector2i>? usedSlots = null;
        List<ExtendedZDO>? toRemove = null;
        var inventory = zdo.GetInventory();
        for (int i = inventory.Items.Count - 1; i >= 0; i--)
        {
            var item = inventory.Items[i];
            foreach (var containers in Instance<ContainerProcessor>().ContainersByItemName.EnumerateAdjacent((state.PlayerZDO.GetPosition(), item.m_shared)))
            {
                toRemove?.Clear();
                foreach (var containerZdo in containers)
                {
                    if (containerZdo.PrefabInfo.Container is null)
                    {
                        (toRemove ??= []).Add(containerZdo);
                        continue;
                    }

                    if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
                        continue; // in use or player to close

                    var containerInventory = containerZdo.GetInventory();
                    var pickupRangeSqr = containerInventory.PickupRange ?? Config.Containers.AutoPickupRange.Value;
                    pickupRangeSqr *= pickupRangeSqr;

                    if (pickupRangeSqr is 0f || Utils.DistanceSqr(state.PlayerZDO.GetPosition(), containerZdo.GetPosition()) > pickupRangeSqr)
                        continue;

                    var stack = item.m_stack;
                    usedSlots ??= [];
                    usedSlots.Clear();

                    var requestContainerOwn = false;

                    ItemDrop.ItemData? containerItem = null;
                    foreach (var slot in containerInventory.Items)
                    {
                        usedSlots.Add(slot.m_gridPos);
                        if (new ItemDataKey(item) != slot)
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
                        (toRemove ??= []).Add(containerZdo);
                        continue;
                    }

                    for (var emptySlots = containerInventory.Inventory.GetEmptySlots(); stack > 0 && emptySlots > 0; emptySlots--)
                    {
                        if (!containerZdo.IsOwnerOrUnassigned())
                            requestContainerOwn = true;
                        if (requestContainerOwn)
                            break;

                        var amount = Math.Min(stack, item.m_shared.m_maxStackSize);

                        var slot = containerItem.Clone();
                        slot.m_stack = amount;
                        slot.m_gridPos.x = -1;
                        for (int x = 0; x < containerInventory.Inventory.GetWidth() && slot.m_gridPos.x < 0; x++)
                        {
                            for (int y = 0; y < containerInventory.Inventory.GetHeight(); y++)
                            {
                                if (usedSlots.Add(new(x, y)))
                                {
                                    (slot.m_gridPos.x, slot.m_gridPos.y) = (x, y);
                                    break;
                                }
                            }
                        }
                        containerInventory.Items.Add(slot);
                        stack -= amount;
                    }

                    if (requestContainerOwn)
                    {
                        Instance<ContainerProcessor>().RequestOwnership(containerZdo, state.PlayerZDO.Vars.GetPlayerID());
                        continue;
                    }

                    if (stack != item.m_stack)
                    {
                        containerInventory.Save();
                        (item.m_stack, stack) = (stack, item.m_stack);
                        changed = true;
                        ShowMessage(peers, containerZdo,
                            Config.Localization.Containers.FormatAutoPickup(containerZdo.PrefabInfo.Container.Value.Container.m_name, item.m_shared.m_name, stack),
                            Config.Containers.PickedUpMessageType.Value);
                    }

                    if (item.m_stack is 0)
                    {
                        inventory.Items.RemoveAt(i);
                        break;
                    }
                }

                if (toRemove is not null)
                {
                    foreach (var containerZdo in toRemove)
                        containers.Remove(containerZdo);
                }

                if (item.m_stack is 0)
                    break;
            }
        }

        if (changed)
            inventory.Save();
        return changed;
    }

    void OnStackContainerDestroyed(ExtendedZDO zdo) => _stackContainers.Remove(zdo);

    internal static (int Width, int Height) GetBackpackSize(int slots)
    {
        var height = slots switch
        {
            < 4 => 1,
            < 9 => 2,
            < 16 => 3,
            <= 8 * 4 => 4,
            _ => 0
        };

        var width = 0;
        if (height > 0)
            width = (slots + height - 1) / height;
        else
        {
            width = 8;
            height = (slots + width - 1) / width;
        }

        return (width, height);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (_stackContainers.TryGetValue(zdo, out var stackContainerState))
        {
            var inventory = zdo.GetInventory();
            if (inventory.Items.Count is 0)
                DestroyObject(zdo);
            else if (stackContainerState.Stacked)
            {
                if (stackContainerState.RemoveAfter < DateTimeOffset.UtcNow)
                    RPC.TakeAllResponse(zdo, true);
                else if (MoveItems(zdo, stackContainerState, peers))
                {
                    zdo.Destroyed -= OnStackContainerDestroyed;
                    _stackContainers.Remove(zdo);
                    if (inventory.Items.Count is 0)
                        DestroyObject(zdo);
                    else
                    {
                        _stackContainers.Add(zdo = RecreatePiece(zdo), stackContainerState);
                        zdo.Destroyed += OnStackContainerDestroyed;
                        // stackContainerState.RemoveAfter = DateTimeOffset.UtcNow;
                    }
                }
                return false;
            }
            else if (inventory.Items.Any(static x => x is { m_gridPos.x: > 0 } or { m_stack: > 1 }))
            {
                for (int i = inventory.Items.Count - 1; i >= 0; i--)
                {
                    var item = inventory.Items[i];
                    if (item.m_gridPos.x is not 0)
                        continue;
                    if (--item.m_stack is 0)
                        inventory.Items.RemoveAt(i);
                }
                inventory.Save();
                stackContainerState.Stacked = true;
                stackContainerState.RemoveAfter = DateTimeOffset.UtcNow.AddSeconds(Config.Players.StackInventoryIntoContainersReturnDelay.Value);
                zdo.Destroyed -= OnStackContainerDestroyed;
                _stackContainers.Remove(zdo);
                _stackContainers.Add(zdo = RecreatePiece(zdo), stackContainerState);
                zdo.Destroyed += OnStackContainerDestroyed;
            }
            else if (stackContainerState.RemoveAfter < DateTimeOffset.UtcNow)
            {
                DestroyObject(zdo);
            }
            else
            {
                RPC.StackResponse(zdo, true);
            }
            return true;
        }

        if (_backpacks.TryGetValue(zdo, out var state))
        {
            var hasNonTeleportableItems = false;
            var weightLimitExceeded = false;
            var totalWeight = 0f;
            var inventory = zdo.GetInventory();
            var dropPos = state.PlayerZDO.GetPosition();
            dropPos.y += 2;
            for (int i = inventory.Items.Count - 1; i >= 0; i--)
            {
                var item = inventory.Items[i];
                var drop = false;
                if (!IsItemTeleportable(item))
                {
                    hasNonTeleportableItems = true;
                    drop = true;
                }
                else
                {
                    totalWeight += item.GetWeight();
                    if (Config.Players.MaxBackpackWeight.Value > 0 && totalWeight > Config.Players.MaxBackpackWeight.Value)
                    {
                        weightLimitExceeded = true;
                        drop = true;
                    }
                }

                if (drop)
                {
                    ItemDrop.DropItem(item, 0, dropPos, state.PlayerZDO.GetRotation());
                    inventory.Items.RemoveAt(i);
                }
            }

            if (hasNonTeleportableItems || weightLimitExceeded)
            {
                var owner = zdo.GetOwner();
                zdo.ClaimOwnershipInternal();
                inventory.Save();
                zdo.SetOwnerInternal(owner);
                state.BackpackContainer = RecreatePiece(zdo);
                RPC.ShowMessage(owner, MessageHud.MessageType.Center, hasNonTeleportableItems ?
                    Config.Localization.Players.Backpack.ForbiddenItems :
                    Config.Localization.Players.Backpack.FormatWeightLimitExceeded(Config.Players.MaxBackpackWeight.Value));
                state.OpenBackpackAfter = DateTimeOffset.UtcNow + OpenBackpackDelay;
            }
            return true;
        }

        if (zdo.PrefabInfo.Player is null)
        {
            UnregisterZdoProcessor = true;

            if (zdo.PrefabInfo.SpawnSystem is not null)
                _zoneControls[zdo.GetSector()] = zdo;
            else if (zdo.PrefabInfo is { Vagon: not null, Container: not null } && Config.Players.OpenCartEmote.Value is not ModConfigBase.DisabledEmote)
            {
                UnregisterZdoProcessor = false;
                if (_playerStates.TryGetValue(zdo.GetOwner(), out state))
                    state.AttachedCart = zdo.Vars.GetAttachJoint() ? zdo : null;
                return true;
            }
            else if (zdo.GetPrefab() == BackpackTombstonePrefab && zdo.Vars.GetIsBackpack())
            {
                UnregisterZdoProcessor = false;
                if (_playersByID.TryGetValue(zdo.Vars.GetOwner(), out var playerZdo) && !playerZdo.Vars.GetIsDead() &&
                    Vector3.Distance(playerZdo.GetPosition(), zdo.GetPosition()) < Config.Advanced.Players.BackpackOnDeathDropTombStone.AutoCollectDistance &&
                    _playerStates.TryGetValue(playerZdo.GetOwner(), out state))
                {
                    state.EnsureBackpackExists();
                    var inventory = zdo.GetInventory();
                    var backpackInventory = state.BackpackContainer.GetInventory();
                    foreach (var item in inventory.Items)
                    {
                        if (!backpackInventory.Inventory.AddItem(item))
                            DropBackpackItem(item, zdo, state.Owner);
                    }
                    state.BackpackContainer.ClaimOwnershipInternal();
                    backpackInventory.Save();
                    RPC.ShowMessage(state.Owner, MessageHud.MessageType.Center, $"$piece_tombstone_recovered ({Config.Localization.Players.Backpack.Name})");
                    zdo.Destroy();
                }
            }
            else if (zdo.GetPrefab() == _mudRoadPrefab && Config.Admins.CycleLevelGroundMode.Value is not ModConfigBase.DisabledEmote)
            {
                float minDistSqr = float.PositiveInfinity;
                Peer? peer = null;
                foreach (var p in peers)
                {
                    if (p.Info is not { LastUsedItem.name: PrefabNames.Hoe })
                        continue;
                    var distSqr = Utils.DistanceSqr(p.m_refPos, zdo.GetPosition());
                    if (distSqr < minDistSqr)
                    {
                        minDistSqr = distSqr;
                        peer = p;
                    }
                }

                if (peer is { Info.LevelGroundMode: LevelGroundModes.Reset })
                {
                    var zdos = new List<ZDO>();
                    ZDOMan.instance.FindSectorObjects(zdo.GetSector(), ZoneSystem.instance.GetActiveArea(), 0, zdos);
                    foreach (ExtendedZDO zdo2 in zdos)
                    {
                        if (zdo2.PrefabInfo.LocationProxy is not null)
                        {
                            var hash = zdo2.Vars.GetLocation();
                            _ = Remove(zdo2, hash, zdo.GetPosition(), DevGround1) || Remove(zdo2, hash, zdo.GetPosition(), DevGround2);

                            static bool Remove(ExtendedZDO zdo, int hash, Vector3 pos, ZoneSystem.ZoneLocation location)
                            {
                                if (hash != location.Hash || Utils.DistanceXZ(pos, zdo.GetPosition()) > location.m_exteriorRadius)
                                    return false;
                                zdo.Destroy();
                                return true;
                            }
                        }
                        else if (zdo2.PrefabInfo.TerrainComp is not null && TerrainCompData.Load(zdo2) is { } terrainComp)
                        {
                            terrainComp.ResetTerrain(zdo.GetPosition(), Config.Advanced.Admins.ResetTerrainRadius);
                            if (terrainComp.HasModifications is false)
                                zdo2.Destroy();
                        }
                    }
                }
                else if (peer?.Info switch
                {
                    { IsAdmin: true, LevelGroundMode: LevelGroundModes.FlattenMedium } => DevGround1,
                    { IsAdmin: true, LevelGroundMode: LevelGroundModes.FlattenLarge } => DevGround2,
                    _ => default
                } is { } location)
                {
                    /// <see cref="ZoneSystem.instance.TestSpawnLocation"/>
                    ZoneSystem.instance.SpawnLocation(location, 0, zdo.GetPosition(), zdo.GetRotation(), ZoneSystem.SpawnMode.Full);
                    var zdos = new List<ZDO>();
                    ZDOMan.instance.FindSectorObjects(zdo.GetSector(), ZoneSystem.instance.GetActiveArea(), 0, zdos);
                    foreach (ExtendedZDO zdo2 in zdos)
                    {
                        if (zdo2.PrefabInfo.TerrainComp is not null && TerrainCompData.Load(zdo2) is { } terrainComp)
                        {
                            terrainComp.ResetTerrain(zdo.GetPosition(), location.m_exteriorRadius);
                            if (terrainComp.HasModifications is false)
                                zdo2.Destroy();
                        }
                    }
                }
            }

            return false;
        }

        if (!_playerStates.TryGetValue(zdo.GetOwner(), out state))
        {
            _playerStates.Add(zdo.GetOwner(), state = new(zdo, this));
            if (state.Rpc is not null)
                _statesByRpc[state.Rpc] = state;
            _players[zdo.m_uid] = zdo;
            _playersByID[state.PlayerID] = zdo;
            zdo.Destroyed += OnZdoDestroyed;

            if (Config.Players.CanSacrificeMegingjord.Value && DataZDO.Vars.GetSacrifiedMegingjord(state.PlayerID))
                RPC.AddStatusEffect(zdo, StatusEffects.Megingjord);
            if (Config.Players.CanSacrificeWishbone.Value && DataZDO.Vars.GetSacrifiedWishbone(state.PlayerID))
                RPC.AddStatusEffect(zdo, StatusEffects.Wishbone);
            if (Config.Players.CanSacrificeTornSpirit.Value && DataZDO.Vars.GetSacrifiedTornSpirit(state.PlayerID))
                RPC.AddStatusEffect(zdo, StatusEffects.Demister);

#if DEBUG
            RPC.AddStatusEffect(zdo, "Rested".GetStableHashCode());
#endif
        }

        var now = DateTimeOffset.UtcNow;

        if (state.NextStaminaCheck < now)
        {
            state.NextStaminaCheck = now.AddSeconds(0.2);
            var stamina = Mathf.FloorToInt(zdo.Vars.GetStamina());
            if (state.Stamina != stamina)
            {
                state.StaminaTimestamp = now;
                state.Stamina = stamina;
            }
            if (stamina < zdo.PrefabInfo.Player.m_encumberedStaminaDrain && Config.Players.InfiniteEncumberedStamina.Value && zdo.Vars.GetAnimationIsEncumbered())
                RPC.UseStamina(zdo, -zdo.PrefabInfo.Player.m_encumberedStaminaDrain);
            else if (stamina < zdo.PrefabInfo.Player.m_sneakStaminaDrain && Config.Players.InfiniteSneakingStamina.Value && zdo.Vars.GetAnimationIsCrouching())
                RPC.UseStamina(zdo, -zdo.PrefabInfo.Player.m_sneakStaminaDrain);
            else if (stamina < zdo.PrefabInfo.Player.m_swimStaminaDrainMinSkill && Config.Players.InfiniteSwimmingStamina.Value && zdo.Vars.GetAnimationInWater())
                RPC.UseStamina(zdo, -zdo.PrefabInfo.Player.m_swimStaminaDrainMinSkill);

            var eitr = Mathf.FloorToInt(zdo.Vars.GetEitr());
            if (state.Eitr != eitr)
            {
                state.EitrTimestamp = now;
                state.Eitr = eitr;
            }
        }

        if (state.BackpackContainer is not null)
        {
            if (state.OpenBackpackAfter < now)
            {
                state.OpenBackpackAfter = null;
                RPC.OpenResponse(state.BackpackContainer, true);
            }
            else if (state.BackpackContainer.GetPosition() is { y: > -1000 } &&
                Vector3.Distance(zdo.GetPosition(), state.BackpackContainer.GetPosition()) > InventoryGui.instance.m_autoCloseDistance)
            {
                state.BackpackContainer.SetPosition(state.BackpackContainer.GetPosition() with { y = -1000 });
                state.BackpackContainer = RecreatePiece(state.BackpackContainer);
            }
        }

        if (_estimateSkillLevels && state.CheckSkillItem is not null)
        {
            var usesEitr = state.CheckSkillItem.m_itemData.m_shared.m_attack.m_attackEitr > 0;
            var staminaOrEitr = usesEitr ? zdo.Vars.GetEitr() : zdo.Vars.GetStamina();

            if (staminaOrEitr < state.CheckSkillStaminaEitr)
            {
                var shared = state.CheckSkillItem.m_itemData.m_shared;
                var max = usesEitr ? shared.m_attack.m_attackEitr : shared.m_attack.m_attackStamina;
                var eff = state.CheckSkillStaminaEitr - staminaOrEitr;
                var diff = max - eff;
                var estSkill = diff / (max * 0.33f);
                if (estSkill is >= 0f and <= 1f)
                {
                    const int HalfHistoryWindow = 3;
                    const int HistoryWindow = 2 * HalfHistoryWindow + 1;

                    var prevEstSkill = state.GetEstimatedSkillLevel(shared.m_skillType);
                    if (!state.EstimatedSkillLevelHistories.TryGetValue(shared.m_skillType, out var history))
                    {
                        state.EstimatedSkillLevelHistories.Add(shared.m_skillType, history = (new(HistoryWindow), new(HistoryWindow)));
                        if (!float.IsNaN(prevEstSkill))
                        {
                            for (int i = 0; i < HistoryWindow; i++)
                            {
                                history.Queue.Enqueue(prevEstSkill);
                                history.List.Add(prevEstSkill);
                            }
                        }
                    }

                    while (history.Queue.Count >= HistoryWindow)
                        history.List.Remove(history.Queue.Dequeue());

                    history.Queue.Enqueue(estSkill);
                    history.List.InsertSorted(estSkill);
                    // median
                    estSkill = history.List[history.List.Count / 2];

                    state.EstimatedSkillLevels[shared.m_skillType] = estSkill;
                    DataZDO.Vars.SetEstimatedSkillLevel(state.PlayerID, shared.m_skillType, estSkill);
                    var intSkill = Mathf.Floor(estSkill * 100);
                    var intPrevSkill = Mathf.Floor(prevEstSkill * 100);
                    if (intSkill != intPrevSkill)
                        Logger.Log(intSkill - intPrevSkill > 1f ? LogLevel.Warning : LogLevel.Info, $"Player {state.PlayerName}: Estimated {shared.m_skillType} skill level: {intSkill}, Previous estimate: {intPrevSkill} (Item: {state.CheckSkillItem.name}, max stamina: {max}, used stamina: {eff})");
                }
                state.CheckSkillItem = null;
            }
        }

        (var lastCraftingAnimation, state.LastCraftingAnimation) = (state.LastCraftingAnimation, zdo.Vars.GetAnimationCrafting());
        if (lastCraftingAnimation != state.LastCraftingAnimation && state.LastCraftingAnimation is not 0)
        {
            if (Instance<ContainerProcessor>().GetClosestContainer(zdo.GetPosition(), Config.CraftingStations.OpenClosestContainerRange.Value, state.PlayerID) is { } container)
                RPC.RequestOpenFor(zdo, container);
        }

        if (Config.Players.StackInventoryIntoContainersEmote.Value is not ModConfigBase.DisabledEmote ||
            Config.Players.OpenCartEmote.Value is not ModConfigBase.DisabledEmote ||
            _backpackSlots > 0 ||
            (PossibleBuildModifiers is not BuildModifiers.None && state.IsAdmin) ||
            Config.Admins.CycleLevelGroundMode.Value is not ModConfigBase.DisabledEmote)
        {
            /// <see cref="Emote.DoEmote(Emotes)"/> <see cref="Player.StartEmote(string, bool)"/>
            if (zdo.Vars.GetEmoteID() is var emoteId && emoteId != state.LastEmoteId)
            {
                state.LastEmoteId = emoteId;

                static bool CheckEmote(ExtendedZDO player, Emotes emote)
                    => emote is not ModConfigBase.DisabledEmote && (emote is ModConfigBase.AnyEmote || emote == player.Vars.GetEmote());

                if (CheckEmote(zdo, Config.Players.StackInventoryIntoContainersEmote.Value))
                {
                    Dictionary<SharedItemDataKey, ItemDrop.ItemData>? items = null;
                    foreach (var containerZdo in Instance<ContainerProcessor>().Containers)
                    {
                        //if (containerZdo.Vars.GetInUse() || !CheckMinDistance(peers, containerZdo))
                        //    continue; // in use or player to close

                        var containerInventory = containerZdo.GetInventory();
                        var pickupRangeSqr = containerInventory.PickupRange ?? Config.Containers.AutoPickupRange.Value;
                        pickupRangeSqr *= pickupRangeSqr;

                        if (pickupRangeSqr is 0f || Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) > pickupRangeSqr)
                            continue;

                        if (containerZdo.PrefabInfo.Container!.Value.Container.m_privacy is Container.PrivacySetting.Private && containerZdo.Vars.GetCreator() != zdo.Vars.GetPlayerID())
                            continue; // private container

                        foreach (var item in containerInventory.Items)
                            (items ??= []).TryAdd(item.m_shared, item);
                    }

                    if (items is not null)
                    {
                        var container = PlacePiece(zdo.GetPosition() with { y = -1000 }, Prefabs.WoodChest, 0);
                        var h = Math.Max(4, items.Count);
                        container.Fields<Container>()
                            .Set(static () => x => x.m_width, 8)
                            .Set(static () => x => x.m_height, h);
                        int y = 0;
                        var inventory = container.GetInventory();
                        foreach (var item in items.Values)
                        {
                            var clone = item.Clone();
                            clone.m_stack = 1;
                            clone.m_gridPos = new(0, y++);
                            inventory.Items.Add(clone);
                        }
                        inventory.Save();
                        container.SetOwnerInternal(zdo.GetOwner());
                        _stackContainers.Add(container, new(zdo));
                        container.Destroyed += OnStackContainerDestroyed;
                        RPC.StackResponse(container, true);
                    }
                }
                else if (CheckEmote(zdo, Config.Players.OpenCartEmote.Value) &&
                    state.AttachedCart is not null && state.AttachedCart.GetOwner() == state.Owner && state.AttachedCart.Vars.GetAttachJoint())
                {
                    RPC.OpenResponse(state.AttachedCart, true);
                }
                else if (_backpackSlots > 0 && CheckEmote(zdo, Config.Players.OpenBackpackEmote.Value))
                {
                    state.OpenBackpackAfter = now + OpenBackpackDelay;
                    if (state.EnsureBackpackExists())
                    {
                        state.OpenBackpackAfter = null;
                        RPC.OpenResponse(state.BackpackContainer, true);
                    }
                }
                else if (state.IsAdmin)
                {
                    static void UpdateGlobalKeyModification(PlayerState state, BuildModifiers modifier, GlobalKeys key)
                    {
                        if ((state.BuildModifiers & modifier) is 0)
                            state.RemoveGlobalKeyModification(new(key));
                        else
                            state.AddGlobalKeyModification(new(key), true);
                    }

                    if (CheckEmote(zdo, Config.Admins.ToggleDisableRainDamageEmote.Value))
                    {
                        state.BuildModifiers ^= BuildModifiers.DisableRainDamage;
                        state.NextBuildModifierMessage = default;
                    }
                    if (CheckEmote(zdo, Config.Admins.ToggleDisableSupportRequirements.Value))
                    {
                        state.BuildModifiers ^= BuildModifiers.DisableSupportRequirements;
                        state.NextBuildModifierMessage = default;
                    }
                    if (CheckEmote(zdo, Config.Admins.ToggleMakeIndestructible.Value))
                    {
                        state.BuildModifiers ^= BuildModifiers.MakeIndestructible;
                        state.NextBuildModifierMessage = default;
                    }
                    if (CheckEmote(zdo, Config.Admins.ToggleNoWorkbench.Value))
                    {
                        state.BuildModifiers ^= BuildModifiers.NoWorkbench;
                        state.NextBuildModifierMessage = default;
                        UpdateGlobalKeyModification(state, BuildModifiers.NoWorkbench, GlobalKeys.NoWorkbench);
                    }
                    if (CheckEmote(zdo, Config.Admins.ToggleDungeonBuild.Value))
                    {
                        state.BuildModifiers ^= BuildModifiers.DungeonBuild;
                        state.NextBuildModifierMessage = default;
                        UpdateGlobalKeyModification(state, BuildModifiers.DungeonBuild, GlobalKeys.DungeonBuild);
                    }
                    if (CheckEmote(zdo, Config.Admins.ToggleNoBuildCost.Value))
                    {
                        state.BuildModifiers ^= BuildModifiers.NoBuildCost;
                        state.NextBuildModifierMessage = default;
                        UpdateGlobalKeyModification(state, BuildModifiers.NoBuildCost, GlobalKeys.NoBuildCost);
                    }
                    if (CheckEmote(zdo, Config.Admins.ToggleAllPiecesUnlocked.Value))
                    {
                        state.BuildModifiers ^= BuildModifiers.AllPiecesUnlocked;
                        state.NextBuildModifierMessage = default;
                        UpdateGlobalKeyModification(state, BuildModifiers.AllPiecesUnlocked, GlobalKeys.AllPiecesUnlocked);
                    }
                    if (CheckEmote(zdo, Config.Admins.CycleLevelGroundMode.Value))
                    {
                        state.LevelGroundMode = (LevelGroundModes)(((int)state.LevelGroundMode + 1) % _numberOfLevelGroundModes);
                        state.NextLevelGroundModeMessage = default;
                    }
                }
            }
        }

        if (state.NextBuildModifierMessage == default || (
            state.BuildModifiers != default && state.NextBuildModifierMessage < now && zdo.Vars.GetRightItem() == Prefabs.Hammer))
        {
            state.NextBuildModifierMessage = now.AddSeconds(4);
            RPC.ShowMessage(state.Owner, MessageHud.MessageType.TopLeft, $"Build modifiers: {state.BuildModifiers}");
        }

        if (state.NextLevelGroundModeMessage == default || (
            state.LevelGroundMode != default && state.NextLevelGroundModeMessage < now && zdo.Vars.GetRightItem() == Prefabs.Hoe))
        {
            state.NextLevelGroundModeMessage = now.AddSeconds(4);
            RPC.ShowMessage(state.Owner, MessageHud.MessageType.TopLeft, $"Level ground mode: {state.LevelGroundMode}");
        }

        state.SendGlobalKeyModifications();

        if (!Config.Tames.TeleportFollow.Value && !Config.Tames.TakeIntoDungeons.Value)
            return false;

        if (!Character.InInterior(zdo.GetPosition()))
            state.InitialInInteriorPosition = null;
        else if (state.InitialInInteriorPosition is null)
            state.InitialInInteriorPosition = zdo.GetPosition();

        var playerName = zdo.Vars.GetPlayerName();
        var playerZone = zdo.GetSector();

        foreach (var tameState in Instance<TameableProcessor>().Tames)
        {
            if (!tameState.IsTamed || tameState.ZDO.Vars.GetFollow() != playerName)
                continue;

            var tameZone = tameState.ZDO.GetSector();
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

    bool ShouldTeleport(in Vector2s playerZone, in Vector2s tameZone, ExtendedZDO player, ExtendedZDO tame, PlayerState state)
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

        if (Config.Tames.TeleportFollow.Value && !Character.InInterior(player.GetPosition()))
        {
            if (Config.Advanced.Tames.TeleportFollowExcluded.Contains(tame.GetPrefab()))
                return false;
            if (Utils.DistanceXZ(player.GetPosition(), tame.GetPosition()) >= Config.Tames.TeleportFollowMinDistance.Value)
                return true;
            return false;
        }

        return false;
    }

    static bool EverybodyIsTryingToSleepPrefix(ref bool __result)
    {
        var instance = Instance<PlayerProcessor>();
        __result = instance.EverybodyIsTryingToSleep();
        //instance.Logger.DevLog($"{nameof(EverybodyIsTryingToSleep)}: {__result}");
        return false;
    }

    bool EverybodyIsTryingToSleep()
    {
        if (_playerStates.Count is 0)
            return false;

        var inBed = 0;
        var sitting = 0;
        foreach (var player in _players.Values)
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
            Config.Localization.Sleeping.FormatPrompt(total, _playerStates.Count));

        return false;
    }

    sealed class TerrainCompData
    {
        const int TerrainCompVersion = 1;
        readonly ExtendedZDO _zdo;
        readonly Heightmap _hmap;
        bool[]? _modifiedHeight;
        float[] _levelDelta = default!;
        float[] _smoothDelta = default!;
        bool[] _modifiedPaint = default!;
        Color[] _paintMask = default!;
        int _operations;
        Vector3 _lastOpPoint;
        float _lastOpRadius;

        public bool? HasModifications { get; private set; }

        public static TerrainCompData? Load(ExtendedZDO zdo)
        {
            zdo.AssertIs<TerrainComp>();
            if (GetHeightmap(zdo.GetPosition()) is not { } hmap)
            {
                Main.Instance.Logger.LogWarning($"Heightmap not found at {zdo.GetPosition()}");
                return null;
            }
            return new(zdo, hmap);
        }

        TerrainCompData(ExtendedZDO zdo, Heightmap hmap)
        {
            _zdo = zdo;
            _hmap = hmap;
        }

        [MemberNotNullWhen(true, nameof(_modifiedHeight))]
        bool Load()
        {
            if (_modifiedHeight is not null)
                return true;

            /// <see cref="TerrainComp.Load"/>
            byte[] byteArray = _zdo.GetByteArray(ZDOVars.s_TCData);
            if (byteArray == null)
                return false;

            var expectedLength = _hmap.m_width + 1;
            expectedLength *= expectedLength;

            ZPackage zPackage = new ZPackage(Utils.Decompress(byteArray));
            if (zPackage.ReadInt() is not TerrainCompVersion)
            {
                Main.Instance.Logger.LogWarning("Terrain data load error, version missmatch");
                return false;
            }
            _operations = zPackage.ReadInt();
            _lastOpPoint = zPackage.ReadVector3();
            _lastOpRadius = zPackage.ReadSingle();
            int num = zPackage.ReadInt();
            if (num != expectedLength)
            {
                Main.Instance.Logger.LogWarning("Terrain data load error, height array missmatch");
                return false;
            }

            _modifiedHeight = new bool[expectedLength];
            _levelDelta = new float[expectedLength];
            _smoothDelta = new float[expectedLength];
            _modifiedPaint = new bool[expectedLength];
            _paintMask = new Color[expectedLength];
            HasModifications = false;

            for (int i = 0; i < num; i++)
            {
                _modifiedHeight[i] = zPackage.ReadBool();
                if (_modifiedHeight[i])
                {
                    _levelDelta[i] = zPackage.ReadSingle();
                    _smoothDelta[i] = zPackage.ReadSingle();
                    HasModifications = true;
                }
                else
                {
                    _levelDelta[i] = 0f;
                    _smoothDelta[i] = 0f;
                }
            }

            int num2 = zPackage.ReadInt();
            for (int j = 0; j < num2; j++)
            {
                _modifiedPaint[j] = zPackage.ReadBool();
                if (_modifiedPaint[j])
                {
                    var color = new Color
                    {
                        r = zPackage.ReadSingle(),
                        g = zPackage.ReadSingle(),
                        b = zPackage.ReadSingle(),
                        a = zPackage.ReadSingle()
                    };
                    _paintMask[j] = color;
                    HasModifications = true;
                }
                else
                {
                    _paintMask[j] = Color.black;
                }
            }

            if (num2 == _hmap.m_width * _hmap.m_width)
            {
                Color[] array = new Color[_paintMask.Length];
                _paintMask.CopyTo(array, 0);
                bool[] array2 = new bool[_modifiedPaint.Length];
                _modifiedPaint.CopyTo(array2, 0);
                int num3 = _hmap.m_width + 1;
                for (int k = 0; k < _paintMask.Length; k++)
                {
                    int num4 = k / num3;
                    int num5 = (k + 1) / num3;
                    int num6 = k - num4;
                    if (num4 == _hmap.m_width)
                    {
                        num6 -= _hmap.m_width;
                    }

                    if (k > 0 && (k - num4) % _hmap.m_width == 0 && (k + 1 - num5) % _hmap.m_width == 0)
                    {
                        num6--;
                    }

                    _paintMask[k] = array[num6];
                    _modifiedPaint[k] = array2[num6];
                }
            }

            return true;
        }

        void Save()
        {
            if (_modifiedHeight is null)
                return;

            HasModifications = false;

            ZPackage zPackage = new();
            zPackage.Write(TerrainCompVersion);
            zPackage.Write(_operations);
            zPackage.Write(_lastOpPoint);
            zPackage.Write(_lastOpRadius);
            zPackage.Write(_modifiedHeight.Length);
            for (int i = 0; i < _modifiedHeight.Length; i++)
            {
                zPackage.Write(_modifiedHeight[i]);
                if (_modifiedHeight[i])
                {
                    zPackage.Write(_levelDelta[i]);
                    zPackage.Write(_smoothDelta[i]);
                    HasModifications = true;
                }
            }

            zPackage.Write(_modifiedPaint.Length);
            for (int j = 0; j < _modifiedPaint.Length; j++)
            {
                zPackage.Write(_modifiedPaint[j]);
                if (_modifiedPaint[j])
                {
                    zPackage.Write(_paintMask[j].r);
                    zPackage.Write(_paintMask[j].g);
                    zPackage.Write(_paintMask[j].b);
                    zPackage.Write(_paintMask[j].a);
                    HasModifications = true;
                }
            }

            byte[] bytes = Utils.Compress(zPackage.GetArray());
            _zdo.Set(ZDOVars.s_TCData, bytes);
        }

        public void ResetTerrain(Vector3 pos, float radius)
        {
            _hmap.WorldToVertex(pos, out var x, out var y);
            float b = pos.y - _zdo.GetPosition().y;
            float num = radius / _hmap.m_scale;
            int num2 = Mathf.CeilToInt(num);
            Vector2 a = new Vector2(x, y);
            int num3 = _hmap.m_width + 1;

            var save = false;
            for (int i = y - num2; i <= y + num2; i++)
            {
                for (int j = x - num2; j <= x + num2; j++)
                {
                    float num4 = Vector2.Distance(a, new Vector2(j, i));
                    if (!(num4 > num) && j >= 0 && i >= 0 && j < num3 && i < num3)
                    {
                        if (!Load())
                            return;

                        int num7 = i * num3 + j;
                        _modifiedHeight[num7] = false;
                        _smoothDelta[num7] = 0;
                        _levelDelta[num7] = 0;
                        _modifiedPaint[num7] = false;
                        _paintMask[num7] = Color.black;
                        save = true;
                    }
                }
            }

            if (save)
                Save();
        }
    }
}
