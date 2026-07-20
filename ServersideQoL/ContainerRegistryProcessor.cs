using ServersideQoL.ZDOExtender;
using System.Runtime.CompilerServices;

namespace ServersideQoL;

[Processor(Id, OnlyWhenDependedOn = true)]
public sealed class ContainerRegistryProcessor : Processor<ContainerRegistryProcessor.PrefabInfo>
{
    public const string Id = "fe73690f-6790-4cfa-9795-f93136d57286";
    public sealed record PrefabInfo(Container Container, Piece Piece, PieceTable PieceTable, ZSyncTransform? ZSyncTransform) : ProcessorPrefabInfo;

    public event Action<ZDO, ContainerState>? ContainerChanged;

    readonly Dictionary<ZDO, ContainerStateImpl> _states = [];
    readonly Dictionary<float, WeakReference<SectorDictionary<SharedItemDataKey, HashSet<ZDO>>>> _containersByItemNameBySectorWidth = [];
    bool _openResponseRegistered;

    public SectorDictionary<SharedItemDataKey, HashSet<ZDO>> GetContainersByItemName(float sectorWidth)
    {
        SectorDictionary<SharedItemDataKey, HashSet<ZDO>> dict;
        if (!_containersByItemNameBySectorWidth.TryGetValue(sectorWidth, out var weakRef))
            _containersByItemNameBySectorWidth.Add(sectorWidth, new(dict = new(sectorWidth)));
        else if (!weakRef.TryGetTarget(out dict))
            weakRef.SetTarget(dict = new(sectorWidth));
        return dict;
    }

    public ContainerState? GetState(ZDO zdo)
    {
        if (GetPrefabInfo(zdo) is not { } prefabInfo)
            return default;
        return GetState(zdo, prefabInfo);
    }

    public ContainerState GetState(ZDO zdo, PrefabInfo prefabInfo)
    {
        if (!_states.TryGetValue(zdo, out var inventory))
        {
            _states.Add(zdo, inventory = new(zdo, prefabInfo));
            zdo.GetExtension<IExtendedZDO>().Destroyed += x => _states.Remove(x);
        }

        return inventory.Update();
    }

    public void RequestOwnership(ZDO zdo, long playerID, [CallerFilePath] string caller = default!, [CallerLineNumber] int callerLineNo = default)
        => RequestOwnership(zdo, playerID, _states[zdo], caller, callerLineNo);

    public void RequestOwnership(ZDO zdo, long playerID, ContainerState state, [CallerFilePath] string caller = default!, [CallerLineNumber] int callerLineNo = default)
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
        s.PreviousOwner = zdo.GetOwner();


        //DevShowMessage(zdo, "Requesting ownership", DamageText.TextType.Normal, caller, callerLineNo);
        RPC.RequestOpen(zdo, playerID);
    }

    protected internal override void Initialize()
    {
        RPC.Intercept.UpdateInterception("OpenRespons", RPC_OpenResponse, _openResponseRegistered = false);
    }

    protected override ProcessResult Process(ZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
    {
        if (prefabInfo.Container.m_privacy is Container.PrivacySetting.Private || zdo.Vars.GetOwner() is 0)
            return ProcessResult.UnregisterProcessor;

        if (_containersByItemNameBySectorWidth.Count is 0)
            return default; // ProcessResult.UnregisterProcessor;

        if (zdo.Vars.GetInUse())
            return default;

        var state = GetState(zdo, prefabInfo);

        List<float>? remove = null;
        foreach (var (key, weakRef) in _containersByItemNameBySectorWidth)
        {
            if (!weakRef.TryGetTarget(out var dict))
            {
                (remove ??= []).Add(key);
                continue;
            }
            foreach (var item in state.InventoryItems)
                dict.TryAdd(item.m_shared, zdo);
        }

        if (remove is not null)
        {
            foreach (var key in remove)
                _containersByItemNameBySectorWidth.Remove(key);
        }

        ContainerChanged?.Invoke(zdo, state);

        return default;
    }

    bool RPC_OpenResponse(ZDO? zdo, bool granted)
    {
        if (zdo is null || !_states.TryGetValue(zdo, out var state) || !state.WaitingForResponse)
            return true;

        //Logger.DevLog($"Container {data.m_targetZDO}: OpenResponse: {granted}");
        state.WaitingForResponse = false;
        return false;
    }

    sealed class ContainerStateImpl(ZDO zdo, PrefabInfo prefabInfo) : ContainerState
    {
        public DateTimeOffset NextOwnershipRequest { get; set; }
        public bool WaitingForResponse { get; set; }
        public long PreviousOwner { get; set; }

        Inventory _inventory = default!;
        readonly ZDO _zdo = zdo;
        readonly PrefabInfo _prefabInfo = prefabInfo;

        List<ItemDrop.ItemData>? _items;
        uint _dataRevision = uint.MaxValue;
        byte[]? _data;
        Dictionary<string, float>? _floats;
        static readonly ZPackage _pkg = new();

        public override PrefabInfo PrefabInfo => _prefabInfo;
        public override Inventory Inventory => _inventory;

        public override List<ItemDrop.ItemData> InventoryItems
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

        public ContainerState Update()
        {
            if (_dataRevision == _zdo.DataRevision)
                return this;

            var data = _zdo.Vars.GetItems();
            if (ReferenceEquals(data, _data))
                return this;
            if (data is not null && _data is not null && data.SequenceEqual(_data))
                return this;

            var fields = _zdo.Fields<Container>();
            var w = fields.GetInt(static () => x => x.m_width);
            var h = fields.GetInt(static () => x => x.m_height);
            if (_inventory is null || _inventory.GetWidth() != w || _inventory.GetHeight() != h)
            {
                _inventory = new(_prefabInfo.Container.m_name, _prefabInfo.Container.m_bkg, w, h);
                _items = null;
            }

            if (data is not { Length: > 0 })
                InventoryItems.Clear();
            else
            {
                _pkg.Load(data);
                _inventory.Load(_pkg);
            }

            _dataRevision = _zdo.DataRevision;
            _data = data;
            return this;
        }

        public override void SaveIntenvory()
        {
            _pkg.Clear();
            _inventory.Save(_pkg);
            var dataRevision = _zdo.DataRevision;
            var data = _pkg.GetArray();
            _zdo.Vars.SetItems(data);
            if (dataRevision != _zdo.DataRevision) // items changed
            {
                // moving ZDO are constantly updated, so we need to get ahead for our changes to stick.
                // Not sure about the increment value though...
                if (_prefabInfo.ZSyncTransform is not null)
                    _zdo.DataRevision += 120;

                ZDOMan.instance.ForceSendZDO(_zdo.m_uid);
            }

            _dataRevision = _zdo.DataRevision;
            _data = data;
        }

        public override void SetFloat(string key, float? value)
        {
            if (value.HasValue)
                (_floats ??= [])[key] = value.Value;
            else if (_floats is not null && _floats.Remove(key) && _floats.Count is 0)
                _floats = null;
        }

        public override float? GetFloat(string key)
            => _floats is not null && _floats.TryGetValue(key, out var value) ? value : null;
    }
}
