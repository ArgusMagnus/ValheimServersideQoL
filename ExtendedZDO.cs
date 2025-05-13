using System.Collections.Concurrent;
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
    int? PickupRange { get; set; }
    int? FeedRange { get; set; }
}

sealed class ExtendedZDO : ZDO
{
    ZDOID _lastId = ZDOID.None;
    AdditionalData_? _addData;

    public delegate void RecreateHandler(ExtendedZDO oldZdo, ExtendedZDO newZdo);

    AdditionalData_ AddData
    {
        get
        {
            if (_lastId != m_uid || _addData is null)
            {
                _lastId = m_uid;
                if (m_uid != ZDOID.None && SharedProcessorState.GetPrefabInfo(GetPrefab()) is { } prefabInfo)
                    _addData = new(prefabInfo);
                else
                    _addData = AdditionalData_.Dummy;
            }
            return _addData;
        }
    }

    public PrefabInfo PrefabInfo => AddData.PrefabInfo;
    public IZDOInventory Inventory => (AddData.Inventory ??= (PrefabInfo.Container is not null ? new(this) : throw new InvalidOperationException())).Update();

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

    public IReadOnlyList<Processor> Processors => AddData.Processors;
    public void UnregisterProcessors(IEnumerable<Processor> processors) => AddData.Ungregister(processors);

    public void ReregisterAllProcessors() => _addData?.ReregisterAll();

    public void UpdateProcessorDataRevision(Processor processor)
        => (AddData.ProcessorDataRevisions ??= new())[processor] = DataRevision;

    public void ResetProcessorDataRevision(Processor processor)
        => AddData.ProcessorDataRevisions?.Remove(processor);

    public bool CheckProcessorDataRevisionChanged(Processor processor)
    {
        if (AddData.ProcessorDataRevisions is null || !AddData.ProcessorDataRevisions.TryGetValue(processor, out var dataRevision) || dataRevision != DataRevision)
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

    public ExtendedZDO Recreate()
    {
        var prefab = GetPrefab();
        var pos = GetPosition();
        var owner = GetOwner();
        var pkg = new ZPackage();
        Serialize(pkg);

        var zdo = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, prefab);
        zdo.Deserialize(new(pkg.GetArray()));
        zdo.SetOwnerInternal(owner);

        // Call before Destroy and thus before ZDOMan.instance.m_onZDODestroyed
        _addData?.Recreated?.Invoke(this, zdo);

        Destroy();
        return zdo;
    }

    public void ClaimOwnership() => SetOwner(ZDOMan.GetSessionID());
    public void ClaimOwnershipInternal() => SetOwnerInternal(ZDOMan.GetSessionID());

    public TimeSpan GetTimeSinceSpawned() => ZNet.instance.GetTime() - Vars.GetSpawnTime();

    public ComponentFieldAccessor<TComponent> Fields<TComponent>(bool getUnknownComponent = false) where TComponent : MonoBehaviour
        => (ComponentFieldAccessor<TComponent>)(AddData.ComponentFieldAccessors ??= new()).GetOrAdd(typeof(TComponent), key =>
        {
            if (!PrefabInfo.Components.TryGetValue(key, out var component) && getUnknownComponent)
                component = PrefabInfo.Prefab.GetComponentInChildren<TComponent>();
            if (component is null)
                throw new KeyNotFoundException();
            return new ComponentFieldAccessor<TComponent>(this, (TComponent)component);
        });

    public readonly struct ZDOVars_(ExtendedZDO zdo)
    {
        readonly ExtendedZDO _zdo = zdo;
        public int GetState(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_state, defaultValue);
        public void SetState(int value) => _zdo.Set(ZDOVars.s_state, value);
        public long GetCreator(long defaultValue = default) => _zdo.GetLong(ZDOVars.s_creator, defaultValue);
        public void SetCreator(long value) => _zdo.Set(ZDOVars.s_creator, value);
        public bool GetInUse(bool defaultValue = default) => _zdo.GetBool(ZDOVars.s_inUse, defaultValue);
        public void SetInUse(bool value) => _zdo.Set(ZDOVars.s_inUse, value);
        public float GetFuel(float defaultValue = default) => _zdo.GetFloat(ZDOVars.s_fuel, defaultValue);
        public void SetFuel(float value) => _zdo.Set(ZDOVars.s_fuel, value);
        public bool GetPiece(bool defaultValue = default) => _zdo.GetBool(ZDOVars.s_piece, defaultValue);
        public void SetPiece(bool value) => _zdo.Set(ZDOVars.s_piece, value);
        public string GetItems(string defaultValue = "") => _zdo.GetString(ZDOVars.s_items, defaultValue);
        public void SetItems(string value) => _zdo.Set(ZDOVars.s_items, value);
        public string GetTag(string defaultValue = "") => _zdo.GetString(ZDOVars.s_tag, defaultValue);
        public void SetTag(string value) => _zdo.Set(ZDOVars.s_tag, value);
        public byte[]? GetData(byte[]? defaultValue = null) => _zdo.GetByteArray(ZDOVars.s_data, defaultValue);
        public void SetData(byte[]? value) => _zdo.Set(ZDOVars.s_data, value);
        public float GetStamina(float defaultValue = default) => _zdo.GetFloat(ZDOVars.s_stamina, defaultValue);
        public void SetStamina(float value) => _zdo.Set(ZDOVars.s_stamina, value);
        public long GetPlayerID(long defaultValue = default) => _zdo.GetLong(ZDOVars.s_playerID, defaultValue);
        public void SetPlayerID(long value) => _zdo.Set(ZDOVars.s_playerID, value);
        public string GetPlayerName(string defaultValue = "") => _zdo.GetString(ZDOVars.s_playerName, defaultValue);
        public void SetPlayerName(string value) => _zdo.Set(ZDOVars.s_playerName, value);
        public string GetFollow(string defaultValue = "") => _zdo.GetString(ZDOVars.s_follow, defaultValue);
        public void SetFollow(string value) => _zdo.Set(ZDOVars.s_follow, value);
        public int GetRightItem(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_rightItem, defaultValue);
        public void SetRightItem(int value) => _zdo.Set(ZDOVars.s_rightItem, value);
        public string GetText(string defaultValue = "") => _zdo.GetString(ZDOVars.s_text, defaultValue);
        public void SetText(string value) => _zdo.Set(ZDOVars.s_text, value);
        public string GetItem(string defaultValue = "") => _zdo.GetString(ZDOVars.s_item, defaultValue);
        public void SetItem(string value) => _zdo.Set(ZDOVars.s_item, value);
        public string GetItem(int idx, string defaultValue = "") => _zdo.GetString(Invariant($"item{idx}"), defaultValue);
        public void SetItem(int idx, string value) => _zdo.Set(Invariant($"item{idx}"), value);
        public int GetQueued(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_queued, defaultValue);
        public void SetQueued(int value) => _zdo.Set(ZDOVars.s_queued, value);
        public bool GetTamed(bool defaultValue = default) => _zdo.GetBool(ZDOVars.s_tamed, defaultValue);
        public void SetTamed(bool value) => _zdo.Set(ZDOVars.s_tamed, value);
        public float GetTameTimeLeft(float defaultValue = default) => _zdo.GetFloat(ZDOVars.s_tameTimeLeft, defaultValue);
        public void SetTameTimeLeft(float value) => _zdo.Set(ZDOVars.s_tameTimeLeft, value);
        public int GetAmmo(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_ammo, defaultValue);
        public void SetAmmo(int value) => _zdo.Set(ZDOVars.s_ammo, value);
        public string GetAmmoType(string defaultValue = "") => _zdo.GetString(ZDOVars.s_ammoType, defaultValue);
        public void SetAmmoType(string value) => _zdo.Set(ZDOVars.s_ammoType, value);
        public float GetGrowStart(float defaultValue = default) => _zdo.GetFloat(ZDOVars.s_growStart, defaultValue);
        public void SetGrowStart(float value) => _zdo.Set(ZDOVars.s_growStart, value);
        public DateTime GetSpawnTime(DateTime defaultValue = default) => new(_zdo.GetLong(ZDOVars.s_spawnTime, defaultValue.Ticks));
        public void SetSpawnTime(DateTime value) => _zdo.Set(ZDOVars.s_spawnTime, value.Ticks);
        public float GetHealth(float defaultValue = default) => _zdo.GetFloat(ZDOVars.s_health, defaultValue);
        public void SetHealth(float value) => _zdo.Set(ZDOVars.s_health, value);
        public int GetPermitted(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_permitted, defaultValue);
        public void SetPermitted(int value) => _zdo.Set(ZDOVars.s_permitted, value);
        public int GetLevel(int defaultValue = 1) => _zdo.GetInt(ZDOVars.s_level, defaultValue);
        public void SetLevel(int value) => _zdo.Set(ZDOVars.s_level, value);
        public bool GetPatrol(bool defaultValue = default) => _zdo.GetBool(ZDOVars.s_patrol, defaultValue);
        public void SetPatrol(bool value) => _zdo.Set(ZDOVars.s_patrol, value);
        public Vector3 GetPatrolPoint(Vector3 defaultValue = default) => _zdo.GetVec3(ZDOVars.s_patrolPoint, defaultValue);
        public void SetPatrolPoint(Vector3 value) => _zdo.Set(ZDOVars.s_patrolPoint, value);
        public Vector3 GetSpawnPoint(Vector3 defaultValue = default) => _zdo.GetVec3(ZDOVars.s_spawnPoint, defaultValue);
        public void SetSpawnPoint(Vector3 value) => _zdo.Set(ZDOVars.s_spawnPoint, value);
        public int GetEmoteID(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_emoteID, defaultValue);
        public Emotes GetEmote(Emotes defaultValue = ModConfig.PlayersConfig.DisabledEmote) => Enum.TryParse<Emotes>(_zdo.GetString(ZDOVars.s_emote), true, out var e) ? e : defaultValue;


        static int __lastSpawnedTime = $"{Main.PluginGuid}.LastSpawnedTime".GetStableHashCode();
        public DateTimeOffset GetLastSpawnedTime(DateTimeOffset defaultValue = default) => new(_zdo.GetLong(__lastSpawnedTime, defaultValue.Ticks), default);
        public void SetLastSpawnedTime(DateTimeOffset value) => _zdo.Set(__lastSpawnedTime, value.Ticks - value.Offset.Ticks);

        static int __portalHubId = $"{Main.PluginGuid}.PortalHubId".GetStableHashCode();
        public int GetPortalHubId(int defaultValue = default) => _zdo.GetInt(__portalHubId, defaultValue);
        public void SetPortalHubId(int value) => _zdo.Set(__portalHubId, value);
    }

    sealed class AdditionalData_(PrefabInfo prefabInfo)
    {
        public IReadOnlyList<Processor> Processors { get; private set; } = Processor.DefaultProcessors;
        public PrefabInfo PrefabInfo { get; } = prefabInfo;
        public ConcurrentDictionary<Type, object>? ComponentFieldAccessors { get; set; }
        public Dictionary<Processor, uint>? ProcessorDataRevisions { get; set; }
        public ZDOInventory? Inventory { get; set; }
        public bool? HasFields { get; set; }
        public RecreateHandler? Recreated { get; set; }
        public Action<ExtendedZDO>? Destroyed { get; set; }

        static ConcurrentDictionary<int, IReadOnlyList<Processor>> _processors = new();

        public void Ungregister(IEnumerable<Processor> processors)
        {            
            var hash = 0;
            foreach (var processor in Processors)
            {
                if (!processors.Any(x => ReferenceEquals(x, processor)))
                    hash = (hash, processor.GetType()).GetHashCode();
            }

            Processors = _processors.GetOrAdd(hash, _ => Processors.Where(x => !processors.Any(y => ReferenceEquals(x, y))).ToList());
            foreach (var processor in processors)
                ProcessorDataRevisions?.Remove(processor);
        }

        public void ReregisterAll() => Processors = Processor.DefaultProcessors;

        public static AdditionalData_ Dummy { get; } = new(PrefabInfo.Dummy);
    }

    public sealed class ComponentFieldAccessor<TComponent>(ExtendedZDO zdo, TComponent component)
    {
        readonly ExtendedZDO _zdo = zdo;
        readonly TComponent _component = component;
        bool? _hasComponentFields;

        static readonly int __hasComponentFieldsHash = Invariant($"{ZNetView.CustomFieldsStr}{typeof(TComponent).Name}").GetStableHashCode();
        public bool HasFields => _zdo.HasFields && (_hasComponentFields ??= _zdo.GetBool(__hasComponentFieldsHash));
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
            return Invariant($"{typeof(TComponent).Name}.{field.Name}").GetStableHashCode();
        }

        T Get<T>(Expression<Func<TComponent, T>> fieldExpression, Func<ZDO, int, T?, T> getter)
        {
            var body = (MemberExpression)fieldExpression.Body;
            var field = (FieldInfo)body.Member;
            if (!HasFields)
                return (T)field.GetValue(_component);

            var hash = Invariant($"{typeof(TComponent).Name}.{field.Name}").GetStableHashCode();
            return getter(_zdo, hash, (T)field.GetValue(_component));
        }

        public bool GetBool(Expression<Func<TComponent, bool>> fieldExpression)
            => Get(fieldExpression, static (zdo, hash, defaultValue) => zdo.GetBool(hash, defaultValue));

        public float GetFloat(Expression<Func<TComponent, float>> fieldExpression)
            => Get(fieldExpression, static (zdo, hash, defaultValue) => zdo.GetFloat(hash, defaultValue));

        public int GetInt(Expression<Func<TComponent, int>> fieldExpression)
            => Get(fieldExpression, static (zdo, hash, defaultValue) => zdo.GetInt(hash, defaultValue));

        public string GetString(Expression<Func<TComponent, string>> fieldExpression)
            => Get(fieldExpression, static (zdo, hash, defaultValue) => zdo.GetString(hash, defaultValue));

        ComponentFieldAccessor<TComponent> SetCore<TObj, T>(Expression<Func<TComponent, TObj>> fieldExpression, TObj valueObj, Action<ZDO, int>? remover, Action<ZDO, int, T> setter, Func<TObj, T> cast)
            where TObj : notnull
            where T : notnull
        {
            var hash = GetHash(fieldExpression, out var field);
            var value = cast(valueObj);
            if (remover is not null && value.Equals(cast((TObj)field.GetValue(_component))))
                remover(_zdo, hash);
            else
            {
                if (!HasFields)
                    SetHasFields(true);
                setter(_zdo, hash, value);
            }
            return this;
        }

        ComponentFieldAccessor<TComponent> SetCore<T>(Expression<Func<TComponent, T>> fieldExpression, T value, Action<ZDO, int>? remover, Action<ZDO, int, T> setter)
            where T : notnull
            => SetCore(fieldExpression, value, remover, setter, static x => x);

        public ComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, bool>> fieldExpression, bool value)
            => SetCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveInt(hash), static (zdo, hash, value) => zdo.Set(hash, value));

        public ComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, float>> fieldExpression, float value)
            => SetCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveFloat(hash), static (zdo, hash, value) => zdo.Set(hash, value));

        public ComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, int>> fieldExpression, int value)
            => SetCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveInt(hash), static (zdo, hash, value) => zdo.Set(hash, value));

        public ComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, Vector3>> fieldExpression, Vector3 value)
            => SetCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveVec3(hash), static (zdo, hash, value) => zdo.Set(hash, value));

        public ComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, string>> fieldExpression, string value)
            => SetCore(fieldExpression, value, null, static (zdo, hash, value) => zdo.Set(hash, value));

        public ComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, GameObject>> fieldExpression, GameObject value)
            => SetCore(fieldExpression, value, null, static (zdo, hash, value) => zdo.Set(hash, value), static x => x.name);

        public ComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, ItemDrop>> fieldExpression, ItemDrop value)
            => SetCore(fieldExpression, value, null, static (zdo, hash, value) => zdo.Set(hash, value), static x => x.name);

        bool SetIfChangedCore<TObj, T>(Expression<Func<TComponent, TObj>> fieldExpression, TObj valueObj, Action<ZDO, int>? remover, Action<ZDO, int, T> setter, Func<ZDO, int, T?, T> getter, Func<TObj, T> cast)
            where TObj : notnull
            where T : notnull
        {
            var hash = GetHash(fieldExpression, out var field);
            var defaultValue = cast((TObj)field.GetValue(_component));
            var value = cast(valueObj);
            if (value.Equals(getter(_zdo, hash, defaultValue)))
                return false;

            if (remover is not null && value.Equals(defaultValue))
                remover(_zdo, hash);
            else
            {
                if (!HasFields)
                    SetHasFields(true);
                setter(_zdo, hash, value);
            }
            return true;
        }

        bool SetIfChangedCore<T>(Expression<Func<TComponent, T>> fieldExpression, T value, Action<ZDO, int>? remover, Action<ZDO, int, T> setter, Func<ZDO, int, T?, T> getter)
            where T : notnull
            => SetIfChangedCore(fieldExpression, value, remover, setter, getter, static x => x);

        public bool SetIfChanged(Expression<Func<TComponent, bool>> fieldExpression, bool value)
            => SetIfChangedCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveInt(hash), static (zdo, hash, value) => zdo.Set(hash, value), static (zdo, hash, defaultValue) => zdo.GetBool(hash, defaultValue));

        public bool SetIfChanged(Expression<Func<TComponent, float>> fieldExpression, float value)
            => SetIfChangedCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveFloat(hash), static (zdo, hash, value) => zdo.Set(hash, value), static (zdo, hash, defaultValue) => zdo.GetFloat(hash, defaultValue));

        public bool SetIfChanged(Expression<Func<TComponent, int>> fieldExpression, int value)
            => SetIfChangedCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveInt(hash), static (zdo, hash, value) => zdo.Set(hash, value), static (zdo, hash, defaultValue) => zdo.GetInt(hash, defaultValue));

        public bool SetIfChanged(Expression<Func<TComponent, string>> fieldExpression, string value)
            => SetIfChangedCore(fieldExpression, value, null, static (zdo, hash, value) => zdo.Set(hash, value), static (zdo, hash, defaultValue) => zdo.GetString(hash, defaultValue));

        public bool SetIfChanged(Expression<Func<TComponent, GameObject>> fieldExpression, GameObject value)
            => SetIfChangedCore(fieldExpression, value, null, static (zdo, hash, value) => zdo.Set(hash, value), static (zdo, hash, defaultValue) => zdo.GetString(hash, defaultValue), static x => x.name);

        public bool SetIfChanged(Expression<Func<TComponent, ItemDrop>> fieldExpression, ItemDrop value)
            => SetIfChangedCore(fieldExpression, value, null, static (zdo, hash, value) => zdo.Set(hash, value), static (zdo, hash, defaultValue) => zdo.GetString(hash, defaultValue), static x => x.name);

        ComponentFieldAccessor<TComponent> ResetCore<T>(Expression<Func<TComponent, T>> fieldExpression, Action<ZDO, int> remover)
        {
            var hash = GetHash(fieldExpression, out _);
            remover(_zdo, hash);
            return this;
        }

        ComponentFieldAccessor<TComponent> ResetCore<TObj, T>(Expression<Func<TComponent, TObj>> fieldExpression, Action<ZDO, int, T> setter, Func<TObj, T> cast)
            where TObj : notnull
            where T : notnull
        {
            var hash = GetHash(fieldExpression, out var field);
            var defaultValue = cast((TObj)field.GetValue(_component));
            setter(_zdo, hash, defaultValue);
            return this;
        }

        public ComponentFieldAccessor<TComponent> Reset(Expression<Func<TComponent, bool>> fieldExpression)
            => ResetCore(fieldExpression, static (zdo, hash) => zdo.RemoveInt(hash));

        public ComponentFieldAccessor<TComponent> Reset(Expression<Func<TComponent, float>> fieldExpression)
            => ResetCore(fieldExpression, static (zdo, hash) => zdo.RemoveFloat(hash));

        public ComponentFieldAccessor<TComponent> Reset(Expression<Func<TComponent, int>> fieldExpression)
            => ResetCore(fieldExpression, static (zdo, hash) => zdo.RemoveInt(hash));

        public ComponentFieldAccessor<TComponent> Reset(Expression<Func<TComponent, string>> fieldExpression)
            => ResetCore(fieldExpression, static (zdo, hash, value) => zdo.Set(hash, value), static x => x);

        public ComponentFieldAccessor<TComponent> Reset(Expression<Func<TComponent, GameObject>> fieldExpression)
            => ResetCore(fieldExpression, static (zdo, hash, value) => zdo.Set(hash, value), static x => x.name);

        public ComponentFieldAccessor<TComponent> Reset(Expression<Func<TComponent, ItemDrop>> fieldExpression)
            => ResetCore(fieldExpression, static (zdo, hash, value) => zdo.Set(hash, value), static x => x.name);

        bool ResetIfChangedCore<T>(Expression<Func<TComponent, T>> fieldExpression, Func<ZDO, int, bool> remover)
        {
            var hash = GetHash(fieldExpression, out _);
            return remover(_zdo, hash);
        }

        bool ResetIfChangedCore<TObj, T>(Expression<Func<TComponent, TObj>> fieldExpression, Action<ZDO, int, T> setter, Func<ZDO, int, T?, T> getter, Func<TObj, T> cast)
            where TObj : notnull
            where T : notnull
        {
            var hash = GetHash(fieldExpression, out var field);
            var defaultValue = cast((TObj)field.GetValue(_component));
            var value = getter(_zdo, hash, defaultValue);
            if (value.Equals(defaultValue))
                return false;
            setter(_zdo, hash, defaultValue);
            return true;
        }

        public bool ResetIfChanged(Expression<Func<TComponent, bool>> fieldExpression)
            => ResetIfChangedCore(fieldExpression, static (zdo, hash) => zdo.RemoveInt(hash));

        public bool ResetIfChanged(Expression<Func<TComponent, float>> fieldExpression)
            => ResetIfChangedCore(fieldExpression, static (zdo, hash) => zdo.RemoveFloat(hash));

        public bool ResetIfChanged(Expression<Func<TComponent, int>> fieldExpression)
            => ResetIfChangedCore(fieldExpression, static (zdo, hash) => zdo.RemoveInt(hash));

        public bool ResetIfChanged(Expression<Func<TComponent, string>> fieldExpression)
            => ResetIfChangedCore(fieldExpression, static (zdo, hash, value) => zdo.Set(hash, value), static (zdo, hash, defaultValue) => zdo.GetString(hash, defaultValue), static x => x);

        public bool ResetIfChanged(Expression<Func<TComponent, GameObject>> fieldExpression)
            => ResetIfChangedCore(fieldExpression, static (zdo, hash, value) => zdo.Set(hash, value), static (zdo, hash, defaultValue) => zdo.GetString(hash, defaultValue), static x => x.name);

        public bool ResetIfChanged(Expression<Func<TComponent, ItemDrop>> fieldExpression)
            => ResetIfChangedCore(fieldExpression, static (zdo, hash, value) => zdo.Set(hash, value), static (zdo, hash, defaultValue) => zdo.GetString(hash, defaultValue), static x => x.name);

        public bool SetOrReset(Expression<Func<TComponent, bool>> fieldExpression, bool set, bool setValue)
            => set ? SetIfChanged(fieldExpression, setValue) : ResetIfChanged(fieldExpression);

        public bool SetOrReset(Expression<Func<TComponent, float>> fieldExpression, bool set, float setValue)
            => set ? SetIfChanged(fieldExpression, setValue) : ResetIfChanged(fieldExpression);

        public bool SetOrReset(Expression<Func<TComponent, int>> fieldExpression, bool set, int setValue)
            => set ? SetIfChanged(fieldExpression, setValue) : ResetIfChanged(fieldExpression);

        public bool SetOrReset(Expression<Func<TComponent, string>> fieldExpression, bool set, string setValue)
            => set ? SetIfChanged(fieldExpression, setValue) : ResetIfChanged(fieldExpression);

        public bool SetOrReset(Expression<Func<TComponent, GameObject>> fieldExpression, bool set, GameObject setValue)
            => set ? SetIfChanged(fieldExpression, setValue) : ResetIfChanged(fieldExpression);

        public bool SetOrReset(Expression<Func<TComponent, ItemDrop>> fieldExpression, bool set, ItemDrop setValue)
            => set ? SetIfChanged(fieldExpression, setValue) : ResetIfChanged(fieldExpression);
    }

    sealed class ZDOInventory(ExtendedZDO zdo) : IZDOInventory
    {
        public Inventory Inventory { get; private set; } = default!;
        public ExtendedZDO ZDO { get; private set; } = zdo;
        public int? PickupRange { get; set; }
        public int? FeedRange { get; set; }

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

            var data = ZDO.Vars.GetItems();
            if (_lastData == data)
                return this;

            var fields = ZDO.Fields<Container>();
            var w = fields.GetInt(x => x.m_width);
            var h = fields.GetInt(x => x.m_height);
            if (Inventory is null || Inventory.GetWidth() != w || Inventory.GetHeight() != h)
                Inventory = new(ZDO.PrefabInfo.Container!.Value.Container.m_name, ZDO.PrefabInfo.Container!.Value.Container.m_bkg, w, h);

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
            ZDO.Vars.SetItems(data);
            if (dataRevision != ZDO.DataRevision) // items changed
            {
                // moving ZDO are constantly updated, so we need to get ahead for our changes to stick.
                // Not sure about the increment value though...
                if (ZDO.PrefabInfo.Container is { ZSyncTransform: { Value: not null } })
                    ZDO.DataRevision += 120;
            }

            _dataRevision = ZDO.DataRevision;
            _lastData = data;
        }
    }
}
