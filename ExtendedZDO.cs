﻿using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

interface IZDOInventory
{
    Inventory Inventory { get; }
    IList<ItemDrop.ItemData> Items { get; }
    void Save();
}

sealed class ExtendedZDO : ZDO
{
    ZDOID _lastId = ZDOID.None;

    ConcurrentDictionary<Type, object>? _componentFieldAccessors;
    Dictionary<Processor, uint>? _processorDataRevisions;
    PrefabInfo _prefabInfo = PrefabInfo.Dummy;
    ZDOInventory? _inventory;
    PlayerData_? _playerData;
    
    public PrefabInfo PrefabInfo
    {
        get
        {
            if (_lastId != m_uid)
            {
                if (SharedProcessorState.PrefabInfo.TryGetValue(GetPrefab(), out var prefabInfo))
                    _prefabInfo = prefabInfo;
                else
                    _prefabInfo = PrefabInfo.Dummy;
                _lastId = m_uid;

                _hasFields = null;
                _componentFieldAccessors?.Clear();
                _processorDataRevisions?.Clear();
                _inventory = null;
            }
            return _prefabInfo;
        }
    }

    public IZDOInventory Inventory => (_inventory ??= (PrefabInfo.Container is not null ? new(this) : throw new InvalidOperationException())).Update();
    public PlayerData_ PlayerData => _playerData ??= (PrefabInfo.Player is not null ? new() : throw new InvalidOperationException());

    static readonly int __hasFieldsHash = ZNetView.CustomFieldsStr.GetStableHashCode();
    bool? _hasFields;
    public bool HasFields => _hasFields ??= GetBool(__hasFieldsHash);

    void SetHasFields()
    {
        if (_hasFields is not true)
        {
            Set(__hasFieldsHash, true);
            _hasFields = true;
        }
    }

    public void UpdateProcessorDataRevision(Processor processor)
        => (_processorDataRevisions ??= new())[processor] = DataRevision;

    public bool CheckProcessorDataRevisionChanged(Processor processor)
    {
        if (_processorDataRevisions is null || !_processorDataRevisions.TryGetValue(processor, out var dataRevision) || dataRevision != DataRevision)
            return true;
        return false;
    }

    public void Destroy()
    {
        ClaimOwnershipInternal();
        ZDOMan.instance.DestroyZDO(this);
        _hasFields = null;
        _componentFieldAccessors = null;
        _processorDataRevisions = null;
        _inventory = null;
        _playerData = null;
    }

    public ExtendedZDO Recreate()
    {
        var prefab = GetPrefab();
        var pos = GetPosition();
        var owner = GetOwner();
        var pkg = new ZPackage();
        Serialize(pkg);

        ClaimOwnershipInternal();
        ZDOMan.instance.DestroyZDO(this);

        var zdo = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, prefab);
        zdo.Deserialize(new(pkg.GetArray()));
        zdo.SetOwnerInternal(owner);
        (zdo._hasFields, _hasFields) = (_hasFields, null);
        (zdo._componentFieldAccessors, _componentFieldAccessors) = (_componentFieldAccessors, null);
        (zdo._processorDataRevisions, _processorDataRevisions) = (_processorDataRevisions, null);
        (zdo._inventory, _inventory) = (_inventory, null);
        zdo._inventory?.UpdateZDO(zdo);
        (zdo._playerData, _playerData) = (_playerData, null);
        return zdo;
    }

    public void ClaimOwnership() => SetOwner(ZDOMan.GetSessionID());
    public void ClaimOwnershipInternal() => SetOwnerInternal(ZDOMan.GetSessionID());

    public ComponentFieldAccessor<TComponent> Fields<TComponent>() where TComponent : MonoBehaviour
        => (ComponentFieldAccessor<TComponent>)(_componentFieldAccessors ??= new()).GetOrAdd(typeof(TComponent), key => new ComponentFieldAccessor<TComponent>(this, (TComponent)PrefabInfo.Components[key]));


    public sealed class ComponentFieldAccessor<TComponent>(ExtendedZDO zdo, TComponent component)
    {
        readonly ExtendedZDO _zdo = zdo;
        readonly TComponent _component = component;
        bool? _hasComponentFields;

        static readonly int __hasComponentFieldsHash = $"{ZNetView.CustomFieldsStr}{typeof(TComponent).Name}".GetStableHashCode();
        bool HasFields => _zdo.HasFields && (_hasComponentFields ??= _zdo.GetBool(__hasComponentFieldsHash));
        void SetHasFields(bool value)
        {
            if (value && !_zdo.HasFields)
                _zdo.SetHasFields();

            if (_hasComponentFields != value)
                _zdo.Set(__hasComponentFieldsHash, (_hasComponentFields = value).Value);
        }

        static int GetHash<T>(Expression<Func<TComponent, T>> fieldExpression, out FieldInfo field)
        {
            var body = (MemberExpression)fieldExpression.Body;
            field = (FieldInfo)body.Member;
            return $"{typeof(TComponent).Name}.{field.Name}".GetStableHashCode();
        }

        T Get<T>(Expression<Func<TComponent, T>> fieldExpression, Func<ZDO, int, T?, T> getter)
        {
            var body = (MemberExpression)fieldExpression.Body;
            var field = (FieldInfo)body.Member;
            if (!HasFields)
                return (T)field.GetValue(_component);

            var hash = $"{typeof(TComponent).Name}.{field.Name}".GetStableHashCode();
            return getter(_zdo, hash, _component is null ? default : (T)field.GetValue(_component));
        }

        public bool GetBool(Expression<Func<TComponent, bool>> fieldExpression)
            => Get(fieldExpression, static (zdo, hash, defaultValue) => zdo.GetBool(hash, defaultValue));

        public float GetFloat(Expression<Func<TComponent, float>> fieldExpression)
            => Get(fieldExpression, static (zdo, hash, defaultValue) => zdo.GetFloat(hash, defaultValue));

        public int GetInt(Expression<Func<TComponent, int>> fieldExpression)
            => Get(fieldExpression, static (zdo, hash, defaultValue) => zdo.GetInt(hash, defaultValue));

        public string GetString(Expression<Func<TComponent, string>> fieldExpression)
            => Get(fieldExpression, static (zdo, hash, defaultValue) => zdo.GetString(hash, defaultValue));

        ComponentFieldAccessor<TComponent> SetCore<T>(Expression<Func<TComponent, T>> fieldExpression, T value, Action<ZDO, int> remover, Action<ZDO, int, T> setter)
            where T : notnull
        {
            var hash = GetHash(fieldExpression, out var field);
            if (_component is not null && value.Equals(field.GetValue(_component)))
                remover(_zdo, hash);
            else
            {
                if (!HasFields)
                    SetHasFields(true);
                setter(_zdo, hash, value);
            }
            return this;
        }

        public ComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, bool>> fieldExpression, bool value)
            => SetCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveInt(hash), static (zdo, hash, value) => zdo.Set(hash, value));

        public ComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, float>> fieldExpression, float value)
            => SetCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveFloat(hash), static (zdo, hash, value) => zdo.Set(hash, value));

        public ComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, int>> fieldExpression, int value)
            => SetCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveInt(hash), static (zdo, hash, value) => zdo.Set(hash, value));

        public ComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, string>> fieldExpression, string value)
            => SetCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveInt(hash), static (zdo, hash, value) => zdo.Set(hash, value));

        ComponentFieldAccessor<TComponent> ResetCore<T>(Expression<Func<TComponent, T>> fieldExpression, Action<ZDO, int> remover)
        {
            var hash = GetHash(fieldExpression, out _);
            remover(_zdo, hash);
            return this;
        }

        public ComponentFieldAccessor<TComponent> Reset(Expression<Func<TComponent, bool>> fieldExpression)
            => ResetCore(fieldExpression, static (zdo, hash) => zdo.RemoveInt(hash));

        public ComponentFieldAccessor<TComponent> Reset(Expression<Func<TComponent, float>> fieldExpression)
            => ResetCore(fieldExpression, static (zdo, hash) => zdo.RemoveFloat(hash));

        public ComponentFieldAccessor<TComponent> Reset(Expression<Func<TComponent, int>> fieldExpression)
            => ResetCore(fieldExpression, static (zdo, hash) => zdo.RemoveInt(hash));

        //public ComponentFieldAccessor<TComponent> Reset(Expression<Func<TComponent, string>> fieldExpression)
        //    => ResetCore(fieldExpression, static (zdo, hash) => zdo.RemoveString(hash));
    }

    sealed class ZDOInventory(ExtendedZDO zdo) : IZDOInventory
    {
        public Inventory Inventory { get; private set; } = default!;
        public ExtendedZDO ZDO { get; private set; } = zdo;
        IList<ItemDrop.ItemData>? _items;
        uint _dataRevision = uint.MaxValue;
        string? _lastData;

        public IList<ItemDrop.ItemData> Items
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

        public ZDOInventory Update()
        {
            if (_dataRevision == ZDO.DataRevision)
                return this;

            var data = ZDO.GetString(ZDOVars.s_items);
            if (_lastData == data)
                return this;

            var fields = ZDO.Fields<Container>();
            var w = fields.GetInt(x => x.m_width);
            var h = fields.GetInt(x => x.m_height);
            if (Inventory is null || Inventory.GetWidth() != w || Inventory.GetHeight() != h)
                Inventory = new(ZDO.PrefabInfo.Container!.m_name, ZDO.PrefabInfo.Container!.m_bkg, w, h);

            if (string.IsNullOrEmpty(data))
                Items.Clear();
            else
                Inventory.Load(new(data));

            _dataRevision = ZDO.DataRevision;
            _lastData = data;
            return this;
        }

        public void UpdateZDO(ExtendedZDO zdo)
        {
            ZDO = zdo;
            _items = default;
            _dataRevision = default;
            _lastData = default;
            Update();
        }

        public void Save()
        {
            var pkg = new ZPackage();
            Inventory.Save(pkg);
            var dataRevision = ZDO.DataRevision;
            var data = pkg.GetBase64();
            ZDO.Set(ZDOVars.s_items, data);
            if (dataRevision != ZDO.DataRevision) // items changed
            {
                // moving ZDO are constantly updated, so we need to get ahead for our changes to stick.
                // Not sure about the increment value though...
                if (ZDO.PrefabInfo.ZSyncTransform is not null)
                    ZDO.DataRevision += 120;
            }

            _dataRevision = ZDO.DataRevision;
            _lastData = data;
        }
    }

    public sealed class PlayerData_
    {
        public float MaxStamina { get; set; }
        public float UpdateStaminaThreshold { get; set; }
        public float ResetStamina { get; set; } = float.NaN;
    }
}
