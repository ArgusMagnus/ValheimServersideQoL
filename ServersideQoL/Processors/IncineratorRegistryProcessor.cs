using ServersideQoL.Utilities;

namespace ServersideQoL.Processors;

[Processor("f1d2f83d-02c2-441b-84cc-b57b3ac48b51", OnlyWhenDependedOn = true)]
public sealed class IncineratorRegistryProcessor : Processor<IncineratorRegistryProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Incinerator Incinerator, Container Container, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  readonly Dictionary<ServersideQoLZDO, State> _states = [];
  readonly HashSet<ItemDrop.ItemData> _used = [];
  readonly List<ConversionResult> _results = [];
  ContainerState? _containerState;
  ContainerState.IInventory? _inventory;

  public IReadOnlyCollection<ItemDrop.ItemData> ReservedItems => _used;
  public bool ReserveItem(ItemDrop.ItemData item) => _used.Add(item);
  public void AddConversionResult(ConversionResult result) => _results.Add(result);

  public ItemEnumerable GetAvailableItems(ServersideQoLZDO zdo)
  {
    if (_inventory is null)
    {
      _containerState = Instance<ContainerRegistryProcessor>().GetState(zdo, GetProcessorPrefabInfo(zdo)!.Container);
      _inventory = _containerState.GetInventory();
      if (zdo.Reregister(this))
        ScheduleReprocessing(zdo);
    }
    return new(this, _inventory.Items.GetEnumerator());
  }

  public PlayerState? GetRequestingPlayer(ServersideQoLZDO zdo)
  {
    if (!_states.TryGetValue(zdo, out var state))
    {
      _states.Add(zdo, state = new());
      zdo.Destroyed += RemoveState;
    }
    return state.RequestingPlayer ??= Instance<PlayerRegistryProcessor>().GetStateForPeerID(zdo.ZDO.GetOwner());
  }

  const bool ShowSign = true;

  protected internal override void Initialize()
  {
    RPC.Intercept.UpdateInterception(RPC.RpcName.Incinerator.RequestIncinerate, RPC_RequestIncinerate, true);
    RPC.Intercept.UpdateInterception(RPC.RpcName.Incinerator.AnimateLever, RPC_AnimateLever, true);
    RPC.Intercept.UpdateInterception(RPC.RpcName.Incinerator.AnimateLeverReturn, RPC_AnimateLeverReturn, true);
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    State? state = null;
    if (_used.Count > 0 || _results.Count > 0)
    {
      if (!_states.TryGetValue(zdo, out state))
      {
        _states.Add(zdo, state = new());
        zdo.Destroyed += RemoveState;
      }
      state.Results.Clear();
      foreach (var conversionResult in _results)
        state.Results.Add(conversionResult);
    }
    else if (state is not null || _states.TryGetValue(zdo, out state))
    {
      if (!state.LeverReturned && (!state.LeverPulled || zdo.Vars.GetInUse()))
        state.Results.Clear();
    }
    else if (_containerState is null || _inventory is null)
      return ProcessResult.UnregisterProcessor; /// No processor called <see cref="GetAvailableItems"/>

    var result = ProcessResult.Default;
    if (_used.Count is not 0)
    {
      if (state.Sign is null)
      {
        var pos = zdo.ZDO.GetPosition();
        if (ShowSign)
          pos.y += 1.25f;
        state.Sign = PlacePiece(pos, Prefabs.Sign, zdo.ZDO.GetRotation());
      }
      var text = string.Join("<br>", state.Results.Select(static x => x.GetText()));
      state.Sign.Vars.SetText(ShowSign ? $"<color=white>{text}" : $"<mark=#000000><color=white>{text}<size=45><br><br>");
    }
    else if (state is { LeverReturned: true })
    {
      _containerState = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo.Container);
      _inventory = _containerState.GetInventory();
      if (!zdo.IsOwnerOrUnassigned())
        result = ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(_containerState, default));
      else
      {
        var save = false;
        foreach (var conversionResult in state.Results)
        {
          if (conversionResult.GetItem() is not { } item)
            continue;
          _inventory.Inventory.AddItem(item);
          save = true;
        }
        if (save)
          _inventory.Save();
        RemoveState(zdo);
      }
    }
    else if (state?.Results.Count is 0)
    {
      RemoveState(zdo);
    }

    _used.Clear();
    _results.Clear();
    _containerState = null;
    _inventory = null;
    return result;
  }

  void RemoveState(ServersideQoLZDO zdo)
  {
    zdo.Destroyed -= RemoveState;
    if (_states.Remove(zdo, out var state) && state.Sign is not null)
      DestroyObject(state.Sign);
  }

  void RPC_RequestIncinerate(ServersideQoLZDO zdo, ZRoutedRpc.RoutedRPCData data, PlayerID playerID)
  {
    var playerState = Instance<PlayerRegistryProcessor>().GetStateForPeerID(data.m_senderPeerID)
      ?? Instance<PlayerRegistryProcessor>().GetStateForPlayerID(playerID);

    if (playerState is null)
      Logger.LogWarning($"Player state for peerID={data.m_senderPeerID} or playerID={playerID.Value} not found");
    else if (_states.TryGetValue(zdo, out var state))
      state.RequestingPlayer = playerState;
    else
    {
      _states.Add(zdo, new() { RequestingPlayer = playerState });
      zdo.Destroyed += RemoveState;
    }
  }

  void RPC_AnimateLever(ServersideQoLZDO zdo)
  {
    if (_states.TryGetValue(zdo, out var state))
      state.LeverPulled = true;
  }

  void RPC_AnimateLeverReturn(ServersideQoLZDO zdo)
  {
    if (_states.TryGetValue(zdo, out var state))
      state.LeverReturned = true;
  }

  sealed class State
  {
    public PlayerState? RequestingPlayer { get; set; }
    public List<ConversionResult> Results { get; } = [];
    public bool LeverPulled { get; set; }
    public bool LeverReturned { get; set; }
    public ServersideQoLZDO? Sign { get; set; }
  }

  public readonly record struct DynamicResult(Func<string> GetText, Func<ItemDrop.ItemData?> GetResult);
  public readonly union ConversionResult(ItemDrop.ItemData, DynamicResult)
  {
    public string GetText() => this switch
    {
      ItemDrop.ItemData item => $"{Localization.instance.Localize(item.m_shared.m_name)}{(item.m_quality > 1 ? $" [{item.m_quality}]" : "")}{(item.m_stack > 1 ? $" x{item.m_stack}" : "")}",
      DynamicResult x => x.GetText()
    };

    public ItemDrop.ItemData? GetItem() => this switch
    {
      ItemDrop.ItemData item => item,
      DynamicResult x => x.GetResult()
    };
  }

  public ref struct ItemEnumerator(IncineratorRegistryProcessor processor, List<ItemDrop.ItemData>.Enumerator enumerator)
  {
    readonly IncineratorRegistryProcessor _processor = processor;
    List<ItemDrop.ItemData>.Enumerator _enumerator = enumerator;
    public ItemDrop.ItemData Current => _enumerator.Current;

    public bool MoveNext()
    {
      ref var enumerator = ref _enumerator;
      while (enumerator.MoveNext())
      {
        if (!_processor._used.Contains(enumerator.Current))
          return true;
      }
      return false;
    }

    public void Dispose() => _enumerator.Dispose();
  }

  public readonly ref struct ItemEnumerable(IncineratorRegistryProcessor processor, List<ItemDrop.ItemData>.Enumerator enumerator)
  {
    public ItemEnumerator GetEnumerator() => new(processor, enumerator);
  }
}
