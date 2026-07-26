using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ServersideQoL;

[Processor("fe73690f-6790-4cfa-9795-f93136d57286", OnlyWhenDependedOn = true)]
public sealed class ContainerRegistryProcessor : Processor<ContainerRegistryProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Container Container, Piece Piece, PieceTable PieceTable, ZSyncTransform? ZSyncTransform) : ProcessorPrefabInfo;

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
    if (GetPrefabInfo(zdo) is not { } prefabInfo)
      return default;
    return GetState(zdo, prefabInfo);
  }

  public ContainerState GetState(ServersideQoLZDO zdo, PrefabInfo prefabInfo)
    => GetStateCore(zdo, prefabInfo);

  ContainerStateImpl GetStateCore(ServersideQoLZDO zdo, PrefabInfo prefabInfo)
  {
    if (!_states.TryGetValue(zdo, out var state))
    {
      _states.Add(zdo, state = new(zdo, prefabInfo));
      zdo.Destroyed += x => _states.Remove(x);
    }

    return state;
  }

  public void RequestOwnership(ServersideQoLZDO zdo, long playerID, [CallerFilePath] string caller = default!, [CallerLineNumber] int callerLineNo = default)
      => RequestOwnership(zdo, playerID, _states[zdo], caller, callerLineNo);

  public void RequestOwnership(ServersideQoLZDO zdo, long playerID, ContainerState state, [CallerFilePath] string caller = default!, [CallerLineNumber] int callerLineNo = default)
  {
    if (zdo.IsOwnerOrUnassigned() || state is not ContainerStateImpl s || DateTimeOffset.UtcNow < s.NextOwnershipRequest)
      return;

    if (!_openResponseRegistered && Player.m_localPlayer is not null)
    {
      /// <see cref="Container.RPC_OpenRespons"/>
      RPC.Intercept.UpdateInterception("OpenRespons", RPC_OpenResponse, _openResponseRegistered = true);
    }

    //Logger.DevLog($"Container {zdo.m_uid}: RequestOwnership");
    s.NextOwnershipRequest = DateTimeOffset.UtcNow.AddSeconds(1);
    s.WaitingForResponse = true;
    s.PreviousOwner = zdo.ZDO.GetOwner();


    //DevShowMessage(zdo, "Requesting ownership", DamageText.TextType.Normal, caller, callerLineNo);
    RPC.RequestOpen(zdo, playerID);
  }

  protected internal override void Initialize()
  {
    _states.Clear();
    _containersByItemName.Clear();
    RPC.Intercept.UpdateInterception("OpenRespons", RPC_OpenResponse, _openResponseRegistered = false);
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (prefabInfo.Container.m_privacy is Container.PrivacySetting.Private || zdo.Vars.GetCreator() is 0)
      return ProcessResult.UnregisterProcessor;

    if (_containersByItemName.Count is 0)
      return default; // ProcessResult.UnregisterProcessor;

    if (zdo.Vars.GetInUse())
      return default;

    List<float>? remove = null;
    var state = GetStateCore(zdo, prefabInfo);
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

  sealed class ContainerStateImpl(ServersideQoLZDO zdo, PrefabInfo prefabInfo) : ContainerState, ContainerState.IInventory
  {
    public DateTimeOffset NextOwnershipRequest { get; set; }
    public bool WaitingForResponse { get; set; }
    public long PreviousOwner { get; set; }
    public bool AddedToContainers { get; set; }

    Inventory? _inventory;
    readonly ServersideQoLZDO _zdo = zdo;
    readonly PrefabInfo _prefabInfo = prefabInfo;

    List<ItemDrop.ItemData>? _items;
    uint _dataRevision = uint.MaxValue;
    byte[]? _data;
    static readonly ZPackage _pkg = new();

    public override PrefabInfo PrefabInfo => _prefabInfo;

    [MemberNotNull(nameof(_inventory))]
    public override IInventory GetInventory()
    {
      byte[]? data = default;
      if (_inventory is not null)
      {
        if (_dataRevision == _zdo.ZDO.DataRevision)
          return this;

        data = _zdo.Vars.GetItems();
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
        _inventory = new(_prefabInfo.Container.m_name, _prefabInfo.Container.m_bkg, w, h);
        _items = null;
      }

      if (data is not { Length: > 0 })
        ((IInventory)this).Items.Clear();
      else
      {
        _pkg.Load(data);
        _inventory.Load(_pkg);
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
      _pkg.Clear();
      _inventory!.Save(_pkg);
      var dataRevision = _zdo.ZDO.DataRevision;
      var data = _pkg.GetArray();
      _zdo.Vars.SetItems(data);
      if (dataRevision != _zdo.ZDO.DataRevision) // items changed
      {
        // moving ZDO are constantly updated, so we need to get ahead for our changes to stick.
        // Not sure about the increment value though...
        if (_prefabInfo.ZSyncTransform is not null)
          _zdo.ZDO.DataRevision += 120;

        ZDOMan.instance.ForceSendZDO(_zdo.ZDO.m_uid);
      }

      _dataRevision = _zdo.ZDO.DataRevision;
      _data = data;
    }
  }
}
