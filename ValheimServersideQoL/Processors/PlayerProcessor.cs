using BepInEx.Logging;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Valheim.ServersideQoL.HarmonyPatches;

namespace Valheim.ServersideQoL.Processors;

sealed class PlayerProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("159d939c-cb85-4314-ac30-f473d043fdc2");
    public interface IPeerInfo
    {
        ExtendedZDO PlayerZDO { get; }
        long PlayerID { get; }
        string PlayerName { get; }
        bool IsAdmin { get; }
        float ConnectionQuality { get; }
        IReadOnlyDictionary<Skills.SkillType, float> EstimatedSkillLevels { get; }
        public ItemDrop? LastUsedItem { get; }
    }

    sealed class PlayerState(ExtendedZDO playerZDO, PlayerProcessor processor) : IPeerInfo
    {
        readonly PlayerProcessor _processor = processor;
        public ExtendedZDO PlayerZDO { get; } = playerZDO;
        readonly ZNetPeer? _peer = ZNet.instance.GetPeer(playerZDO.GetOwner());
        public ZRpc? Rpc => _peer?.m_rpc;
        //public bool IsServer => PlayerZDO.GetOwner() == ZDOMan.GetSessionID();
        long? _playerID;
        public long PlayerID => _playerID ??= PlayerZDO.Vars.GetPlayerID();
        string? _playerName;
        public string PlayerName => _playerName ??= PlayerZDO.Vars.GetPlayerName();
        bool? _isAdmin;
        public bool IsAdmin => _isAdmin ??= (Player.m_localPlayer?.GetZDOID() == PlayerZDO.m_uid || ZNet.instance.IsAdmin(_peer?.m_socket.GetHostName() ?? ""));
        public int LastEmoteId { get; set; } = 0; // Ignore first 'Sit' when logging in
        public Vector3? InitialInInteriorPosition { get; set; }
        public DateTimeOffset NextStaminaCheck { get; set; }
        public int Stamina { get; set; }
        public DateTimeOffset StaminaTimestamp { get; set; } = DateTimeOffset.UtcNow;

        ExtendedZDO? _backpackContainer;
        public ExtendedZDO? BackpackContainer
        {
            get => _backpackContainer;
            set
            {
                if (_backpackContainer is not null)
                    _processor._backpacks.Remove(_backpackContainer);
                _backpackContainer = value;
                if (_backpackContainer is not null)
                {
                    _processor._backpacks.Add(_backpackContainer, this);
                    _backpackContainer.Destroyed += OnBackpackDestroyed;
                }
            }
        }

        void OnBackpackDestroyed(ExtendedZDO backpack)
        {
            _processor._backpacks.Remove(backpack);
            if (ReferenceEquals(backpack, _backpackContainer))
                _backpackContainer = null;
        }

        public DateTimeOffset? OpenBackpackAfter { get; set; }

        public ItemDrop? LastUsedItem { get; set; }
        public ItemDrop? CheckSkillItem { get; set; }
        public float CheckSkillStamina { get; set; }
        public Dictionary<Skills.SkillType, float> EstimatedSkillLevels => field ??= [];
        IReadOnlyDictionary<Skills.SkillType, float> IPeerInfo.EstimatedSkillLevels => EstimatedSkillLevels;
        public Dictionary<Skills.SkillType, (Queue<float> Queue, List<float> List)> EstimatedSkillLevelHistories => field ??= [];

        public TimeSpan? LastPing { get; private set; }
        public TimeSpan? PingMean { get; private set; }
        public TimeSpan? PingStdDev { get; private set; }
        public TimeSpan? PingJitter { get; private set; }
        public float ConnectionQuality { get; private set; }

        readonly List<TimeSpan> _pingHistory = [];
        DateTimeOffset _pingStart;

        public static void ReceivePingPrefix(ZRpc __instance, ZPackage package)
        {
            var pos = package.GetPos();
            var isPing = !package.ReadBool();
            package.SetPos(pos);
            if (isPing && Instance<PlayerProcessor>()._statesByRpc.TryGetValue(__instance, out var state))
                state.ReceivePingPrefix();
        }

        public static bool SendPackagePrefix(ZRpc __instance, ZPackage pkg)
        {
            var pos = pkg.GetPos();
            pkg.SetPos(0);
            var isPing = pkg.ReadInt() is 0 && pkg.ReadBool();
            pkg.SetPos(pos);
            if (!isPing || !Instance<PlayerProcessor>()._statesByRpc.TryGetValue(__instance, out var state))
                return true;
            return state.SendPingPrefix();
        }

        void ReceivePingPrefix()
        {
            LastPing = DateTimeOffset.UtcNow - _pingStart;
            _pingStart = default;

            var cfg = _processor.Config.Networking;

            while (_pingHistory.Count >= cfg.PingStatisticsWindow.Value)
                _pingHistory.RemoveAt(0);
            _pingHistory.Add(LastPing ?? default);

            (PingMean, PingStdDev, PingJitter) = CalculateStats(_pingHistory);
            var connectionQuality =
                PingMean?.TotalMilliseconds * cfg.ConnectionQualityPingMeanWeight.Value +
                PingStdDev?.TotalMilliseconds * cfg.ConnectionQualityPingStdDevWeight.Value +
                PingJitter?.TotalMilliseconds * cfg.ConnectionQualityPingJitterWeight.Value;
            ConnectionQuality = connectionQuality is null ? float.NaN : (float)connectionQuality;

            PlayerState? ownerState = null;
            if (_processor._zoneControls.TryGetValue(PlayerZDO.GetSector(), out var zoneCtrl) &&
                zoneCtrl.GetOwner() != PlayerZDO.GetOwner())
            {
                _processor._playerStates.TryGetValue(zoneCtrl.GetOwner(), out ownerState);
            }

            if (LastPing > TimeSpan.FromMilliseconds(cfg.LogPingThreshold.Value) || ownerState?.LastPing > TimeSpan.FromMilliseconds(cfg.LogZoneOwnerPingThreshold.Value))
            {
                if (ownerState is null)
                    _processor.Logger.LogInfo(string.Format(cfg.LogPingFormat.Value, [PlayerName, LastPing?.TotalMilliseconds, PingMean?.TotalMilliseconds, PingStdDev?.TotalMilliseconds, PingJitter?.TotalMilliseconds, ConnectionQuality]));
                else
                    _processor.Logger.LogInfo(string.Format(cfg.LogZoneOwnerPingFormat.Value, [PlayerName, LastPing?.TotalMilliseconds, PingMean?.TotalMilliseconds, PingStdDev?.TotalMilliseconds, PingJitter?.TotalMilliseconds, ConnectionQuality, ownerState.PlayerName, ownerState.LastPing?.TotalMilliseconds, ownerState.PingMean?.TotalMilliseconds, ownerState.PingStdDev?.TotalMilliseconds, ownerState.PingJitter?.TotalMilliseconds, ownerState.ConnectionQuality]));
            }
            if (LastPing > TimeSpan.FromMilliseconds(cfg.ShowPingThreshold.Value) || ownerState?.LastPing > TimeSpan.FromMilliseconds(cfg.ShowZoneOwnerPingThreshold.Value))
            {
                if (ownerState is null)
                    RPC.ShowMessage(PlayerZDO.GetOwner(), MessageHud.MessageType.TopLeft, string.Format(cfg.ShowPingFormat.Value, [LastPing?.TotalMilliseconds, PingMean?.TotalMilliseconds, PingStdDev?.TotalMilliseconds, PingJitter?.TotalMilliseconds, ConnectionQuality]));
                else
                    RPC.ShowMessage(PlayerZDO.GetOwner(), MessageHud.MessageType.TopLeft, string.Format(cfg.ShowZoneOwnerPingFormat.Value, [LastPing?.TotalMilliseconds, PingMean?.TotalMilliseconds, PingStdDev?.TotalMilliseconds, PingJitter?.TotalMilliseconds, ConnectionQuality, ownerState.PlayerName, ownerState.LastPing?.TotalMilliseconds, ownerState.PingMean?.TotalMilliseconds, ownerState.PingStdDev?.TotalMilliseconds, ownerState.PingJitter?.TotalMilliseconds, ownerState.ConnectionQuality]));
            }

            static (TimeSpan? Mean, TimeSpan? StdDev, TimeSpan? Jitter) CalculateStats(IReadOnlyList<TimeSpan> pingHistory)
            {
                if (pingHistory.Count < 2)
                    return default;

                double mean = 0;
                double variance = 0;
                long lastTicks = pingHistory.FirstOrDefault().Ticks;
                long jitter = 0;
                int n = 0;
                foreach (var ping in pingHistory)
                {
                    if (ping == default)
                        return default;
                    var value = ping.TotalMilliseconds;
                    var delta = value - mean;
                    mean += delta / ++n;
                    variance += delta * (value - mean);
                    jitter += Math.Abs(ping.Ticks - lastTicks);
                    lastTicks = ping.Ticks;
                }

                n--;
                variance /= n;
                jitter /= n;

                return (
                    double.IsNaN(mean) ? null : TimeSpan.FromMilliseconds(mean),
                    double.IsNaN(variance) ? null : TimeSpan.FromMilliseconds(Math.Sqrt(variance)),
                    jitter is 0 ? null : new(jitter));
            }
        }

        bool SendPingPrefix()
        {
            if (_pingStart != default && Rpc is not null && Rpc.GetTimeSinceLastPing() < ZNet.instance.m_badConnectionPing)
                return false;
            if (_pingStart == default)
                _pingStart = DateTimeOffset.UtcNow;
            return true;
        }
    }

    readonly Dictionary<long, PlayerState> _playerStates = [];
    readonly Dictionary<ZRpc, PlayerState> _statesByRpc = [];

    readonly Dictionary<ZDOID, ExtendedZDO> _players = [];
    public IReadOnlyDictionary<ZDOID, ExtendedZDO> Players => _players;
    readonly Dictionary<long, ExtendedZDO> _playersByID = [];
    public IReadOnlyDictionary<long, ExtendedZDO> PlayersByID => _playersByID;
    public event Action<ExtendedZDO>? PlayerDestroyed;

    readonly Dictionary<Vector2i, ExtendedZDO> _zoneControls = [];
    readonly Dictionary<ExtendedZDO, PlayerState> _backpacks = [];
    int _backpackSlots;
    static TimeSpan OpenBackpackDelay => TimeSpan.FromMilliseconds(200);
    bool _estimateSkillLevels;

    sealed record StackContainerState(ExtendedZDO PlayerZDO)
    {
        public DateTimeOffset RemoveAfter { get; set; } = DateTimeOffset.UtcNow.AddSeconds(20);
        public bool Stacked { get; set; }
    }

    readonly Dictionary<ExtendedZDO, StackContainerState> _stackContainers = [];

    public ExtendedZDO? GetPeerCharacter(long peerID) => _playerStates.TryGetValue(peerID, out var state) ? state.PlayerZDO : null;
    public IPeerInfo? GetPeerInfo(long peerID) => _playerStates.TryGetValue(peerID, out var state) ? state : null;
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

        _estimateSkillLevels = Config.Skills.PickaxeAffectsRockDestruction.Value;

        var subscribeSetTrigger = _estimateSkillLevels;
        if (!subscribeSetTrigger && Game.m_staminaRate > 0)
            subscribeSetTrigger = Config.Players.InfiniteBuildingStamina.Value || Config.Players.InfiniteFarmingStamina.Value || Config.Players.InfiniteMiningStamina.Value || Config.Players.InfiniteWoodCuttingStamina.Value;
        UpdateRpcSubscription("SetTrigger", OnZSyncAnimationSetTrigger, subscribeSetTrigger);

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
        if (Config.Players.OpenBackpackEmote.Value is ModConfig.PlayersConfig.DisabledEmote)
            _backpackSlots = 0;
        else
        {
            UpdateBackpackSlots();
            if (Config.Players.AdditionalBackpackSlotsPerDefeatedBoss.Value is not 0)
                ZoneSystemSendGlobalKeys.GlobalKeysChanged += UpdateBackpackSlots;
        }

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

        if (rightItem is not null && CheckStamina(name, Config.Players))
        {
            var requiredStamina = rightItem.m_itemData.m_shared.m_attack.m_attackStamina;
            if (zdo.Vars.GetStamina() < 2 * requiredStamina)
                RPC.UseStamina(zdo, -requiredStamina);
        }

        if (_estimateSkillLevels)
        {
            state.CheckSkillItem = null;
            if (item.m_itemData.m_shared is { m_skillType: not Skills.SkillType.Swords } or {m_damages.m_slash: > 0 } &&
                ReferenceEquals(item, state.LastUsedItem) &&
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
                    state.CheckSkillStamina = stamina;
                    state.CheckSkillItem = item;
                }
            }
        }
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

        IPeerInfo? peerInfo = null;
        if (Config.Players.CanSacrificeMegingjord.Value && zdo.Inventory.Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.Megingjord))
        {
            peerInfo ??= GetPeerInfo(data.m_senderPeerID);
            if (peerInfo is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedMegingjord(peerInfo.PlayerID, true);
                RPC.AddStatusEffect(peerInfo.PlayerZDO, StatusEffects.Megingjord);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, "You were permanently granted increased carrying weight");
            }
        }
        if (Config.Players.CanSacrificeCryptKey.Value && zdo.Inventory.Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.CryptKey))
        {
            peerInfo ??= GetPeerInfo(data.m_senderPeerID);
            if (peerInfo is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedCryptKey(peerInfo.PlayerID, true);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, "You were permanently granted the ability to open sunken crypt doors");
            }
        }
        if (Config.Players.CanSacrificeWishbone.Value && zdo.Inventory.Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.Wishbone))
        {
            peerInfo ??= GetPeerInfo(data.m_senderPeerID);
            if (peerInfo is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedWishbone(peerInfo.PlayerID, true);
                RPC.AddStatusEffect(peerInfo.PlayerZDO, StatusEffects.Wishbone);
                RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, "You were permanently granted the ability to sense hidden objects");
            }
        }
        if (Config.Players.CanSacrificeTornSpirit.Value && zdo.Inventory.Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.TornSpirit))
        {
            peerInfo ??= GetPeerInfo(data.m_senderPeerID);
            if (peerInfo is null)
                Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
            else
            {
                DataZDO.Vars.SetSacrifiedTornSpirit(peerInfo.PlayerID, true);
                RPC.AddStatusEffect(peerInfo.PlayerZDO, StatusEffects.Demister);
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

    static (int Width, int Height) GetBackpackSize(int slots)
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
            if (zdo.Inventory.Items.Count is 0)
                DestroyObject(zdo);
            else if (stackContainerState.Stacked)
            {
                if (stackContainerState.RemoveAfter < DateTimeOffset.UtcNow)
                    RPC.TakeAllResponse(zdo, true);
                else if (MoveItems(zdo, stackContainerState, peers))
                {
                    zdo.Destroyed -= OnStackContainerDestroyed;
                    _stackContainers.Remove(zdo);
                    if (zdo.Inventory.Items.Count is 0)
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
            var inventory = zdo.Inventory;
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
                zdo.Inventory.Save();
                zdo.SetOwnerInternal(owner);
                state.BackpackContainer = RecreatePiece(zdo);
                RPC.ShowMessage(owner, MessageHud.MessageType.Center, hasNonTeleportableItems ?
                    "Backpack cannot contain non-teleportable items" :
                    $"Backpack weight limit ({Config.Players.MaxBackpackWeight.Value}) exceeded");
                state.OpenBackpackAfter = DateTimeOffset.UtcNow + OpenBackpackDelay;
            }
            return true;
        }

        if (zdo.PrefabInfo.Player is null)
        {
            UnregisterZdoProcessor = true;

            if (zdo.PrefabInfo.SpawnSystem is not null)
                _zoneControls[zdo.GetSector()] = zdo;

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

        if (state.NextStaminaCheck < DateTimeOffset.UtcNow)
        {
            state.NextStaminaCheck = DateTimeOffset.UtcNow.AddSeconds(0.5);
            var stamina = Mathf.FloorToInt(zdo.Vars.GetStamina());
            if (state.Stamina != stamina)
            {
                state.StaminaTimestamp = DateTimeOffset.UtcNow;
                state.Stamina = stamina;
            }
            if (stamina < zdo.PrefabInfo.Player.m_encumberedStaminaDrain && Config.Players.InfiniteEncumberedStamina.Value && zdo.Vars.GetAnimationIsEncumbered())
                RPC.UseStamina(zdo, -zdo.PrefabInfo.Player.m_encumberedStaminaDrain);
            else if (stamina < zdo.PrefabInfo.Player.m_sneakStaminaDrain && Config.Players.InfiniteSneakingStamina.Value && zdo.Vars.GetAnimationIsCrouching())
                RPC.UseStamina(zdo, -zdo.PrefabInfo.Player.m_sneakStaminaDrain);
            else if (stamina < zdo.PrefabInfo.Player.m_swimStaminaDrainMinSkill && Config.Players.InfiniteSwimmingStamina.Value && zdo.Vars.GetAnimationInWater())
                RPC.UseStamina(zdo, -zdo.PrefabInfo.Player.m_swimStaminaDrainMinSkill);
        }

        if (state.BackpackContainer is not null)
        {
            if (state.OpenBackpackAfter < DateTimeOffset.UtcNow)
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
            var stamina = zdo.Vars.GetStamina();
            if (stamina < state.CheckSkillStamina)
            {
                var shared = state.CheckSkillItem.m_itemData.m_shared;
                var max = shared.m_attack.m_attackStamina;
                var eff = state.CheckSkillStamina - stamina;
                var diff = max - eff;
                var estSkill = diff / (max * 0.33f);
                if (estSkill is >= 0f and <= 1f)
                {
                    const int HalfHistoryWindow = 2;
                    const int HistoryWindow = 2 * HalfHistoryWindow + 1;

                    if (!state.EstimatedSkillLevelHistories.TryGetValue(shared.m_skillType, out var history))
                        state.EstimatedSkillLevelHistories.Add(shared.m_skillType, history = (new(HistoryWindow), new(HistoryWindow)));

                    while (history.Queue.Count >= HistoryWindow)
                        history.List.Remove(history.Queue.Dequeue());

                    history.Queue.Enqueue(estSkill);
                    history.List.InsertSorted(estSkill);
                    // median
                    estSkill = history.List[history.List.Count / 2];

                    //var prevEstSkill = DataZDO.Vars.GetEstimatedSkillLevel(state.PlayerID, shared.m_skillType, float.NaN);
                    //DataZDO.Vars.SetEstimatedSkillLevel(state.PlayerID, shared.m_skillType, estSkill);
                    if (!state.EstimatedSkillLevels.TryGetValue(shared.m_skillType, out var prevEstSkill))
                        prevEstSkill = float.NaN;
                    state.EstimatedSkillLevels[shared.m_skillType] = estSkill;
                    var intSkill = Mathf.Floor(estSkill * 100);
                    var intPrevSkill = Mathf.Floor(prevEstSkill * 100);
                    if (intSkill != intPrevSkill)
                        Logger.Log(intSkill - intPrevSkill > 1f ? LogLevel.Warning : LogLevel.Info, $"Player {state.PlayerName}: Estimated {shared.m_skillType} skill level: {intSkill}, Previous estimate: {intPrevSkill} (Item: {state.CheckSkillItem.name}, max stamina: {max}, used stamina: {eff})");
                }
                state.CheckSkillItem = null;
            }
        }

        if (Config.Players.StackInventoryIntoContainersEmote.Value is not ModConfig.PlayersConfig.DisabledEmote ||
            _backpackSlots > 0)
        {
            /// <see cref="Emote.DoEmote(Emotes)"/> <see cref="Player.StartEmote(string, bool)"/>
            if (zdo.Vars.GetEmoteID() is var emoteId && emoteId != state.LastEmoteId)
            {
                state.LastEmoteId = emoteId;

                static bool CheckEmote(ExtendedZDO player, Emotes emote)
                    => emote is not ModConfig.PlayersConfig.DisabledEmote && (emote is ModConfig.PlayersConfig.AnyEmote || emote == player.Vars.GetEmote());

                if (CheckEmote(zdo, Config.Players.StackInventoryIntoContainersEmote.Value))
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
                        container.Fields<Container>()
                            .Set(static () => x => x.m_width, 8)
                            .Set(static () => x => x.m_height, h);
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
                else if (_backpackSlots > 0 && CheckEmote(zdo, Config.Players.OpenBackpackEmote.Value))
                {
                    var backpackPrefab = Prefabs.PrivateChest;

                    state.OpenBackpackAfter = DateTimeOffset.UtcNow + OpenBackpackDelay;

                    var pos = zdo.GetPosition();
                    pos.y -= 0.6f;

                    static bool AdjustSize(ExtendedZDO zdo, int slots)
                    {
                        var fields = zdo.Fields<Container>();
                        var actualSlots = Math.Max(slots, zdo.Inventory.Items.Count);
                        var (width, height) = GetBackpackSize(actualSlots);
                        if ((fields!.SetIfChanged(static () => x => x.m_width, width),
                            fields.SetIfChanged(static () => x => x.m_height, height)) == (false, false))
                            return false;

                        if (actualSlots != slots)
                        {
                            using var enumerator = zdo.Inventory.Items.GetEnumerator();
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++)
                                {
                                    if (!enumerator.MoveNext())
                                    {
                                        zdo.ClaimOwnershipInternal();
                                        zdo.Inventory.Save();
                                        return true;
                                    }

                                    enumerator.Current.m_gridPos = new(x, y);
                                }
                            }
                        }
                        return true;
                    }

                    state.BackpackContainer ??= PlacedObjects.FirstOrDefault(x => x.PrefabInfo.Container is not null && x.IsModCreator(out var marker) && marker is CreatorMarkers.ProcessorOwned && x.Vars.GetPlayerID() == state.PlayerID);
                    if (state.BackpackContainer is null)
                    {
                        state.BackpackContainer = PlacePiece(pos, backpackPrefab, 0, CreatorMarkers.ProcessorOwned);
                        state.BackpackContainer.Vars.SetPlayerID(state.PlayerID);
                        state.BackpackContainer.Fields<Container>().Set(static () => x => x.m_name, "Backpack");
                        AdjustSize(state.BackpackContainer, _backpackSlots);
                        state.BackpackContainer.SetOwnerInternal(zdo.GetOwner());
                    }
#if DEBUG
                    else if (state.BackpackContainer.GetPrefab() != backpackPrefab)
                    {
                        DestroyObject(state.BackpackContainer);
                        state.BackpackContainer = null;
                    }
#endif
                    else if (Vector3.Distance(zdo.GetPosition(), state.BackpackContainer.GetPosition()) > InventoryGui.instance.m_autoCloseDistance
                        || AdjustSize(state.BackpackContainer, _backpackSlots))
                    {
                        state.BackpackContainer.SetPosition(pos);
                        state.BackpackContainer.SetOwnerInternal(zdo.GetOwner());
                        state.BackpackContainer = RecreatePiece(state.BackpackContainer);
                    }
                    else
                    {
                        state.OpenBackpackAfter = null;
                        RPC.OpenResponse(state.BackpackContainer, true);
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
            $"{total} of {_playerStates.Count} players want to sleep.<br>Sit down if you want to sleep as well");

        return false;
    }
}
