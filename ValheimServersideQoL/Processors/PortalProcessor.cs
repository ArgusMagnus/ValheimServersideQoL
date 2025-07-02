using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class PortalProcessor : Processor
{
    bool _destroyNewPortals;
    float _rangeSqr;
    readonly HashSet<ExtendedZDO> _initialPortals = [];
    readonly List<ItemDrop> _teleportableItems = [];
    readonly List<ContainerState> _containers = [];

    sealed class ContainerState(ExtendedZDO container, ExtendedZDO player, ExtendedZDO portal)
    {
        public ExtendedZDO Container { get; set; } = container;
        public ExtendedZDO Player { get; } = player;
        public Vector3 InitialPosition { get; } = player.GetPosition();
        public long PlayerID { get; } = player.Vars.GetPlayerID();
        public Vector3 PortalPosition { get; } = portal.GetPosition();
        public bool Stacked { get; set; }
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        foreach (var zdo in _initialPortals)
            zdo.Destroyed -= OnInitialPortalDestroyed;
        _initialPortals.Clear();

        _destroyNewPortals = Config.GlobalsKeys.NoPortalsPreventsContruction.Value && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals);
        if (_destroyNewPortals)
        {
            ZoneSystem.instance.RemoveGlobalKey(GlobalKeys.NoPortals);
            foreach (ExtendedZDO zdo in ZDOMan.instance.GetPortals())
            {
                _initialPortals.Add(zdo);
                zdo.Destroyed += OnInitialPortalDestroyed;
            }
        }

        _teleportableItems.Clear();
        if (!ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll))
        {
            foreach (var entry in Config.NonTeleportableItems.Entries)
            {
                if (string.IsNullOrEmpty(entry.Config.Value))
                    continue;

                if (ZoneSystem.instance.GetGlobalKey(entry.Config.Value))
                    _teleportableItems.Add(entry.ItemDrop);
            }
        }

        Logger.DevLog($"Count: {_teleportableItems.Count}");

        _rangeSqr = Config.NonTeleportableItems.PortalRange.Value;
        _rangeSqr *= _rangeSqr;
    }

    void OnInitialPortalDestroyed(ExtendedZDO zdo)
    {
        _initialPortals.Remove(zdo);
    }

    void OnContainerDestroyed(ExtendedZDO zdo)
    {
        for (int i = 0; i < _containers.Count; i++)
        {
            if (_containers[i].Container == zdo)
            {
                _containers.RemoveAt(i);
                return;
            }
        }
    }

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        for (int i = _containers.Count - 1; i >= 0; i--)
        {
            var state = _containers[i];

            if (state.Container.Inventory.Items.Count is 0)
                DestroyPiece(state.Container);
            else if (!state.Player.IsValid() || state.Player.PrefabInfo.Player is null)
            {
                if (!state.Stacked)
                    DestroyPiece(state.Container);
                else
                {
                    state.Container.ReleaseOwnership();
                    state.Container.SetPosition(state.InitialPosition);
                    state.Container.Vars.SetCreator(state.PlayerID);
                    state.Container.Fields<Container>().Set(x => x.m_autoDestroyEmpty, true);
                    _containers.RemoveAt(i);
                }
            }
            else if (state.Stacked)
            {
                if (Utils.DistanceSqr(state.PortalPosition, state.Player.GetPosition()) > _rangeSqr)
                {
                    if (state.Container.GetOwner() == state.Player.GetOwner() &&
                        ZNetScene.InActiveArea(ZoneSystem.GetZone(state.Container.GetPosition()), ZoneSystem.GetZone(state.Player.GetPosition())))
                    {
                        Logger.DevLog($"Take all: {state.Container.GetPosition()} / {state.Player.GetPosition()}");
                        RPC.TakeAllResponse(state.Container, true);
                    }
                    else
                    {
                        state.Container.SetOwner(state.Player.GetOwner());
                        state.Container.SetPosition(state.Player.GetPosition());
                        state.Container.Destroyed -= OnContainerDestroyed;
                        state.Container = RecreatePiece(state.Container);
                        state.Container.Destroyed += OnContainerDestroyed;
                    }
                }
            }
            else if (state.Container.Inventory.Items.Any(x => x is { m_gridPos.x: > 0 } or { m_stack: > 1 }))
            {
                for (int k = state.Container.Inventory.Items.Count - 1; k >= 0; k--)
                {
                    var item = state.Container.Inventory.Items[k];
                    if (item.m_gridPos.x is not 0)
                        continue;
                    if (--item.m_stack is 0)
                        state.Container.Inventory.Items.RemoveAt(k);
                }
                state.Container.Inventory.Save();
                state.Stacked = true;
                state.Container.Destroyed -= OnContainerDestroyed;
                state.Container = RecreatePiece(state.Container);
                state.Container.Destroyed += OnContainerDestroyed;
            }
            else if (Utils.DistanceSqr(state.PortalPosition, state.Player.GetPosition()) <= _rangeSqr)
            {
                if (state.Container.GetOwner() == state.Player.GetOwner() &&
                    ZNetScene.InActiveArea(ZoneSystem.GetZone(state.Container.GetPosition()), ZoneSystem.GetZone(state.Player.GetPosition())))
                {
                    RPC.StackResponse(state.Container, true);
                }
                else
                {
                    state.Container.SetOwner(state.Player.GetOwner());
                    state.Container.SetPosition(state.Player.GetPosition());
                    state.Container.Destroyed -= OnContainerDestroyed;
                    state.Container = RecreatePiece(state.Container);
                    state.Container.Destroyed += OnContainerDestroyed;
                }
            }
            else
            {
                DestroyPiece(state.Container);
            }
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (zdo.PrefabInfo.TeleportWorld is null)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (_destroyNewPortals && !_initialPortals.Contains(zdo) && zdo.Vars.GetCreator() != Main.PluginGuidHash)
        {
            RPC.Remove(zdo);

            /// <see cref="Player.TryPlacePiece(Piece)"/>
            var owner = zdo.GetOwner();
            if (owner is not 0)
                RPC.ShowMessage(owner, MessageHud.MessageType.Center, "$msg_nobuildzone");

            return false;
        }

        if (_teleportableItems.Count is 0 || zdo.Fields<TeleportWorld>().GetBool(x => x.m_allowAllItems))
            return false;
        
        foreach (var peer in peers)
        {
            if (Utils.DistanceSqr(zdo.GetPosition(), peer.m_refPos) > _rangeSqr || Instance<PlayerProcessor>().GetPeerCharacter(peer.m_uid) is not { } player)
                continue;

            if (_containers.Any(x => x.Player == player))
                continue;

            Logger.DevLog("Chest built");
            var container = PlacePiece(player.GetPosition() with { y = -1000 }, Prefabs.PrivateChest, 0);
            var h = Math.Max(4, _teleportableItems.Count);
            container.Fields<Container>().Set(x => x.m_width, 8).Set(x => x.m_height, h);
            int y = 0;
            foreach (var item in _teleportableItems)
            {
                var clone = item.m_itemData.Clone();
                clone.m_dropPrefab = item.gameObject;
                clone.m_stack = 1;
                clone.m_gridPos = new(0, y++);
                container.Inventory.Items.Add(clone);
            }
            container.Inventory.Save();
            container.SetOwner(peer.m_uid);
            _containers.Add(new(container, player, zdo));
            container.Destroyed += OnContainerDestroyed;
            RPC.StackResponse(container, true);
        }

        return false;
    }
}
