using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace ServersideQoL.Backpack;

[Processor(Id)]
[DependsOn<PlayerRegistryProcessor>]
[DependsOn<ContainerRegistryProcessor>]
sealed class BackpackProcessor : Processor<BackpackProcessor.PrefabInfo>
{
  public const string Id = "fdd88a40-add7-413c-ac4b-894ac26c500d";

  static int BackpackPrefabHash => Prefabs.PrivateChest;
  static int BackpackTombstonePrefab => Prefabs.TombStone;

  public sealed record PrefabInfo(Container? Container, Player? Player) : ProcessorPrefabInfo
  {
    [MemberNotNullWhen(true, nameof(Container))]
    public bool IsBackpack { get; private set; }

    [MemberNotNullWhen(true, nameof(Container))]
    public bool IsBackpackTombstone { get; private set; }

    public override bool IsValid => Player is not null || (IsBackpack = PrefabInfo.PrefabHash == BackpackPrefabHash) || (IsBackpackTombstone = PrefabInfo.PrefabHash == BackpackTombstonePrefab);
  }

  readonly Dictionary<ServersideQoLZDO, State> _backpacks = [];
  readonly Dictionary<ServersideQoLZDO, State> _backpacksByPlayer = [];
  int _backpackSlots;

  static readonly ServerVar<bool> __isBackpackTombstone = BackpackPlugin.RegisterServerVar<bool>("IsBackbackTombstone");

  protected override void Initialize()
  {
    _backpacks.Clear();
    _backpacksByPlayer.Clear();

    Instance<PlayerRegistryProcessor>().EmoteDetected -= OnEmoteDetected;
    Instance<PlayerRegistryProcessor>().EmoteDetected += OnEmoteDetected;
    RPC.Intercept.UpdateInterception(RPC.RpcName.Player.OnDeath, RPC_OnDeath,
      Config.Instance.BackpackOnDeath.Value is not Config.BackPackOnDeathOptions.Keep);

    ServersideQoLPlugin.Instance.GlobalKeysChanged -= UpdateBackpackSlots;
    if (Config.Instance.OpenBackpackEmote.Value is ConfigBase.DisabledEmote)
      _backpackSlots = 0;
    else
    {
      UpdateBackpackSlots();
      if (Config.Instance.AdditionalBackpackSlotsPerDefeatedBoss.Value is not 0)
        ServersideQoLPlugin.Instance.GlobalKeysChanged += UpdateBackpackSlots;
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    ProcessResult result;

    if (prefabInfo.IsBackpack)
    {
      if (!_backpacks.TryGetValue(zdo, out var state))
        result = PlacedObjects.Contains(zdo) ? default : ProcessResult.UnregisterProcessor;
      else
      {
        result = default;
        var hasNonTeleportableItems = false;
        var weightLimitExceeded = false;
        var totalWeight = 0f;
        var containerState = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo.Container);
        var inventory = containerState.GetInventory();
        var dropPos = state.PlayerState.ZDO.ZDO.GetPosition();
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
            if (Config.Instance.MaxBackpackWeight.Value > 0 && totalWeight > Config.Instance.MaxBackpackWeight.Value)
            {
              weightLimitExceeded = true;
              drop = true;
            }
          }

          if (drop)
          {
            ItemDrop.DropItem(item, 0, dropPos, state.PlayerState.ZDO.ZDO.GetRotation());
            inventory.Items.RemoveAt(i);
          }
        }

        if (hasNonTeleportableItems || weightLimitExceeded)
        {
          var owner = zdo.ZDO.GetOwner();
          zdo.ClaimOwnershipInternal();
          inventory.Save();
          zdo.ZDO.SetOwnerInternal(owner);
          state.BackpackContainer = RecreatePiece(zdo);
          RPC.ShowMessage(owner, MessageHud.MessageType.Center, hasNonTeleportableItems ?
              Config.Instance.Localization.Value.ForbiddenItems :
              Config.Instance.Localization.Value.FormatWeightLimitExceeded(Config.Instance.MaxBackpackWeight.Value));
          state.OpenBackpackAfter = Timestamp.Now.AddSeconds(Config.Instance.Advanced.Value.OpenBackpackDelay);
        }
      }
    }
    else if (prefabInfo.IsBackpackTombstone)
    {
      if (!__isBackpackTombstone.Get(zdo))
        result = ProcessResult.UnregisterProcessor;
      else
      {
        if (Instance<PlayerRegistryProcessor>().GetStateForPlayerID(zdo.Vars.GetCreator()) is { } playerState
          && !playerState.ZDO.Vars.GetIsDead()
          && Vector3.Distance(playerState.ZDO.ZDO.GetPosition(), zdo.ZDO.GetPosition()) < Config.Instance.Advanced.Value.BackpackOnDeathDropTombStone.AutoCollectDistance
          && _backpacksByPlayer.TryGetValue(playerState.ZDO, out var state))
        {
          state.EnsureBackpackExists();
          var inventory = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo.Container).GetInventory();
          var backpackInventory = Instance<ContainerRegistryProcessor>().GetState(state.BackpackContainer, state.Container).GetInventory();
          foreach (var item in inventory.Items)
          {
            if (!backpackInventory.Inventory.AddItem(item))
              DropBackpackItem(item, zdo, playerState.Owner);
          }
          state.BackpackContainer.ClaimOwnershipInternal();
          backpackInventory.Save();
          RPC.ShowMessage(playerState.Owner, MessageHud.MessageType.Center, $"$piece_tombstone_recovered ({Config.Instance.Localization.Value.BackpackName})");
          zdo.Destroy();
        }
        result = ScheduleReprocessing(0.1f);
      }
    }
    else if (prefabInfo.Player is not null)
    {
      if (_backpacksByPlayer.TryGetValue(zdo, out var state) && state.BackpackContainer is not null)
      {
        if (state.OpenBackpackAfter < Timestamp.Now)
        {
          state.OpenBackpackAfter = null;
          RPC.OpenResponse(state.BackpackContainer, true);
        }
        else if (state.BackpackContainer.ZDO.GetPosition() is { y: > -1000 } &&
            Vector3.Distance(zdo.ZDO.GetPosition(), state.BackpackContainer.ZDO.GetPosition()) > InventoryGui.instance.m_autoCloseDistance)
        {
          state.BackpackContainer.ZDO.SetPosition(state.BackpackContainer.ZDO.GetPosition() with { y = -1000 });
          state.BackpackContainer = RecreatePiece(state.BackpackContainer);
        }
      }
      result = ScheduleReprocessing(0.1f);
    }
    else
    {
      Logger.DevLog($"Unexpected ZDO: {prefabInfo.PrefabInfo.PrefabName}");
      result = ProcessResult.UnregisterProcessor;
    }

    return result;
  }

  void UpdateBackpackSlots()
  {
    _backpackSlots = Config.Instance.InitialBackpackSlots.Value;
    if (Config.Instance.AdditionalBackpackSlotsPerDefeatedBoss.Value is 0)
      return;

    _backpackSlots += Config.Instance.AdditionalBackpackSlotsPerDefeatedBoss.Value * BossesByBiome.Values
      .Count(static x => ZoneSystem.instance.GetGlobalKey(x.m_defeatSetGlobalKey));
  }

  State GetState(PlayerState playerState)
  {
    if (!_backpacksByPlayer.TryGetValue(playerState.ZDO, out var state))
    {
      _backpacksByPlayer.Add(playerState.ZDO, state = new(this, playerState));
      playerState.ZDO.Destroyed += zdo =>
      {
        if (_backpacksByPlayer.Remove(zdo, out var state))
          state.BackpackContainer = null;
      };
    }
    return state;
  }

  void OnEmoteDetected(PlayerState playerState, Emotes emote)
  {
    if (Config.Instance.OpenBackpackEmote.Value is not ConfigBase.AnyEmote && Config.Instance.OpenBackpackEmote.Value != emote)
      return;

    var state = GetState(playerState);

    if (!state.EnsureBackpackExists())
      state.OpenBackpackAfter = Timestamp.Now.AddSeconds(Config.Instance.Advanced.Value.OpenBackpackDelay);
    else
    {
      state.OpenBackpackAfter = null;
      RPC.OpenResponse(state.BackpackContainer, true);
    }
  }

  void DestroyBackpack(long peerID)
  {
    if (Instance<PlayerRegistryProcessor>().GetStateForPeerID(peerID) is not { } playerState
      || GetState(playerState).BackpackContainer is not { } backpack)
      return;

    DestroyObject(backpack);
    Logger.LogInfo($"Backpack of player '{playerState.PlayerName}' destroyed on death");
  }

  void DropBackpackItem(ItemDrop.ItemData item, ServersideQoLZDO refPosZdo, long peerID)
  {
    var cfg = Config.Instance.Advanced.Value.BackpackOnDeathDropItems;
    var pos = refPosZdo.ZDO.GetPosition();
    var scatter = UnityEngine.Random.insideUnitCircle * cfg.ScatterRadius;
    pos.x += scatter.x;
    pos.y += cfg.VerticalOffset;
    pos.z += scatter.y;
    var zdo = ItemDrop.DropItem(item, 0, pos, refPosZdo.ZDO.GetRotation()).GetComponent<ZNetView>().GetZDO().ServersideQoLZDO;
    zdo.Fields<ItemDrop>()
        .Set(static () => x => x.m_autoDestroy, !cfg.PreventAutoDestroy)
        .Set(static () => x => x.m_autoPickup, !cfg.PreventAutoPickup);
    zdo.ZDO.SetOwnerInternal(peerID);
  }

  void DropBackpackItems(long peerID)
  {
    if (Instance<PlayerRegistryProcessor>().GetStateForPeerID(peerID) is not { } playerState
      || GetState(playerState) is not { BackpackContainer: { } backpack, Container: { } container })
      return;

    var inventory = Instance<ContainerRegistryProcessor>().GetState(backpack, container).GetInventory();
    foreach (var item in inventory.Items)
      DropBackpackItem(item, playerState.ZDO, peerID);
    DestroyObject(backpack);
    Logger.LogInfo($"Backpack items of player '{playerState.PlayerName}' dropped at death location.");
  }

  void DropBackback(long peerID)
  {
    if (Instance<PlayerRegistryProcessor>().GetStateForPeerID(peerID) is not { } playerState
      || GetState(playerState) is not { BackpackContainer: { } backpack, Container: { } container })
      return;

    var backpackInventory = Instance<ContainerRegistryProcessor>().GetState(backpack, container).GetInventory();
    if (backpackInventory.Items.Count is 0)
      return;

    var pos = playerState.ZDO.ZDO.GetPosition();
    pos.y += Config.Instance.Advanced.Value.BackpackOnDeathDropTombStone.VerticalOffset;
    var zdo = Spawn(BackpackTombstonePrefab, pos, playerState.ZDO.ZDO.GetRotation(), owner: peerID);
    __isBackpackTombstone.Set(zdo, true);
    /// <see cref="TombStone.Setup"/>
    zdo.Vars.SetOwner(playerState.PlayerID);
    zdo.Vars.SetOwnerName($"{playerState.PlayerName} - {Config.Instance.Localization.Value.BackpackName}");

    zdo.Fields<Container>()
        .Set(static () => x => x.m_width, backpackInventory.Inventory.GetWidth())
        .Set(static () => x => x.m_height, backpackInventory.Inventory.GetHeight());

    var inventory = Instance<ContainerRegistryProcessor>().GetState(zdo)!.GetInventory();
    foreach (var item in backpackInventory.Items)
      inventory.Items.Add(item);
    inventory.Save();

    DestroyObject(backpack);
    Logger.LogInfo($"Backpack of player '{playerState.PlayerName}' dropped at death location.");
  }

  void RPC_OnDeath(ZRoutedRpc.RoutedRPCData data)
  {
    switch (Config.Instance.BackpackOnDeath.Value)
    {
      case Config.BackPackOnDeathOptions.SameAsInventory:
        if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteItems) || ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteUnequipped))
          DestroyBackpack(data.m_senderPeerID);
        else
          DropBackback(data.m_senderPeerID);
        break;

      case Config.BackPackOnDeathOptions.Destroy:
        DestroyBackpack(data.m_senderPeerID);
        break;

      case Config.BackPackOnDeathOptions.DropTombStone:
        DropBackback(data.m_senderPeerID);
        break;

      case Config.BackPackOnDeathOptions.DropItems:
        DropBackpackItems(data.m_senderPeerID);
        break;
    }
  }

  sealed class State(BackpackProcessor processor, PlayerState playerState)
  {
    readonly BackpackProcessor _processor = processor;
    public PlayerState PlayerState { get; } = playerState;
    public Container? Container { get; private set; }

    ServersideQoLZDO? _backpackContainer;
    public ServersideQoLZDO? BackpackContainer
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

    void OnBackpackDestroyed(ServersideQoLZDO backpack)
    {
      _processor._backpacks.Remove(backpack);
      if (ReferenceEquals(backpack, _backpackContainer))
        _backpackContainer = null;
    }

    [MemberNotNull(nameof(BackpackContainer), nameof(Container))]
    public bool EnsureBackpackExists()
    {
      var backpackPrefab = Prefabs.PrivateChest;

      var pos = PlayerState.ZDO.ZDO.GetPosition();
      pos.y -= 0.6f;

      static bool AdjustSize(ServersideQoLZDO zdo, int slots)
      {
        var fields = zdo.Fields<Container>();
        var state = Instance<ContainerRegistryProcessor>().GetState(zdo)!;
        var inventory = state.GetInventory();
        var actualSlots = Math.Max(slots, inventory.Items.Count);
        var (width, height) = GetInventorySize(actualSlots, true);
        if ((fields.UpdateValue(static () => x => x.m_width, width),
            fields.UpdateValue(static () => x => x.m_height, height)) == (false, false))
          return false;

        using var enumerator = inventory.Items.GetEnumerator();
        for (int y = 0; y < height; y++)
        {
          for (int x = 0; x < width; x++)
          {
            if (!enumerator.MoveNext())
            {
              zdo.ClaimOwnershipInternal();
              inventory.Save();
              return true;
            }

            enumerator.Current.m_gridPos = new(x, y);
          }
        }
        return true;
      }

      if (BackpackContainer is null)
      {
        BackpackContainer = _processor.PlacedObjects.FirstOrDefault(x => GetProcessorPrefabInfo(x) is { Container: not null } && x.IsModCreator(out var marker) && marker is CreatorMarkers.ProcessorOwned && x.Vars.GetPlayerID() == PlayerState.PlayerID);
        BackpackContainer?.Fields<Container>().Set(static () => x => x.m_name, Config.Instance.Localization.Value.BackpackName);
      }
#if DEBUG
      else if (BackpackContainer.ZDO.GetPrefab() != backpackPrefab)
      {
        _processor.DestroyObject(BackpackContainer);
        BackpackContainer = null;
      }
#endif

      if (BackpackContainer is null)
      {
        BackpackContainer = _processor.PlacePiece(pos, backpackPrefab, 0, CreatorMarkers.ProcessorOwned);
        BackpackContainer.Vars.SetPlayerID(PlayerState.PlayerID);
        BackpackContainer.Fields<Container>().Set(static () => x => x.m_name, Config.Instance.Localization.Value.BackpackName);
        AdjustSize(BackpackContainer, _processor._backpackSlots);
        BackpackContainer.ZDO.SetOwnerInternal(PlayerState.Owner);
      }
      else if (Vector3.Distance(PlayerState.ZDO.ZDO.GetPosition(), BackpackContainer.ZDO.GetPosition()) > InventoryGui.instance.m_autoCloseDistance
          || AdjustSize(BackpackContainer, _processor._backpackSlots))
      {
        BackpackContainer.ZDO.SetPosition(pos);
        BackpackContainer.ZDO.SetOwnerInternal(PlayerState.Owner);
        BackpackContainer = _processor.RecreatePiece(BackpackContainer);
      }
      else
      {
        Container = GetPrefabInfo(BackpackContainer).GetRequiredComponent<Container>();
        return true;
      }
      Container = GetPrefabInfo(BackpackContainer).GetRequiredComponent<Container>();
      return false;
    }

    public Timestamp? OpenBackpackAfter { get; set; }
  }
}
