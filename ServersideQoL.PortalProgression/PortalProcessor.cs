using UnityEngine;

namespace ServersideQoL.PortalProgression;

[Processor("87d052fd-a4dc-4095-9d5d-7fb765f02f52")]
[DependsOn<PlayerRegistryProcessor>]
[DependsOn<ContainerRegistryProcessor>]
public sealed class PortalProcessor : Processor<PortalProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(TeleportWorld TeleportWorld) : ProcessorPrefabInfo
  {
    public override bool IsValid => !TeleportWorld.m_allowAllItems;
  }

  static int ContainerPrefabHash => Prefabs.PrivateChest;
  readonly Dictionary<ItemDrop.ItemData, GameObject> _teleportableItems = new(SharedItemDataKeyComparer.Instance);
  readonly List<State> _containers = [];
  float _rangeSqr;

  public IReadOnlyDictionary<ItemDrop.ItemData, GameObject> TeleportableItems => _teleportableItems;

  protected override void Initialize()
  {
    _rangeSqr = Config.Instance.PortalRange.Value;
    _rangeSqr *= _rangeSqr;
    _containers.Clear();

    ServersideQoLPlugin.Instance.GlobalKeysChanged -= UpdateTeleportableItems;
    UpdateTeleportableItems();
    ServersideQoLPlugin.Instance.GlobalKeysChanged += UpdateTeleportableItems;
  }

  protected override void PreProcess(PeersEnumerable peers)
  {

    for (int i = _containers.Count - 1; i >= 0; i--)
    {
      var state = _containers[i];
      var inventory = state.Container.GetInventory();

      if (inventory.Items.Count is 0)
        DestroyObject(state.Container.ZDO);
      else if (state.Stacked)
      {
        if (Utils.DistanceSqr(state.PortalPosition, state.Player.ZDO.ZDO.GetPosition()) > _rangeSqr)
        {
          if (state.Container.ZDO.ZDO.GetOwner() == state.Player.Owner &&
              ZNetScene.InActiveArea(state.Container.ZDO.ZDO.GetSector(), state.Player.ZDO.ZDO.GetSector()))
          {
            var now = Timestamp.Now;
            if (now > state.NextRequest)
            {
              state.NextRequest = now.AddSeconds(0.2f);
              RPC.TakeAllResponse(state.Container.ZDO, true);
              ShowMessage([state.Peer], state.PortalPosition, Config.Instance.Localization.Value.ItemsReturned, Config.Instance.MessageType.Value);
            }
          }
          else
          {
            state.Container.ZDO.ZDO.SetOwnerInternal(state.Player.Owner);
            state.Container.ZDO.ZDO.SetPosition(state.Player.ZDO.ZDO.GetPosition() with { y = -1000 });
            state.Container.ZDO.Destroyed -= OnContainerDestroyed;
            state.Container = Instance<ContainerRegistryProcessor>().GetState(RecreatePiece(state.Container.ZDO))!;
            state.Container.ZDO.UnregisterAll();
            state.Container.ZDO.Destroyed += OnContainerDestroyed;
          }
        }
      }
      else if (inventory.Items.Any(static x => x is { m_gridPos.x: > 0 } or { m_stack: > 1 }))
      {
        int count = 0;
        for (int k = inventory.Items.Count - 1; k >= 0; k--)
        {
          var item = inventory.Items[k];
          if (item.m_gridPos.x is not 0)
            continue;
          if (--item.m_stack is 0)
            inventory.Items.RemoveAt(k);
          count += item.m_stack;
        }
        inventory.Save();
        Instance<ContainerRegistryProcessor>().SetReturnContentToCreator(state.Container.ZDO, true);
        state.Container.ZDO.Vars.SetCreator(state.Player.PlayerID);
        state.Stacked = true;
        state.Container.ZDO.Destroyed -= OnContainerDestroyed;
        state.Container = Instance<ContainerRegistryProcessor>().GetState(RecreatePiece(state.Container.ZDO))!;
        state.Container.ZDO.UnregisterAll();
        state.Container.ZDO.Destroyed += OnContainerDestroyed;
        //if (Config.NonTeleportableItems.MessageType.Value is not MessageTypes.CenterNear and not MessageTypes.CenterFar)
        //    RPC.ShowMessage(state.Player.GetOwner(), MessageHud.MessageType.Center, "");
        ShowMessage([state.Peer], state.PortalPosition, Config.Instance.Localization.Value.FormatItemsTaken(count), Config.Instance.MessageType.Value);
        state.NextRequest = Timestamp.Now.AddSeconds(1);
      }
      else if (Utils.DistanceSqr(state.PortalPosition, state.Player.ZDO.ZDO.GetPosition()) <= _rangeSqr)
      {
        var now = Timestamp.Now;
        if (now > state.NextRequest)
        {
          state.NextRequest = now.AddSeconds(0.2f);
          if (state.Container.ZDO.ZDO.GetOwner() != state.Player.Owner ||
              !ZNetScene.InActiveArea(state.Container.ZDO.ZDO.GetSector(), state.Player.ZDO.ZDO.GetSector()))
          {
            state.Container.ZDO.ZDO.SetOwnerInternal(state.Player.Owner);
            state.Container.ZDO.ZDO.SetPosition(state.Player.ZDO.ZDO.GetPosition() with { y = -1000 });
            state.Container.ZDO.Destroyed -= OnContainerDestroyed;
            state.Container = Instance<ContainerRegistryProcessor>().GetState(RecreatePiece(state.Container.ZDO))!;
            state.Container.ZDO.UnregisterAll();
            state.Container.ZDO.Destroyed += OnContainerDestroyed;
          }
          RPC.StackResponse(state.Container.ZDO, true);
          RPC.ShowMessage(state.Player.Owner, MessageHud.MessageType.Center, "");
        }
      }
      else if (Timestamp.Now > state.DestroyAfter)
      {
        DestroyObject(state.Container.ZDO);
      }
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (zdo.Fields<TeleportWorld>().GetBool(static () => x => x.m_allowAllItems))
      return ProcessResult.UnregisterProcessor;

    if (_teleportableItems.Count is 0)
      return ProcessResult.ScheduleReprocessing;

    foreach (var peer in peers.Enumerate())
    {
      if (peer.PlayerState is not { } player)
        continue;
      if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), player.ZDO.ZDO.GetPosition()) > _rangeSqr)
        continue;
      if (_containers.Any(x => x.Player == player))
        continue;

      var container = PlacePiece(player.ZDO.ZDO.GetPosition() with { y = -1000 }, Prefabs.PrivateChest, 0);
      container.UnregisterAll();
      var h = Math.Max(4, TeleportableItems.Count);
      container.Fields<Container>()
          .Set(static () => x => x.m_width, 8)
          .Set(static () => x => x.m_height, h);
      int y = 0;
      var containerState = Instance<ContainerRegistryProcessor>().GetState(container)!;
      var inventory = containerState.GetInventory();
      foreach (var (item, dropPrefab) in TeleportableItems)
      {
        var clone = item.Clone();
        clone.m_dropPrefab = dropPrefab;
        clone.m_stack = 1;
        clone.m_gridPos = new(0, y++);
        inventory.Items.Add(clone);
      }
      inventory.Save();
      container.ZDO.SetOwnerInternal(peer.ZNetPeer.m_uid);
      _containers.Add(new(peer, containerState, player, zdo) { NextRequest = Timestamp.Now.AddSeconds(0.2f) });
      container.Destroyed += OnContainerDestroyed;
      if (!peer.ZNetPeer.m_server)
        player.ZDO.Destroyed += OnPlayerDestroyed;
      RPC.StackResponse(container, true);
      RPC.ShowMessage(player.Owner, MessageHud.MessageType.Center, "");
    }

    return ProcessResult.ScheduleReprocessing;
  }

  void UpdateTeleportableItems()
  {
    _teleportableItems.Clear();
    if (!Config.Instance.Enabled.Value)
    {
      ServersideQoLPlugin.Instance.GlobalKeysChanged -= UpdateTeleportableItems;
      return;
    }

    foreach (var entry in Config.Instance.Entries)
    {
      if (string.IsNullOrEmpty(entry.Config.Value))
        continue;

      if (ZoneSystem.instance.GetGlobalKey(entry.Config.Value))
        _teleportableItems.Add(entry.ItemDrop.m_itemData, entry.ItemDrop.gameObject);
    }
  }

  void OnContainerDestroyed(ServersideQoLZDO zdo)
  {
    for (int i = 0; i < _containers.Count; i++)
    {
      var state = _containers[i];
      if (state.Container.ZDO == zdo)
      {
        state.Player.ZDO.Destroyed -= OnPlayerDestroyed;
        _containers.RemoveAt(i);
        return;
      }
    }
  }

  void OnPlayerDestroyed(ServersideQoLZDO zdo)
  {
    for (int i = 0; i < _containers.Count; i++)
    {
      var state = _containers[i];
      if (state.Player.ZDO == zdo)
      {
        state.Container.ZDO.Destroyed -= OnContainerDestroyed;
        if (!state.Stacked)
          DestroyObject(state.Container.ZDO);
        else
        {
          state.Container.ZDO.ReleaseOwnershipInternal();
          state.Container.ZDO.ZDO.SetPosition(state.InitialPosition with { y = -1000 });
          state.Container.ZDO.Fields<Container>().Set(static () => x => x.m_autoDestroyEmpty, true);
          state.Container.ZDO.CreateClone(false);
          DestroyObject(state.Container.ZDO); // release exclusive claim
        }
        _containers.RemoveAt(i);
        return;
      }
    }
  }

  sealed class State(Peer peer, ContainerState container, PlayerState player, ServersideQoLZDO portal)
  {
    public Peer Peer { get; } = peer;
    public ContainerState Container { get; set; } = container;
    public PlayerState Player { get; } = player;
    public Vector3 InitialPosition { get; } = player.ZDO.ZDO.GetPosition();
    public Vector3 PortalPosition { get; } = portal.ZDO.GetPosition();
    public bool Stacked { get; set; }
    public Timestamp NextRequest { get => field; set { field = value; DestroyAfter = value.AddSeconds(5); } }
    public Timestamp DestroyAfter { get; private set; }
  }
}
