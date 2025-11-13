using UnityEngine;
using Valheim.ServersideQoL.HarmonyPatches;

namespace Valheim.ServersideQoL.Processors;

sealed class PortalProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("a59669f7-3573-4ece-9ec3-d42e67a772c1");

    bool _destroyNewPortals;
    float _rangeSqr;
    readonly HashSet<ExtendedZDO> _initialPortals = [];
    readonly List<ContainerState> _containers = [];

    sealed class ContainerState(ExtendedZDO container, Peer peer, ExtendedZDO player, ExtendedZDO portal)
    {
        public Peer Peer { get; } = peer;
        public ExtendedZDO Container { get; set; } = container;
        public ExtendedZDO Player { get; } = player;
        public Vector3 InitialPosition { get; } = player.GetPosition();
        public long PlayerID { get; } = player.Vars.GetPlayerID();
        public Vector3 PortalPosition { get; } = portal.GetPosition();
        public bool Stacked { get; set; }
        public DateTimeOffset NextRequest { get => field; set { field = value; DestroyAfter = value.AddSeconds(5); } }
        public DateTimeOffset DestroyAfter { get; private set; }
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

        _rangeSqr = Config.NonTeleportableItems.PortalRange.Value;
        _rangeSqr *= _rangeSqr;

        if (!firstTime)
            return;

        _containers.Clear();
    }

    void OnInitialPortalDestroyed(ExtendedZDO zdo)
    {
        _initialPortals.Remove(zdo);
    }

    void OnContainerDestroyed(ExtendedZDO zdo)
    {
        for (int i = 0; i < _containers.Count; i++)
        {
            var state = _containers[i];
            if (state.Container == zdo)
            {
                state.Player.Destroyed -= OnPlayerDestroyed;
                _containers.RemoveAt(i);
                return;
            }
        }
    }

    void OnPlayerDestroyed(ExtendedZDO zdo)
    {
        for (int i = 0; i < _containers.Count; i++)
        {
            var state = _containers[i];
            if (state.Player == zdo)
            {
                state.Container.Destroyed -= OnContainerDestroyed;
                if (!state.Stacked)
                    DestroyObject(state.Container);
                else
                {
                    state.Container.ReleaseOwnershipInternal();
                    state.Container.SetPosition(state.InitialPosition with { y = -1000 });
                    state.Container.Fields<Container>().Set(static () => x => x.m_autoDestroyEmpty, true);
                    state.Container.CreateClone();
                    DestroyObject(state.Container); // release exclusive claim
                }
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
                DestroyObject(state.Container);
            else if (state.Stacked)
            {
                if (Utils.DistanceSqr(state.PortalPosition, state.Player.GetPosition()) > _rangeSqr)
                {
                    if (state.Container.GetOwner() == state.Player.GetOwner() &&
                        ZNetScene.InActiveArea(state.Container.GetSector(), state.Player.GetSector()))
                    {
                        var now = DateTimeOffset.UtcNow;
                        if (now > state.NextRequest)
                        {
                            state.NextRequest = now.AddMilliseconds(200);
                            RPC.TakeAllResponse(state.Container, true);
                            ShowMessage([state.Peer], state.PortalPosition, Config.Localization.NonTeleportableItems.ItemsReturned, Config.NonTeleportableItems.MessageType.Value);
                        }
                    }
                    else
                    {
                        state.Container.SetOwnerInternal(state.Player.GetOwner());
                        state.Container.SetPosition(state.Player.GetPosition() with { y = -1000 });
                        state.Container.Destroyed -= OnContainerDestroyed;
                        state.Container = RecreatePiece(state.Container);
                        state.Container.UnregisterAllProcessors();
                        state.Container.Destroyed += OnContainerDestroyed;
                    }
                }
            }
            else if (state.Container.Inventory.Items.Any(static x => x is { m_gridPos.x: > 0 } or { m_stack: > 1 }))
            {
                int count = 0;
                for (int k = state.Container.Inventory.Items.Count - 1; k >= 0; k--)
                {
                    var item = state.Container.Inventory.Items[k];
                    if (item.m_gridPos.x is not 0)
                        continue;
                    if (--item.m_stack is 0)
                        state.Container.Inventory.Items.RemoveAt(k);
                    count += item.m_stack;
                }
                state.Container.Inventory.Save();
                state.Container.Vars.SetReturnContentToCreator(true);
                state.Container.Vars.SetCreator(state.PlayerID);
                state.Stacked = true;
                state.Container.Destroyed -= OnContainerDestroyed;
                state.Container = RecreatePiece(state.Container);
                state.Container.UnregisterAllProcessors();
                state.Container.Destroyed += OnContainerDestroyed;
                //if (Config.NonTeleportableItems.MessageType.Value is not MessageTypes.CenterNear and not MessageTypes.CenterFar)
                //    RPC.ShowMessage(state.Player.GetOwner(), MessageHud.MessageType.Center, "");
                ShowMessage([state.Peer], state.PortalPosition, Config.Localization.NonTeleportableItems.FormatItemsTaken(count), Config.NonTeleportableItems.MessageType.Value);
                state.NextRequest = DateTimeOffset.UtcNow.AddSeconds(1);
            }
            else if (Utils.DistanceSqr(state.PortalPosition, state.Player.GetPosition()) <= _rangeSqr)
            {
                var now = DateTimeOffset.UtcNow;
                if (now > state.NextRequest)
                {
                    state.NextRequest = now.AddMilliseconds(200);
                    if (state.Container.GetOwner() != state.Player.GetOwner() ||
                        !ZNetScene.InActiveArea(state.Container.GetSector(), state.Player.GetSector()))
                    {
                        state.Container.SetOwnerInternal(state.Player.GetOwner());
                        state.Container.SetPosition(state.Player.GetPosition() with { y = -1000 });
                        state.Container.Destroyed -= OnContainerDestroyed;
                        state.Container = RecreatePiece(state.Container);
                        state.Container.UnregisterAllProcessors();
                        state.Container.Destroyed += OnContainerDestroyed;
                    }
                    RPC.StackResponse(state.Container, true);
                    RPC.ShowMessage(state.Player.GetOwner(), MessageHud.MessageType.Center, "");
                }
            }
            else if (DateTimeOffset.UtcNow > state.DestroyAfter)
            {
                DestroyObject(state.Container);
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

        if (_destroyNewPortals && !_initialPortals.Contains(zdo) && !zdo.IsModCreator())
        {
            RPC.Remove(zdo);

            /// <see cref="Player.TryPlacePiece(Piece)"/>
            var owner = zdo.GetOwner();
            if (owner is not 0)
                RPC.ShowMessage(owner, MessageHud.MessageType.Center, "$msg_nobuildzone");

            return false;
        }

        if (zdo.Fields<TeleportWorld>().GetBool(static () => x => x.m_allowAllItems))
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (TeleportableItems.Count is 0)
            return false;
        
        foreach (var peer in peers.AsEnumerable())
        {
            if (Instance<PlayerProcessor>().GetPeerCharacter(peer.m_uid) is not { } player)
                continue;
            if (Utils.DistanceSqr(zdo.GetPosition(), player.GetPosition()) > _rangeSqr)
                continue;
            if (_containers.Any(x => x.Player == player))
                continue;

            var container = PlacePiece(player.GetPosition() with { y = -1000 }, Prefabs.PrivateChest, 0);
            container.UnregisterAllProcessors();
            var h = Math.Max(4, TeleportableItems.Count);
            container.Fields<Container>()
                .Set(static () => x => x.m_width, 8)
                .Set(static () => x => x.m_height, h);
            int y = 0;
            foreach (var (item, dropPrefab) in TeleportableItems)
            {
                var clone = item.Clone();
                clone.m_dropPrefab = dropPrefab;
                clone.m_stack = 1;
                clone.m_gridPos = new(0, y++);
                container.Inventory.Items.Add(clone);
            }
            container.Inventory.Save();
            container.SetOwner(peer.m_uid);
            _containers.Add(new(container, peer, player, zdo) { NextRequest = DateTimeOffset.UtcNow.AddMilliseconds(200) });
            container.Destroyed += OnContainerDestroyed;
            if (!peer.IsServer)
                player.Destroyed += OnPlayerDestroyed;
            RPC.StackResponse(container, true);
            RPC.ShowMessage(player.GetOwner(), MessageHud.MessageType.Center, "");
        }

        return false;
    }
}
