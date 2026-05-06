using System;
using System.Collections.Generic;
using System.Text;

namespace Valheim.ServersideQoL;

partial class ExtendedZDO
{
    sealed class ZDOInventory(ExtendedZDO zdo) : IZDOInventory, IZDOInventoryReadOnly
    {
        public Inventory Inventory { get; private set; } = default!;
        public ExtendedZDO ZDO { get; private set; } = zdo;
        public int? PickupRange { get; set; }
        public int? FeedRange { get; set; }

        List<ItemDrop.ItemData>? _items;
        uint _dataRevision = uint.MaxValue;

        List<ItemDrop.ItemData> Items
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

        IList<ItemDrop.ItemData> IZDOInventory.Items => Items;
        IReadOnlyList<ItemDrop.ItemData> IZDOInventoryReadOnly.Items => Items;

        public ZDOInventory Update()
        {
            if (_dataRevision == ZDO.DataRevision)
                return this;

            var data = ZDO.Vars.GetItems();

            var fields = ZDO.Fields<Container>();
            var w = fields.GetInt(static () => x => x.m_width);
            var h = fields.GetInt(static () => x => x.m_height);
            if (Inventory is null || Inventory.GetWidth() != w || Inventory.GetHeight() != h)
            {
                Inventory = new(ZDO.PrefabInfo.Container!.Value.Container.m_name, ZDO.PrefabInfo.Container!.Value.Container.m_bkg, w, h);
                _items = null;
            }

            if (data is { Length: > 0 })
                Inventory.Load(new(data));
            else
                Items.Clear();

            _dataRevision = ZDO.DataRevision;
            return this;
        }

        public void UpdateZDO(ExtendedZDO zdo)
        {
            ZDO = zdo;
            _items = default;
            _dataRevision = default;
            Update();
        }

        public void Save()
        {
            var pkg = new ZPackage();
            Inventory.Save(pkg);
            var dataRevision = ZDO.DataRevision;
            var data = pkg.GetArray();
            ZDO.Vars.SetItems(data);
            if (dataRevision != ZDO.DataRevision) // items changed
            {
                // moving ZDO are constantly updated, so we need to get ahead for our changes to stick.
                // Not sure about the increment value though...
                if (ZDO.PrefabInfo.Container is { ZSyncTransform.Value: not null })
                    ZDO.DataRevision += 120;

                ZDOMan.instance.ForceSendZDO(ZDO.m_uid);
            }

            _dataRevision = ZDO.DataRevision;
        }
    }
}
