using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using static Skills;

namespace Valheim.ServersideQoL.Processors;

sealed partial class PlayerProcessor
{
    sealed class PlayerState(ExtendedZDO playerZDO, PlayerProcessor processor) : IPeerInfo
    {
        readonly PlayerProcessor _processor = processor;
        public long Owner { get; } = playerZDO.GetOwner();
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
        public int LastCraftingAnimation { get; set; } = 0;
        public Vector3? InitialInInteriorPosition { get; set; }
        public DateTimeOffset NextStaminaCheck { get; set; }
        public int Stamina { get; set; }
        public DateTimeOffset StaminaTimestamp { get; set; } = DateTimeOffset.UtcNow;
        public int Eitr { get; set; }
        public DateTimeOffset EitrTimestamp { get; set; } = DateTimeOffset.UtcNow;

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


        [MemberNotNull(nameof(BackpackContainer))]
        public bool EnsureBackpackExists()
        {
            var backpackPrefab = Prefabs.PrivateChest;

            var pos = PlayerZDO.GetPosition();
            pos.y -= 0.6f;

            static bool AdjustSize(ExtendedZDO zdo, int slots)
            {
                var fields = zdo.Fields<Container>();
                var actualSlots = Math.Max(slots, zdo.Inventory.Items.Count);
                var (width, height) = GetBackpackSize(actualSlots);
                if ((fields.UpdateValue(static () => x => x.m_width, width),
                    fields.UpdateValue(static () => x => x.m_height, height)) == (false, false))
                    return false;

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
                return true;
            }

            if (BackpackContainer is null)
            {
                BackpackContainer = _processor.PlacedObjects.FirstOrDefault(x => x.PrefabInfo.Container is not null && x.IsModCreator(out var marker) && marker is CreatorMarkers.ProcessorOwned && x.Vars.GetPlayerID() == PlayerID);
                BackpackContainer?.Fields<Container>().Set(static () => x => x.m_name, _processor.Config.Localization.Players.Backpack.Name);
            }
#if DEBUG
            else if (BackpackContainer.GetPrefab() != backpackPrefab)
            {
                _processor.DestroyObject(BackpackContainer);
                BackpackContainer = null;
            }
#endif

            if (BackpackContainer is null)
            {
                BackpackContainer = _processor.PlacePiece(pos, backpackPrefab, 0, CreatorMarkers.ProcessorOwned);
                BackpackContainer.Vars.SetPlayerID(PlayerID);
                BackpackContainer.Fields<Container>().Set(static () => x => x.m_name, _processor.Config.Localization.Players.Backpack.Name);
                AdjustSize(BackpackContainer, _processor._backpackSlots);
                BackpackContainer.SetOwnerInternal(Owner);
            }
            else if (Vector3.Distance(PlayerZDO.GetPosition(), BackpackContainer.GetPosition()) > InventoryGui.instance.m_autoCloseDistance
                || AdjustSize(BackpackContainer, _processor._backpackSlots))
            {
                BackpackContainer.SetPosition(pos);
                BackpackContainer.SetOwnerInternal(Owner);
                BackpackContainer = _processor.RecreatePiece(BackpackContainer);
            }
            else
            {
                return true;
            }
            return false;
        }

        public DateTimeOffset? OpenBackpackAfter { get; set; }

        public BuildModifiers BuildModifiers { get; set; }
        public LevelGroundModes LevelGroundMode { get; set; }
        public DateTimeOffset NextBuildModifierMessage { get; set; } = DateTimeOffset.MaxValue;
        public DateTimeOffset NextLevelGroundModeMessage { get; set; } = DateTimeOffset.MaxValue;

        public ItemDrop? LastUsedItem { get; set; }
        public ItemDrop? CheckSkillItem { get; set; }
        public float CheckSkillStaminaEitr { get; set; }
        public Dictionary<SkillType, float> EstimatedSkillLevels => field ??= [];
        public Dictionary<SkillType, (Queue<float> Queue, List<float> List)> EstimatedSkillLevelHistories => field ??= [];

        public float GetEstimatedSkillLevel(SkillType skillType)
        {
            if (!EstimatedSkillLevels.TryGetValue(skillType, out var level))
                EstimatedSkillLevels.Add(skillType, level = DataZDO.Vars.GetEstimatedSkillLevel(PlayerID, skillType, float.NaN));
            return level;
        }

        public ExtendedZDO? AttachedCart
        {
            get => field;
            set => (field = value)?.AssertIsAll<Vagon, Container>();
        }

        bool _hasChangedGlobalKeyModifications;
        Dictionary<GlobalKey, bool>? _globalKeyModifications;
        public IReadOnlyDictionary<GlobalKey, bool> GlobalKeyModifications => _globalKeyModifications ?? ReadOnlyDictionary<GlobalKey, bool>.Empty;

        public void AddGlobalKeyModification(GlobalKey key, bool add)
        {
            _globalKeyModifications ??= [];
            if (_globalKeyModifications.TryAdd(key, add))
                _hasChangedGlobalKeyModifications = true;
        }

        public void RemoveGlobalKeyModification(GlobalKey key)
        {
            if (_globalKeyModifications?.Remove(key) is true)
                _hasChangedGlobalKeyModifications = true;
        }

        public void SendGlobalKeyModifications()
        {
            if (!_hasChangedGlobalKeyModifications)
                return;
            _processor.Logger.DevLog($"Sending global key modifications to player {PlayerName}");
            ZoneSystem.instance.SendGlobalKeys(Owner);
            _hasChangedGlobalKeyModifications = false;
        }

        public TimeSpan? LastPing { get; private set; }
        public DateTimeOffset? LastPingTimestamp { get; private set; }
        public TimeSpan? PingMean { get; private set; }
        public TimeSpan? PingStdDev { get; private set; }
        public TimeSpan? PingJitter { get; private set; }
        public TimeSpan? PingEMA { get; private set; }
        public float ConnectionQuality { get; private set; }

        readonly Queue<TimeSpan> _pingHistory = [];
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
            var now = DateTimeOffset.UtcNow;
            LastPing = now - _pingStart;
            _pingStart = default;

            // exponential moving average
            if (PingEMA is null || LastPingTimestamp is null)
                PingEMA = LastPing;
            else
            {
                var dt = (now - LastPingTimestamp.Value).TotalSeconds;
                // alpha depends on elapsed time
                var alpha = 1.0 - Math.Exp(-dt / _processor._emaTau);
                PingEMA = alpha * LastPing.Value + (1.0 - alpha) * PingEMA.Value;
            }
            LastPingTimestamp = now;

            var cfg = _processor.Config.Networking;

            while (_pingHistory.Count >= cfg.PingStatisticsWindow.Value)
                _pingHistory.Dequeue();
            _pingHistory.Enqueue(LastPing ?? default);

            (PingMean, PingStdDev, PingJitter) = CalculateStats(_pingHistory);
            var connectionQuality =
                PingMean?.TotalMilliseconds * cfg.ConnectionQualityPingMeanWeight.Value +
                PingStdDev?.TotalMilliseconds * cfg.ConnectionQualityPingStdDevWeight.Value +
                PingJitter?.TotalMilliseconds * cfg.ConnectionQualityPingJitterWeight.Value +
                PingEMA?.TotalMilliseconds * cfg.ConnectionQualityPingEMAWeight.Value;
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
                    _processor.Logger.LogInfo(string.Format(cfg.LogPingFormat.Value, [PlayerName, LastPing?.TotalMilliseconds, PingMean?.TotalMilliseconds, PingStdDev?.TotalMilliseconds, PingJitter?.TotalMilliseconds, ConnectionQuality, PingEMA?.TotalMilliseconds]));
                else
                    _processor.Logger.LogInfo(string.Format(cfg.LogZoneOwnerPingFormat.Value, [PlayerName, LastPing?.TotalMilliseconds, PingMean?.TotalMilliseconds, PingStdDev?.TotalMilliseconds, PingJitter?.TotalMilliseconds, ConnectionQuality, ownerState.PlayerName, ownerState.LastPing?.TotalMilliseconds, ownerState.PingMean?.TotalMilliseconds, ownerState.PingStdDev?.TotalMilliseconds, ownerState.PingJitter?.TotalMilliseconds, ownerState.ConnectionQuality, PingEMA?.TotalMilliseconds, ownerState.PingEMA?.TotalMilliseconds]));
            }
            if (LastPing > TimeSpan.FromMilliseconds(cfg.ShowPingThreshold.Value) || ownerState?.LastPing > TimeSpan.FromMilliseconds(cfg.ShowZoneOwnerPingThreshold.Value))
            {
                if (ownerState is null)
                    RPC.ShowMessage(PlayerZDO.GetOwner(), MessageHud.MessageType.TopLeft, string.Format(cfg.ShowPingFormat.Value, [LastPing?.TotalMilliseconds, PingMean?.TotalMilliseconds, PingStdDev?.TotalMilliseconds, PingJitter?.TotalMilliseconds, ConnectionQuality, PingEMA?.TotalMilliseconds]));
                else
                    RPC.ShowMessage(PlayerZDO.GetOwner(), MessageHud.MessageType.TopLeft, string.Format(cfg.ShowZoneOwnerPingFormat.Value, [LastPing?.TotalMilliseconds, PingMean?.TotalMilliseconds, PingStdDev?.TotalMilliseconds, PingJitter?.TotalMilliseconds, ConnectionQuality, ownerState.PlayerName, ownerState.LastPing?.TotalMilliseconds, ownerState.PingMean?.TotalMilliseconds, ownerState.PingStdDev?.TotalMilliseconds, ownerState.PingJitter?.TotalMilliseconds, ownerState.ConnectionQuality, PingEMA?.TotalMilliseconds, ownerState.PingEMA?.TotalMilliseconds]));
            }

            static (TimeSpan? Mean, TimeSpan? StdDev, TimeSpan? Jitter) CalculateStats(Queue<TimeSpan> pingHistory)
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
}
