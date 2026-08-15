using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ServersideQoL.Processors;

[Processor("fe73690f-6790-4cfa-9795-f93136d57286", OnlyWhenDependedOn = true)]
public sealed class ContainerRegistryProcessor : Processor<ContainerRegistryProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Container Container, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  public event Action<ServersideQoLZDO, ContainerState>? ContainerChanged;

  readonly Dictionary<ServersideQoLZDO, ContainerStateImpl> _states = [];
  readonly Dictionary<float, WeakReference<SectorDictionary<SharedItemDataKey, HashSet<ServersideQoLZDO>>>> _containersByItemName = [];
  readonly Dictionary<float, WeakReference<SectorDictionary<HashSet<ServersideQoLZDO>>>> _containers = [];
  bool _openResponseRegistered;

  public SectorDictionary<SharedItemDataKey, HashSet<ServersideQoLZDO>> GetContainersByItemName(float sectorWidth)
  {
    SectorDictionary<SharedItemDataKey, HashSet<ServersideQoLZDO>> dict;
    if (!_containersByItemName.TryGetValue(sectorWidth, out var weakRef))
      _containersByItemName.Add(sectorWidth, new(dict = new(sectorWidth)));
    else if (!weakRef.TryGetTarget(out dict))
      weakRef.SetTarget(dict = new(sectorWidth));
    return dict;
  }

  public SectorDictionary<HashSet<ServersideQoLZDO>> GetContainers(float sectorWidth)
  {
    SectorDictionary<HashSet<ServersideQoLZDO>> dict;
    if (!_containers.TryGetValue(sectorWidth, out var weakRef))
      _containers.Add(sectorWidth, new(dict = new(sectorWidth)));
    else if (!weakRef.TryGetTarget(out dict))
      weakRef.SetTarget(dict = new(sectorWidth));
    return dict;
  }

  public ContainerState? GetState(ServersideQoLZDO zdo)
  {
    if (zdo.PrefabInfo?.GetComponent<Container>() is not { } container)
      return null;
    return GetStateCore(zdo, container);
  }

  public ContainerState GetState(ServersideQoLZDO zdo, Container container)
  {
    System.Diagnostics.Debug.Assert(zdo.PrefabInfo?.GetComponent<Container>() == container);
    return GetStateCore(zdo, container);
  }

  static readonly ServerVar<bool> __returnContentToCreatorVar = ServersideQoLPlugin.RegisterServerVar<bool>("ReturnContentToCreator");
  public bool GetReturnContentToCreator(ServersideQoLZDO zdo, bool defaultValue = default) => __returnContentToCreatorVar.Get(zdo, defaultValue);

  public void SetReturnContentToCreator(ServersideQoLZDO zdo, bool value)
  {
    AssertHasProcessorPrefabInfo(zdo);
    __returnContentToCreatorVar.Set(zdo, value);
  }

  ContainerStateImpl GetStateCore(ServersideQoLZDO zdo, Container container)
  {
    if (!_states.TryGetValue(zdo, out var state))
    {
      _states.Add(zdo, state = new(zdo, container));
      zdo.Destroyed += x => _states.Remove(x);
    }

    return state;
  }

  public float RequestOwnership(ServersideQoLZDO zdo, PlayerID playerID, [CallerFilePath] string caller = default!, [CallerLineNumber] int callerLineNo = default)
      => RequestOwnership(zdo, playerID, _states[zdo], caller, callerLineNo);

  public float RequestOwnership(ServersideQoLZDO zdo, PlayerID playerID, ContainerState state, [CallerFilePath] string caller = default!, [CallerLineNumber] int callerLineNo = default)
  {
    if (zdo.IsOwnerOrUnassigned() || state is not ContainerStateImpl s || DateTimeOffset.UtcNow < s.NextOwnershipRequest)
      return Config.Instance.Advanced.Value.ProcessingDelays.AfterContainerOwnershipRequest;

    if (!_openResponseRegistered && Player.m_localPlayer is not null)
    {
      /// <see cref="Container.RPC_OpenRespons"/>
      RPC.Intercept.UpdateInterception("OpenRespons", RPC_OpenResponse, _openResponseRegistered = true);
    }

    //Logger.DevLog($"Container {zdo.m_uid}: RequestOwnership");
    s.NextOwnershipRequest = DateTimeOffset.UtcNow.AddSeconds(Config.Instance.Advanced.Value.Containers.MinOwnershipRequestInterval);
    s.WaitingForResponse = true;
    s.PreviousOwner = zdo.ZDO.GetOwner();


    //DevShowMessage(zdo, "Requesting ownership", DamageText.TextType.Normal, caller, callerLineNo);
    RPC.RequestOpen(zdo, playerID);
    return Config.Instance.Advanced.Value.ProcessingDelays.AfterContainerOwnershipRequest;
  }

  protected internal override void Initialize()
  {
    _states.Clear();
    RPC.Intercept.UpdateInterception("OpenRespons", RPC_OpenResponse, _openResponseRegistered = false);
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    PlayerID creator = default;
    if (prefabInfo.Container.m_privacy is Container.PrivacySetting.Private || (creator = zdo.Vars.GetCreator()).Value is 0)
      return ProcessResult.UnregisterProcessor;

    ContainerStateImpl? state = null;

    if (GetReturnContentToCreator(zdo))
    {
      if (Instance<PlayerRegistryProcessor>().GetStateForPlayerID(creator) is { } playerState)
      {
        state = GetStateCore(zdo, prefabInfo.Container);
        if (state.GetInventory().Items.Count is 0)
          return ProcessResult.DestroyZDO;
        else if (zdo.ZDO.GetOwner() != playerState.Owner)
          zdo.ZDO.SetOwner(playerState.Owner);
        else
          RPC.TakeAllResponse(zdo, true);
      }
      return ScheduleReprocessing();
    }

    if (zdo.Vars.GetInUse())
      return default;

    state ??= GetStateCore(zdo, prefabInfo.Container);

    List<float>? remove = null;
    if (_containers.Count is not 0)
    {
      if (!state.AddedToContainers)
      {
        state.AddedToContainers = true;
        foreach (var (key, weakRef) in _containers)
        {
          if (!weakRef.TryGetTarget(out var dict))
          {
            (remove ??= []).Add(key);
            continue;
          }
          dict.Add(zdo);
        }

        if (remove is not null)
        {
          foreach (var key in remove)
            _containers.Remove(key);
        }
      }
    }

    if (_containersByItemName.Count is not 0)
    {
      ContainerState.IInventory? inventory = null;
      remove?.Clear();
      foreach (var (key, weakRef) in _containersByItemName)
      {
        if (!weakRef.TryGetTarget(out var dict))
        {
          (remove ??= []).Add(key);
          continue;
        }
        inventory ??= state.GetInventory();
        foreach (var item in inventory.Items)
          dict.TryAdd(item.m_shared, zdo);
      }

      if (remove is not null)
      {
        foreach (var key in remove)
          _containersByItemName.Remove(key);
      }
    }

    ContainerChanged?.Invoke(zdo, state);

    return default;
  }

  bool RPC_OpenResponse(ServersideQoLZDO? zdo, bool granted)
  {
    if (zdo is null || !_states.TryGetValue(zdo, out var state) || !state.WaitingForResponse)
      return true;

    //Logger.DevLog($"Container {data.m_targetZDO}: OpenResponse: {granted}");
    state.WaitingForResponse = false;
    return false;
  }

  sealed class ContainerStateImpl(ServersideQoLZDO zdo, Container container) : ContainerState, ContainerState.IInventory
  {
    public DateTimeOffset NextOwnershipRequest { get; set; }
    public bool WaitingForResponse { get; set; }
    public long PreviousOwner { get; set; }
    public bool AddedToContainers { get; set; }

    Inventory? _inventory;
    readonly ServersideQoLZDO _zdo = zdo;
    readonly Container _container = container;

    List<ItemDrop.ItemData>? _items;
    uint _dataRevision = uint.MaxValue;
    byte[]? _data;

    public override Container Container => _container;
    public override ServersideQoLZDO ZDO => _zdo;

    [MemberNotNull(nameof(_inventory))]
    public override IInventory GetInventory()
    {
      if (_inventory is not null && _dataRevision == _zdo.ZDO.DataRevision)
        return this;

      var data = _zdo.Vars.GetItems();
      if (_inventory is not null)
      {
        if (ReferenceEquals(data, _data))
          return this;
        if (data is not null && _data is not null && data.SequenceEqual(_data))
          return this;
      }

      var fields = _zdo.Fields<Container>();
      var w = fields.GetInt(static () => x => x.m_width);
      var h = fields.GetInt(static () => x => x.m_height);
      if (_inventory is null || _inventory.GetWidth() != w || _inventory.GetHeight() != h)
      {
        _inventory = new(_container.m_name, _container.m_bkg, w, h);
        _items = null;
      }

      if (data is not { Length: > 0 })
        ((IInventory)this).Items.Clear();
      else
      {
        SingletonCache<ZPackage>.Instance.Load(data);
        _inventory.Load(SingletonCache<ZPackage>.Instance);
      }

      _dataRevision = _zdo.ZDO.DataRevision;
      _data = data;
      return this;
    }

    Inventory IInventory.Inventory => _inventory!;

    List <ItemDrop.ItemData> IInventory.Items
    {
      get
      {
        if (_items is null)
          _items = _inventory!.GetAllItems();
        else if (!ReferenceEquals(_items, _inventory!.GetAllItems()))
          throw new Exception("Assumption violated");
        return _items;
      }
    }

    void IInventory.Save()
    {
      SingletonCache<ZPackage>.Instance.Clear();
      _inventory!.Save(SingletonCache<ZPackage>.Instance);
      var dataRevision = _zdo.ZDO.DataRevision;
      var data = SingletonCache<ZPackage>.Instance.GetArray();
      _zdo.Vars.SetItems(data);
      if (dataRevision != _zdo.ZDO.DataRevision) // items changed
      {
        // moving ZDO are constantly updated, so we need to get ahead for our changes to stick.
        // Not sure about the increment value though...
        if (_zdo.PrefabInfo?.HasComponent<ZSyncTransform>() is true)
          _zdo.ZDO.DataRevision += 120;

        ZDOMan.instance.ForceSendZDO(_zdo.ZDO.m_uid);
      }

      _dataRevision = _zdo.ZDO.DataRevision;
      _data = data;
    }
  }
}
