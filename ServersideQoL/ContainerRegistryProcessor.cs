using Valheim.ZDOExtender;

namespace ServersideQoL;

[Processor(Id, OnlyWhenDependedOn = true)]
public sealed class ContainerRegistryProcessor : Processor<ContainerRegistryProcessor.PrefabInfo>
{
    public const string Id = "fe73690f-6790-4cfa-9795-f93136d57286";
    public sealed record PrefabInfo(Container Container, Piece Piece, PieceTable PieceTable, ZSyncTransform? ZSyncTransform) : ProcessorPrefabInfo;

    readonly Dictionary<ZDO, ContainerInventory> _inventories = [];
    readonly Dictionary<float, SectorDictionary<SharedItemDataKey, HashSet<ZDO>>> _containersByItemNameBySectorWidth = [];

    public SectorDictionary<SharedItemDataKey, HashSet<ZDO>> GetContainersByItemName(float sectorWidth)
    {
        if (!_containersByItemNameBySectorWidth.TryGetValue(sectorWidth, out var dict))
            _containersByItemNameBySectorWidth.Add(sectorWidth, dict = new(sectorWidth));
        return dict;
    }

    protected internal override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        _containersByItemNameBySectorWidth.Clear();
    }

    public IContainerInventory? GetInventory(ZDO zdo)
    {
        if (GetPrefabInfo(zdo) is not { } prefabInfo)
            return default;
        return GetInventory(zdo, prefabInfo);
    }

    IContainerInventory GetInventory(ZDO zdo, PrefabInfo prefabInfo)
    {
        if (!_inventories.TryGetValue(zdo, out var inventory))
        {
            _inventories.Add(zdo, inventory = new(zdo, prefabInfo));
            zdo.GetExtension<IExtendedZDO>().Destroyed += x => _inventories.Remove(x);
        }

        return inventory.Update();
    }

    protected override ProcessResult Process(ZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
    {
        if (zdo.Vars.GetOwner() is 0)
            return ProcessResult.UnregisterProcessor;

        if (_containersByItemNameBySectorWidth.Count is 0)
            return ProcessResult.UnregisterProcessor;

        if (zdo.Vars.GetInUse())
            return default;

        var inventory = GetInventory(zdo, prefabInfo);

        foreach (var item in inventory.Items)
        {
            if (prefabInfo.Container.m_privacy is Container.PrivacySetting.Private)
                continue;

            SharedItemDataKey key = item.m_shared;
            foreach (var dict in _containersByItemNameBySectorWidth.Values)
                dict.TryAdd(key, zdo);
        }
        return default;
    }

    sealed class ContainerInventory(ZDO zdo, PrefabInfo prefabInfo) : IContainerInventory, IContainerInventoryReadOnly
    {
        public Inventory Inventory { get; private set; } = default!;
        readonly ZDO _zdo = zdo;
        readonly PrefabInfo _prefabInfo = prefabInfo;

        List<ItemDrop.ItemData>? _items;
        uint _dataRevision = uint.MaxValue;
        byte[]? _data;

        public List<ItemDrop.ItemData> Items
        {
            get
            {
                if (_items is null)
                    _items = Inventory!.GetAllItems();
                else if (!ReferenceEquals(_items, Inventory!.GetAllItems()))
                    throw new Exception("Assumption violated");
                return _items;
            }
        }

        public float TotalWeight => Inventory.GetTotalWeight();

        IReadOnlyList<ItemDrop.ItemData> IContainerInventoryReadOnly.Items => Items;

        public ContainerInventory Update()
        {
            if (_dataRevision == _zdo.DataRevision)
                return this;

            var data = _zdo.Vars.GetItems();
            if (ReferenceEquals(data, _data)) // review: maybe also check SequenceEquals?
                return this;

            var fields = _zdo.Fields<Container>();
            var w = fields.GetInt(static () => x => x.m_width);
            var h = fields.GetInt(static () => x => x.m_height);
            if (Inventory is null || Inventory.GetWidth() != w || Inventory.GetHeight() != h)
            {
                Inventory = new(_prefabInfo.Container.m_name, _prefabInfo.Container.m_bkg, w, h);
                _items = null;
            }

            if (data is { Length: > 0 })
                Inventory.Load(new(data));
            else
                Items.Clear();

            _dataRevision = _zdo.DataRevision;
            _data = data;
            return this;
        }

        public void Save()
        {
            var pkg = new ZPackage();
            Inventory.Save(pkg);
            var dataRevision = _zdo.DataRevision;
            var data = pkg.GetArray();
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
    }
}

public interface IContainerInventoryReadOnly
{
    IReadOnlyList<ItemDrop.ItemData> Items { get; }
    float TotalWeight { get; }
}

public interface IContainerInventory
{
    Inventory Inventory { get; }
    List<ItemDrop.ItemData> Items { get; }
    float TotalWeight { get; }
    void Save();
}