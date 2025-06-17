using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class ContainerProcessor : Processor
{
    readonly Dictionary<ItemKey, int> _stackPerItem = new();
    readonly Dictionary<ExtendedZDO, List<ExtendedZDO>> _signsByChests = [];
    readonly Dictionary<ExtendedZDO, ExtendedZDO> _chestsBySigns = [];

    public event Action<ExtendedZDO>? ContainerChanged;

    sealed class ContainerState
    {
        public HashSet<SharedItemDataKey> Items { get; } = [];
        public DateTimeOffset LastOwnershipRequest { get; set; }
        public bool WaitingForResponse { get; set; }
        public long PreviousOwner { get; set; }
        public SwapContentRequest? SwapContentRequest { get; set; }
    }

    readonly Dictionary<ExtendedZDO, ContainerState> _containers = [];
    public IReadOnlyCollection<ExtendedZDO> Containers => _containers.Keys;
    public ConcurrentDictionary<SharedItemDataKey, ConcurrentHashSet<ExtendedZDO>> ContainersByItemName { get; } = new();
    public IReadOnlyDictionary<ExtendedZDO, ExtendedZDO> ChestsBySigns => _chestsBySigns;
    public IReadOnlyDictionary<ExtendedZDO, List<ExtendedZDO>> SignsByChests => _signsByChests;
    bool _openResponseRegistered;

    sealed record SwapContentRequest(long SenderPeerID, ExtendedZDO From, ExtendedZDO? To)
    {
        public required DateTimeOffset SwapAfter { get; set; }
        public IReadOnlyList<ItemDrop.ItemData> FromItems { get; } = [.. From.Inventory.Items];
    }

    readonly List<SwapContentRequest> _swapContentRequests = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        foreach (var zdo in _chestsBySigns.Keys)
            zdo.Destroy();
        _signsByChests.Clear();
        _chestsBySigns.Clear();

        UpdateRpcSubscription("OpenRespons", RPC_OpenResponse, false);
        UpdateRpcSubscription("RPC_AnimateLever", RPC_AnimateLever, Config.Containers.ObliteratorItemTeleporter.Value is not ModConfig.ContainersConfig.ObliteratorItemTeleporterOptions.Disabled);
        UpdateRpcSubscription("RPC_AnimateLeverReturn", RPC_AnimateLeverReturn, Config.Containers.ObliteratorItemTeleporter.Value is not ModConfig.ContainersConfig.ObliteratorItemTeleporterOptions.Disabled);
        _openResponseRegistered = false;
    }

    void OnChestDestroyed(ExtendedZDO zdo)
    {
        if (_containers.Remove(zdo, out var state))
        {
            foreach (var key in state.Items)
            {
                if (ContainersByItemName.TryGetValue(key, out var set))
                {
                    set.Remove(zdo);
                    if (set.Count is 0)
                        ContainersByItemName.TryRemove(key, out _);
                }
            }
        }
        if (_signsByChests.Remove(zdo, out var signs))
        {
            foreach (var sign in signs)
            {
                _chestsBySigns.Remove(sign);
                sign.Destroy();
            }
        }
    }

    ModConfig.ContainersConfig.SignOptions GetSignOptions(int prefab)
    {
        if (prefab == Prefabs.WoodChest)
            return Config.Containers.WoodChestSigns.Value;
        if (prefab == Prefabs.ReinforcedChest)
            return Config.Containers.ReinforcedChestSigns.Value;
        if (prefab == Prefabs.BlackmetalChest)
            return Config.Containers.BlackmetalChestSigns.Value;
        if (prefab == Prefabs.Incinerator)
            return Config.Containers.ObliteratorSigns.Value;
        return default;
    }

    public override bool ClaimExclusive(ExtendedZDO zdo) => false;

    public void RequestOwnership(ExtendedZDO zdo, long playerID, [CallerFilePath]string caller = default!, [CallerLineNumber] int callerLineNo = default)
        => RequestOwnership(zdo, playerID, _containers[zdo], caller, callerLineNo);

    void RequestOwnership(ExtendedZDO zdo, long playerID, ContainerState state, [CallerFilePath] string caller = default!, [CallerLineNumber] int callerLineNo = default)
    {
        if (zdo.IsOwnerOrUnassigned() || (DateTimeOffset.UtcNow - state.LastOwnershipRequest < TimeSpan.FromSeconds(1)))
            return;

        if (!_openResponseRegistered && Player.m_localPlayer is not null)
        {
            _openResponseRegistered = true;
            /// <see cref="Container.RPC_OpenRespons"/>
            UpdateRpcSubscription("OpenRespons", RPC_OpenResponse, true);
        }

        //Logger.DevLog($"Container {zdo.m_uid}: RequestOwnership");
        state.LastOwnershipRequest = DateTimeOffset.UtcNow;
        state.WaitingForResponse = true;
        state.PreviousOwner = zdo.GetOwner();

        DevShowMessage(zdo, $"Requesting ownership: {Path.GetFileNameWithoutExtension(caller)} L{callerLineNo}");
        RPC.RequestOwn(zdo, playerID);
    }

    bool RPC_OpenResponse(ExtendedZDO? zdo, bool granted)
    {
        if (zdo is null || !_containers.TryGetValue(zdo, out var state) || !state.WaitingForResponse)
            return true;

        //Logger.DevLog($"Container {data.m_targetZDO}: OpenResponse: {granted}");
        state.WaitingForResponse = false;
        return false;
    }

    void RPC_AnimateLever(ExtendedZDO zdo, ZRoutedRpc.RoutedRPCData data)
    {
        if (zdo is not { PrefabInfo.Container.Incinerator.Value: not null } || zdo.Inventory.TeleportTag is null || zdo.Inventory.Items.Count is 0)
            return;

        if (!_containers.TryGetValue(zdo, out var state))
        {
            _containers.Add(zdo, state = new());
            zdo.Destroyed += OnChestDestroyed;
        }

        var (other, otherState) = _containers.FirstOrDefault(x => !ReferenceEquals(x.Key, zdo) && x.Key.Inventory.TeleportTag == zdo.Inventory.TeleportTag);

        zdo.ReleaseOwnership(); /// cancel obliteration of items <see cref="Incinerator.Incinerate(long)"/>
        other?.ReleaseOwnership();

        var request = new SwapContentRequest(data.m_senderPeerID, zdo, other) { SwapAfter = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(zdo.PrefabInfo.Container.Value.Incinerator.Value!.m_effectDelayMax + 0.2) };
        _swapContentRequests.Add(request);
        state.SwapContentRequest = request;
        if (otherState is not null)
            otherState.SwapContentRequest = request;
    }

    void RPC_AnimateLeverReturn(ExtendedZDO zdo)
    {
        for (int i = 0; i < _swapContentRequests.Count; i++)
        {
            var request = _swapContentRequests[i];
            if (ReferenceEquals(request.From, zdo))
                request.SwapAfter = DateTimeOffset.UtcNow.AddMilliseconds(200);
        }
    }

    bool CheckForbiddenItems(IEnumerable<ItemDrop.ItemData> from, IEnumerable<ItemDrop.ItemData> to)
    {
        switch (Config.Containers.ObliteratorItemTeleporter.Value)
        {
            case ModConfig.ContainersConfig.ObliteratorItemTeleporterOptions.EnabledAllItems:
                return false;
            case ModConfig.ContainersConfig.ObliteratorItemTeleporterOptions.Enabled:
                if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll))
                    return false;
                break;
        }

        static bool HasNonTeleportableItems(IEnumerable<ItemDrop.ItemData> items)
        {
            foreach (var item in items)
            {
                if (!item.m_shared.m_teleportable)
                    return true;
            }
            return false;
        }

        return HasNonTeleportableItems(from) || HasNonTeleportableItems(to);
    }

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        for (int i = _swapContentRequests.Count - 1; i >= 0; i--)
        {
            var request = _swapContentRequests[i];
            if (request.From.GetOwner() is not 0 || request.To?.GetOwner() is not null and not 0)
            {
                request.From.ReleaseOwnership();
                request.To?.ReleaseOwnership();
            }
            else if (request.SwapAfter <= DateTimeOffset.UtcNow)
            {
                if (request.To is null)
                    ShowMessage(peers, request.From, $"No target with tag '{request.From.Inventory.TeleportTag}' found", Config.Containers.ObliteratorItemTeleporterMessageType.Value, DamageText.TextType.Bonus);
                else if (CheckForbiddenItems(request.FromItems, request.To.Inventory.Items))
                    ShowMessage(peers, request.From, "An item prevents the teleportation", Config.Containers.ObliteratorItemTeleporterMessageType.Value, DamageText.TextType.Bonus);
                else
                {
                    var toItems = request.To.Inventory.Items.ToList();
                    request.From.Inventory.Items.Clear();
                    request.To.Inventory.Items.Clear();
                    foreach (var item in request.FromItems)
                        request.To.Inventory.Items.Add(item);
                    foreach (var item in toItems)
                        request.From.Inventory.Items.Add(item);
                    request.To.Inventory.Save();
                    request.From.Inventory.Save();
                    ShowMessage(peers, request.From, "Items teleported", Config.Containers.ObliteratorItemTeleporterMessageType.Value, DamageText.TextType.Weak);
                    ShowMessage(peers, request.To, "Items teleported", Config.Containers.ObliteratorItemTeleporterMessageType.Value, DamageText.TextType.Weak);
                }
                request.From.SetOwner(request.SenderPeerID);
                _swapContentRequests.RemoveAt(i);
            }
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (zdo.PrefabInfo.Container is null || zdo.Vars.GetCreator() is 0)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (!_containers.TryGetValue(zdo, out var state))
        {
            _containers.Add(zdo, state = new());
            zdo.Destroyed += OnChestDestroyed;
        }

        if (state.SwapContentRequest is not null)
        {
            if (state.SwapContentRequest.SwapAfter < DateTimeOffset.UtcNow)
                state.SwapContentRequest = null;
            else
                return false;
        }

        var signOptions = GetSignOptions(zdo.GetPrefab());

        if (signOptions is not ModConfig.ContainersConfig.SignOptions.None && !_signsByChests.ContainsKey(zdo) && Config.Containers.ChestSignOffsets.TryGetValue(zdo.GetPrefab(), out var signOffset))
        {
            var p = zdo.GetPosition();
            var r = zdo.GetRotation();
            var rot = r.eulerAngles.y + 90;
            var signs = new List<ExtendedZDO>(4);
            var text = zdo.Vars.GetText();
            if (string.IsNullOrEmpty(text))
                text = Config.Containers.ChestSignsDefaultText.Value;
            ExtendedZDO sign;
            p.y += signOffset.Top / 2;
            if (signOptions.HasFlag(ModConfig.ContainersConfig.SignOptions.Left))
            {
                sign = PlacePiece(p + r * Vector3.right * signOffset.Left, Prefabs.Sign, rot);
                sign.Vars.SetText(text);
                signs.Add(sign);
                _chestsBySigns.Add(sign, zdo);
            }
            if (signOptions.HasFlag(ModConfig.ContainersConfig.SignOptions.Right))
            {
                sign = PlacePiece(p + r * Vector3.left * signOffset.Right, Prefabs.Sign, rot + 180);
                sign.Vars.SetText(text);
                signs.Add(sign);
                _chestsBySigns.Add(sign, zdo);
            }
            if (signOptions.HasFlag(ModConfig.ContainersConfig.SignOptions.Front))
            {
                sign = PlacePiece(p + r * Vector3.forward * signOffset.Front, Prefabs.Sign, rot + 270);
                sign.Vars.SetText(text);
                signs.Add(sign);
                _chestsBySigns.Add(sign, zdo);
            }
            if (signOptions.HasFlag(ModConfig.ContainersConfig.SignOptions.Back))
            {
                sign = PlacePiece(p + r * Vector3.back * signOffset.Back, Prefabs.Sign, rot + 90);
                sign.Vars.SetText(text);
                signs.Add(sign);
                _chestsBySigns.Add(sign, zdo);
            }
            p = zdo.GetPosition();
            p.y += signOffset.Top;
            if (signOptions.HasFlag(ModConfig.ContainersConfig.SignOptions.TopLongitudinal))
            {
                sign = PlacePiece(p, Prefabs.Sign, Quaternion.Euler(-90, rot - 90, 0));
                sign.Vars.SetText(text);
                signs.Add(sign);
                _chestsBySigns.Add(sign, zdo);
            }
            if (signOptions.HasFlag(ModConfig.ContainersConfig.SignOptions.TopLateral))
            {
                sign = PlacePiece(p, Prefabs.Sign, Quaternion.Euler(-90, rot, 0));
                sign.Vars.SetText(text);
                signs.Add(sign);
                _chestsBySigns.Add(sign, zdo);
            }
            _signsByChests.Add(zdo, signs);
        }

        var fields = zdo.Fields<Container>();
        var inventory = zdo.Inventory!;
        var width = inventory.Inventory.GetWidth();
        var height = inventory.Inventory.GetHeight();
        if (Config.Containers.ContainerSizes.TryGetValue(zdo.GetPrefab(), out var sizeCfg)
            && sizeCfg.Value.Split(['x'], 2) is { Length: 2 } parts
            && int.TryParse(parts[0], out var desiredWidth)
            && int.TryParse(parts[1], out var desiredHeight)
            && (width, height) != (desiredWidth, desiredHeight))
        {
            if (zdo.Inventory is { Items.Count: 0 })
            {
                fields.Set(x => x.m_width, width = desiredWidth);
                fields.Set(x => x.m_height, height = desiredHeight);
                RecreateZdo = true;
                if (zdo.PrefabInfo.Container is { ZSyncTransform.Value: not null })
                    zdo.ReleaseOwnershipInternal(); // required for physics to work again
                return false;
            }
        }
        else
        {
            desiredWidth = width;
            desiredHeight = height;
        }

        //if (!CheckMinDistance(peers, zdo))
        //    return false;

        if (zdo.Vars.GetInUse())
            return true; // in use or player to close

        if (inventory is { Items.Count: 0 })
        {
            ContainerChanged?.Invoke(zdo);
            return true;
        }

        if ((width, height) != (desiredWidth, desiredHeight))
        {
            RecreateZdo = true;
            if (inventory.Items.Count > desiredWidth * desiredHeight)
            {
                var found = false;
                for (var h = desiredHeight; !found && h <= height; h++)
                {
                    for (var w = desiredWidth; !found && w <= width; w++)
                    {
                        if (inventory.Items.Count <= w * h)
                        {
                            found = true;
                            (desiredWidth, desiredHeight) = (w, h);
                        }
                    }
                }

                if (!found || (width, height) == (desiredWidth, desiredHeight))
                    RecreateZdo = false;
            }
            if (RecreateZdo)
            {
                fields.Set(x => x.m_width, width = desiredWidth);
                fields.Set(x => x.m_height, height = desiredHeight);
            }
        }

        var changed = false;
        ItemDrop.ItemData? lastPartialSlot = null;
        _stackPerItem.Clear();
        foreach (var item in inventory.Items
            .OrderBy(x => x.IsEquipable() ? 0 : 1)
            .ThenBy(x => x.m_shared.m_name)
            .ThenByDescending(x => x.m_stack))
        {
            state.Items.Add(item.m_shared);
            if (zdo.PrefabInfo.Container.Value.Container.m_privacy is not Container.PrivacySetting.Private)
            {
                var set = ContainersByItemName.GetOrAdd(item.m_shared, static _ => []);
                set.Add(zdo);
            }
            if (!Config.Containers.AutoSort.Value && !RecreateZdo)
                continue;

            if (lastPartialSlot is not null && new ItemKey(item) == lastPartialSlot)
            {
                changed = true;
                if (!zdo.IsOwnerOrUnassigned())
                    break;
                else
                {
                    var diff = Math.Min(item.m_stack, lastPartialSlot.m_shared.m_maxStackSize - lastPartialSlot.m_stack);
                    lastPartialSlot.m_stack += diff;
                    item.m_stack -= diff;
                }
            }

            if (item.m_stack is 0)
                continue;

            if (!_stackPerItem.TryGetValue(item, out var stackCount))
                stackCount = 0;
            _stackPerItem[item] = stackCount + 1;

            if (item.m_stack < item.m_shared.m_maxStackSize)
                lastPartialSlot = item;
        }

        if (changed && zdo.IsOwnerOrUnassigned())
        {
            for (int i = inventory.Items.Count - 1; i >= 0; i--)
            {
                if (inventory.Items[i].m_stack is 0)
                    inventory.Items.RemoveAt(i);
            }
        }

        if (_stackPerItem.Count > 0)
        {
            if (_stackPerItem.Values.Sum(x => (int)Math.Ceiling((double)x / width)) <= height)
            {
                var x = -1;
                var y = 0;
                ItemKey? lastKey = null;
                foreach (var item in inventory.Items
                    .OrderBy(x => x.IsEquipable() ? 0 : 1)
                    .ThenBy(x => x.m_shared.m_name)
                    .ThenByDescending(x => x.m_stack))
                {
                    if (++x >= width || (lastKey.HasValue && lastKey != item))
                    {
                        x = 0;
                        y++;
                    }
                    if (item.m_gridPos.x != x || item.m_gridPos.y != y)
                    {
                        changed = true;
                        if (zdo.IsOwnerOrUnassigned())
                            item.m_gridPos = new(x, y);
                    }
                    lastKey = item;
                }
            }
            else if (_stackPerItem.Values.Sum(x => (int)Math.Ceiling((double)x / height)) <= width)
            {
                var x = 0;
                var y = height;
                ItemKey? lastKey = null;
                foreach (var item in inventory.Items
                    .OrderBy(x => x.IsEquipable() ? 0 : 1)
                    .ThenBy(x => x.m_shared.m_name)
                    .ThenByDescending(x => x.m_stack))
                {
                    if (--y < 0 || (lastKey.HasValue && lastKey != item))
                    {
                        y = height - 1;
                        x++;
                    }
                    if (item.m_gridPos.x != x || item.m_gridPos.y != y)
                    {
                        changed = true;
                        if (zdo.IsOwnerOrUnassigned())
                            item.m_gridPos = new(x, y);
                    }
                    lastKey = item;
                }
            }
            else
            {
                var x = 0;
                var y = 0;
                foreach (var item in inventory.Items
                    .OrderBy(x => x.IsEquipable() ? 0 : 1)
                    .ThenBy(x => x.m_shared.m_name)
                    .ThenByDescending(x => x.m_stack))
                {
                    if (item.m_gridPos.x != x || item.m_gridPos.y != y)
                    {
                        changed = true;
                        if (zdo.IsOwnerOrUnassigned())
                            item.m_gridPos = new(x, y);
                    }
                    if (++x >= width)
                    {
                        x = 0;
                        y++;
                    }
                }
            }
        }

        if (changed)
        {
            if (!zdo.IsOwnerOrUnassigned())
                RequestOwnership(zdo, zdo.Vars.GetCreator(), state);
            else
            {
                inventory.Save();
                ShowMessage(peers, zdo, $"{zdo.PrefabInfo.Container.Value.Piece.m_name} sorted", Config.Containers.SortedMessageType.Value);
            }
        }

        if (!RecreateZdo)
            ContainerChanged?.Invoke(zdo);
        else if (zdo.PrefabInfo.Container is { ZSyncTransform.Value: not null })
            zdo.ReleaseOwnership(); // required for physics to work again

        return true;
    }

    protected override void PostProcessCore()
    {
        //if (!ZNet.instance.IsDedicated())
        //    return;

        //foreach (var (zdo, state) in _containers)
        //{
        //    if (!zdo.IsOwner() || _swapContentRequests.Any(x => ReferenceEquals(zdo, x.From) || ReferenceEquals(zdo, x.To)))
        //        continue;
        //    //Logger.DevLog($"Setting owner for {zdo.m_uid} to {state.PreviousOwner}");
        //    //zdo.SetOwner(state.PreviousOwner);
        //    zdo.SetOwner(0);
        //}
    }
}
