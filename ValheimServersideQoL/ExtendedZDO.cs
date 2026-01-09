using UnityEngine;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

interface IZDOInventoryReadOnly
{
    IReadOnlyList<ItemDrop.ItemData> Items { get; }
    float TotalWeight { get; }
}

interface IZDOInventory
{
    Inventory Inventory { get; }
    IList<ItemDrop.ItemData> Items { get; }
    float TotalWeight { get; }
    void Save();
    int? PickupRange { get; set; }
    int? FeedRange { get; set; }
}

sealed partial class ExtendedZDO : ZDO
{
    ZDOID _lastId = ZDOID.None;
    AdditionalData? _addData;

    public delegate void RecreateHandler(ExtendedZDO oldZdo, ExtendedZDO newZdo);

    AdditionalData AddData
    {
        get
        {
            if (_lastId != m_uid || _addData is null)
            {
                _lastId = m_uid;
                if (m_uid != ZDOID.None && SharedProcessorState.GetPrefabInfo(GetPrefab()) is { } prefabInfo)
                    _addData = new(prefabInfo);
                else
                    _addData = AdditionalData.Dummy;
            }
            return _addData;
        }
    }

    public void SetModAsCreator(Processor.CreatorMarkers marker = Processor.CreatorMarkers.None) => Vars.SetCreator((long)Main.PluginGuidHash | (long)((ulong)marker << 32));
    public bool IsModCreator(out Processor.CreatorMarkers marker)
    {
        marker = Processor.CreatorMarkers.None;
        if ((int)Vars.GetCreator() != Main.PluginGuidHash)
            return false;
        marker = (Processor.CreatorMarkers)((ulong)Vars.GetCreator() >> 32);
        return true;
    }
    public bool IsModCreator() => IsModCreator(out _);

    public PrefabInfo PrefabInfo => AddData.PrefabInfo;
    public IZDOInventory Inventory => (AddData.Inventory ??= (PrefabInfo.Container is not null ? new(this) : throw new InvalidOperationException())).Update();
    public IZDOInventoryReadOnly InventoryReadOnly => (AddData.Inventory ??= (PrefabInfo.Container is not null ? new(this) : throw new InvalidOperationException()));

    static readonly int __hasFieldsHash = ZNetView.CustomFieldsStr.GetStableHashCode();
    public bool HasFields => AddData.HasFields ??= GetBool(__hasFieldsHash);

    public event RecreateHandler? Recreated
    {
        add => AddData.Recreated += value;
        remove => AddData.Recreated -= value;
    }

    static bool _onZdoDestroyedRegistered;
    static void OnZdoDestroyed(ZDO zdo)
    {
        var exZdo = (ExtendedZDO)zdo;
        exZdo._addData?.Destroyed?.Invoke(exZdo);
        exZdo._addData = null;
    }

    public event Action<ExtendedZDO>? Destroyed
    {
        add
        {
            if (!_onZdoDestroyedRegistered)
            {
                ZDOMan.instance.m_onZDODestroyed += OnZdoDestroyed;
                _onZdoDestroyedRegistered = true;
            }
            AddData.Destroyed += value;
        }
        remove => AddData.Destroyed -= value;
    }

    public ZDOVars_ Vars => new(this);

    void SetHasFields()
    {
        if (AddData.HasFields is not true)
        {
            Set(__hasFieldsHash, true);
            AddData.HasFields = true;
        }
    }

    public bool HasProcessors => AddData.HasProcessors;
    public IReadOnlyList<Processor> Processors => AddData.Processors;
    public void UnregisterProcessors(IReadOnlyList<Processor> processors) => AddData.Ungregister(processors);
    public void UnregisterAllExcept(Processor processor) => AddData.UnregisterAllExcept(processor);
    public void UnregisterAllProcessors() => AddData.UnregisterAll();

    public void ReregisterAllProcessors() => _addData?.ReregisterAll();

    public void UpdateProcessorDataRevision(Processor processor)
        => (AddData.ProcessorDataRevisions ??= [])[processor] = (DataRevision, OwnerRevision);

    public void ResetProcessorDataRevision(Processor processor)
        => AddData.ProcessorDataRevisions?.Remove(processor);

    public bool CheckProcessorDataRevisionChanged(Processor processor)
    {
        if (AddData.ProcessorDataRevisions is null || !AddData.ProcessorDataRevisions.TryGetValue(processor, out var revision) || revision != (DataRevision, OwnerRevision))
            return true;
        return false;
    }

    public void Destroy()
    {
        ClaimOwnershipInternal();
        ZDOMan.instance.DestroyZDO(this);
    }

    //public record ZDOData(int Prefab, Vector3 Position, long Owner, byte[] Data);

    //public ZDOData GetDataAndDestroy()
    //{
    //    var pkg = new ZPackage();
    //    Serialize(pkg);
    //    var data = new ZDOData(GetPrefab(), GetPosition(), GetOwner(), pkg.GetArray());
    //    Destroy();
    //    return data;
    //}

    //public static ExtendedZDO Create(ZDOData data)
    //{
    //    var zdo = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(data.Position, data.Prefab);
    //    zdo.Deserialize(new(data.Data));
    //    zdo.SetOwnerInternal(data.Owner);
    //    return zdo;
    //}

    public ExtendedZDO CreateClone()
    {
        var prefab = GetPrefab();
        var pos = GetPosition();
        var owner = GetOwner();
        var pkg = new ZPackage();
        Serialize(pkg);

        var zdo = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, prefab);
        zdo.Deserialize(new(pkg.GetArray()));
        zdo.SetOwnerInternal(owner);
        return zdo;
    }

    public ExtendedZDO Recreate()
    {
        var zdo = CreateClone();

        // Call before Destroy and thus before ZDOMan.instance.m_onZDODestroyed
        _addData?.Recreated?.Invoke(this, zdo);

        Destroy();
        return zdo;
    }

    public DateTimeOffset OwnerTimestamp { get; private set; }
    public PlayerProcessor.IPeerInfo? OwnerPeerInfo
    {
        get
        {
            if (!HasOwner())
                return null;
            var owner = GetOwner();
            if (field?.Owner != owner)
            {
                void OnOwnerPlayerZdoDestroyed(ZDO zdo) => field = null;
                field?.PlayerZDO.Destroyed -= OnOwnerPlayerZdoDestroyed;
                field = Processor.Instance<PlayerProcessor>().GetPeerInfo(owner);
                field?.PlayerZDO.Destroyed += OnOwnerPlayerZdoDestroyed;
            }
            return field;
        }
    }

    public new void SetOwner(long uid)
    {
        OwnerTimestamp = DateTimeOffset.UtcNow;
        base.SetOwner(uid);
    }

    public new void SetOwnerInternal(long uid)
    {
        OwnerTimestamp = DateTimeOffset.UtcNow;
        base.SetOwnerInternal(uid);
    }

    public void ClaimOwnership() => SetOwner(ZDOMan.GetSessionID());
    public void ClaimOwnershipInternal() => SetOwnerInternal(ZDOMan.GetSessionID());
    public void ReleaseOwnership() => SetOwner(0);
    public void ReleaseOwnershipInternal() => SetOwnerInternal(0);

    public bool IsOwnerOrUnassigned() => !HasOwner() || IsOwner();

    public TimeSpan GetTimeSinceSpawned() => ZNet.instance.GetTime() - Vars.GetSpawnTime();

    public ComponentFieldAccessor<TComponent> Fields<TComponent>(bool getUnknownComponent = false) where TComponent : MonoBehaviour
    {
        if (!ReferenceEquals(AddData, AdditionalData.Dummy))
        {
            return (ComponentFieldAccessor<TComponent>)(AddData.ComponentFieldAccessors ??= new()).GetOrAdd(typeof(TComponent), key =>
            {
                if (!PrefabInfo.Components.TryGetValue(key, out var component) && getUnknownComponent)
                    component = PrefabInfo.Prefab.GetComponentInChildren<TComponent>();
                if (component is null)
                    throw new KeyNotFoundException();
                return new ComponentFieldAccessor<TComponent>(this, (TComponent)component);
            });
        }
        else if (getUnknownComponent)
        {
            if (ZNetScene.instance.GetPrefab(GetPrefab())?.GetComponentInChildren<TComponent>() is not { } component)
                throw new KeyNotFoundException();
            return new ComponentFieldAccessor<TComponent>(this, component);
        }
        throw new InvalidOperationException();
    }
}
