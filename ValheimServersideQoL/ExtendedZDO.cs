using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Valheim.ServersideQoL.CodeAnalysis;
using Valheim.ServersideQoL.Processors;
using YamlDotNet.Core.Tokens;

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
        if (!ReferenceEquals(AddData, AdditionalData_.Dummy))
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

    public readonly struct ZDOVars_(ExtendedZDO zdo)
    {
        readonly ExtendedZDO _zdo = zdo;

        void ValidateOwnership(string filePath, int lineNo)
        {
#if !DEBUG
            if (!Main.Instance.Config.General.DiagnosticLogs.Value)
                return;
#endif
            if (_zdo.PrefabInfo.Container is null || _zdo.IsOwnerOrUnassigned() || _zdo.IsModCreator())
                return;

            Main.Instance.Logger.LogWarning($"{Path.GetFileName(filePath)} L{lineNo}: Container was modified while it is owned by a client, which can lead to the loss of items.");
        }

        public int GetState(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_state, defaultValue);
        public void SetState(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_state, value); }
        public long GetCreator(long defaultValue = default) => _zdo.GetLong(ZDOVars.s_creator, defaultValue);
        public void SetCreator(long value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_creator, value); }
        public bool GetInUse(bool defaultValue = default) => _zdo.GetBool(ZDOVars.s_inUse, defaultValue);
        public void SetInUse(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_inUse, value); }
        public float GetFuel(float defaultValue = default) => _zdo.GetFloat(ZDOVars.s_fuel, defaultValue);
        public void SetFuel(float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_fuel, value); }
        public bool GetPiece(bool defaultValue = default) => _zdo.GetBool(ZDOVars.s_piece, defaultValue);
        public void SetPiece(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_piece, value); }
        public string GetItems(string defaultValue = "") => _zdo.GetString(ZDOVars.s_items, defaultValue);
        public void SetItems(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_items, value); }
        public string GetTag(string defaultValue = "") => _zdo.GetString(ZDOVars.s_tag, defaultValue);
        public void SetTag(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_tag, value); }
        public byte[]? GetData(byte[]? defaultValue = null) => _zdo.GetByteArray(ZDOVars.s_data, defaultValue);
        public void SetData(byte[]? value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_data, value); }
        public float GetStamina(float defaultValue = default) => _zdo.GetFloat(ZDOVars.s_stamina, defaultValue);
        public void SetStamina(float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_stamina, value); }
        public long GetPlayerID(long defaultValue = default) => _zdo.GetLong(ZDOVars.s_playerID, defaultValue);
        public void SetPlayerID(long value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_playerID, value); }
        public string GetPlayerName(string defaultValue = "") => _zdo.GetString(ZDOVars.s_playerName, defaultValue);
        public void SetPlayerName(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_playerName, value); }
        public string GetFollow(string defaultValue = "") => _zdo.GetString(ZDOVars.s_follow, defaultValue);
        public void SetFollow(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_follow, value); }
        public int GetRightItem(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_rightItem, defaultValue);
        public int GetLeftItem(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_leftItem, defaultValue);
        public string GetText(string defaultValue = "") => _zdo.GetString(ZDOVars.s_text, defaultValue);
        public void SetText(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_text, value); }
        public string GetItem(string defaultValue = "") => _zdo.GetString(ZDOVars.s_item, defaultValue);
        public void SetItem(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_item, value); }
        public string GetItem(int idx, string defaultValue = "") => _zdo.GetString(Invariant($"item{idx}"), defaultValue);
        public void SetItem(int idx, string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(Invariant($"item{idx}"), value); }
        public int GetQueued(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_queued, defaultValue);
        public void SetQueued(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_queued, value); }
        public bool GetTamed(bool defaultValue = default) => _zdo.GetBool(ZDOVars.s_tamed, defaultValue);
        public void SetTamed(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_tamed, value); }
        public float GetTameTimeLeft(float defaultValue = default) => _zdo.GetFloat(ZDOVars.s_tameTimeLeft, defaultValue);
        public void SetTameTimeLeft(float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_tameTimeLeft, value); }
        public int GetAmmo(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_ammo, defaultValue);
        public void SetAmmo(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_ammo, value); }
        public string GetAmmoType(string defaultValue = "") => _zdo.GetString(ZDOVars.s_ammoType, defaultValue);
        public void SetAmmoType(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_ammoType, value); }
        public float GetGrowStart(float defaultValue = default) => _zdo.GetFloat(ZDOVars.s_growStart, defaultValue);
        public void SetGrowStart(float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_growStart, value); }
        public DateTime GetSpawnTime(DateTime defaultValue = default) => new(_zdo.GetLong(ZDOVars.s_spawnTime, defaultValue.Ticks));
        public void SetSpawnTime(DateTime value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_spawnTime, value.Ticks); }
        public float GetHealth(float defaultValue = default) => _zdo.GetFloat(ZDOVars.s_health, defaultValue);
        public void SetHealth(float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_health, value); }
        public string GetHealthString(string defaultValue = "") => _zdo.GetString(ZDOVars.s_health, defaultValue);
        public void SetHealth(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_health, value); }
        public void RemoveHealth([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.RemoveFloat(ZDOVars.s_health); }
        public int GetPermitted(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_permitted, defaultValue);
        public void SetPermitted(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_permitted, value); }
        public int GetLevel(int defaultValue = 1) => _zdo.GetInt(ZDOVars.s_level, defaultValue);
        public void SetLevel(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_level, value); }
        public bool GetPatrol(bool defaultValue = default) => _zdo.GetBool(ZDOVars.s_patrol, defaultValue);
        public void SetPatrol(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_patrol, value); }
        public Vector3 GetPatrolPoint(Vector3 defaultValue = default) => _zdo.GetVec3(ZDOVars.s_patrolPoint, defaultValue);
        public void SetPatrolPoint(Vector3 value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_patrolPoint, value); }
        public Vector3 GetSpawnPoint(Vector3 defaultValue = default) => _zdo.GetVec3(ZDOVars.s_spawnPoint, defaultValue);
        public void SetSpawnPoint(Vector3 value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_spawnPoint, value); }
        public int GetEmoteID(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_emoteID, defaultValue);
        public Emotes GetEmote(Emotes defaultValue = ModConfig.PlayersConfig.DisabledEmote) => Enum.TryParse<Emotes>(_zdo.GetString(ZDOVars.s_emote), true, out var e) ? e : defaultValue;
        public bool GetAnimationIsEncumbered(bool defaultValue = default) => _zdo.GetBool(PrivateAccessor.ZSyncAnimationZDOSalt + PrivateAccessor.CharacterAnimationHashEncumbered, defaultValue);
        public bool GetAnimationInWater(bool defaultValue = default) => _zdo.GetBool(PrivateAccessor.ZSyncAnimationZDOSalt + PrivateAccessor.CharacterAnimationHashInWater, defaultValue);
        public bool GetAnimationIsCrouching(bool defaultValue = default) => _zdo.GetBool(PrivateAccessor.ZSyncAnimationZDOSalt + PrivateAccessor.PlayerAnimationHashCrouching, defaultValue);
        public DateTime GetTameLastFeeding(DateTime defaultValue = default) => new(_zdo.GetLong(ZDOVars.s_tameLastFeeding, defaultValue.Ticks));
        public void SetTameLastFeeding(DateTime value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(ZDOVars.s_tameLastFeeding, value.Ticks); }
        public bool GetEventCreature(bool defaultValue = default) => _zdo.GetBool(ZDOVars.s_eventCreature, defaultValue);
        public bool GetInBed(bool defaultValue = default) => _zdo.GetBool(ZDOVars.s_inBed, defaultValue);
        public int GetLocation(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_location, defaultValue);
        public int GetSeed(int defaultValue = default) => _zdo.GetInt(ZDOVars.s_seed, defaultValue);

        static int __processorId = $"{Main.PluginGuid}.ProcessorId".GetStableHashCode();
        public Guid GetProcessorId(Guid defaultValue = default) => _zdo.GetByteArray(__processorId, Array.Empty<byte>()) is { Length: > 0 } arr ? new(arr) : defaultValue;
        public void SetProcessorId(Guid value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__processorId, value == default ? Array.Empty<byte>() : value.ToByteArray()); }

        static int __intTag = $"{Main.PluginGuid}.IntTag".GetStableHashCode();
        public int GetIntTag(int defaultValue = default) => _zdo.GetInt(__intTag, defaultValue);
        public void SetIntTag(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__intTag, value); }
        public void RemoveIntTag([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.RemoveInt(__intTag); }

        static int __lastSpawnedTime = $"{Main.PluginGuid}.LastSpawnedTime".GetStableHashCode();
        public DateTimeOffset GetLastSpawnedTime(DateTimeOffset defaultValue = default) => new(_zdo.GetLong(__lastSpawnedTime, defaultValue.Ticks), default);
        public void SetLastSpawnedTime(DateTimeOffset value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__lastSpawnedTime, value.Ticks - value.Offset.Ticks); }

        static int __spawnedByTrophy = $"{Main.PluginGuid}.SpawnedByTrophy".GetStableHashCode();
        public bool GetSpawnedByTrophy(bool defaultValue = default) => _zdo.GetBool(__spawnedByTrophy, defaultValue);
        public void SetSpawnedByTrophy(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__spawnedByTrophy, value); }

        static int __portalHubId = $"{Main.PluginGuid}.PortalHubId".GetStableHashCode();
        public int GetPortalHubId(int defaultValue = default) => _zdo.GetInt(__portalHubId, defaultValue);
        public void SetPortalHubId(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__portalHubId, value); }

        static int __returnContentToCreator = $"{Main.PluginGuid}.ReturnContentToCreator".GetStableHashCode();
        public bool GetReturnContentToCreator(bool defaultValue = default) => _zdo.GetBool(__returnContentToCreator, defaultValue);
        public void SetReturnContentToCreator(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__returnContentToCreator, value); }

        static int __initialLevel = $"{Main.PluginGuid}.PortalHubId".GetStableHashCode();
        public int GetInitialLevel(int defaultValue = default) => _zdo.GetInt(__initialLevel, defaultValue);
        public void SetInitialLevel(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__initialLevel, value); }
        public void RemoveInitialLevel([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.RemoveInt(__initialLevel); }

        static int __beaconFound = $"{Main.PluginGuid}.BeaconState".GetStableHashCode();
        public bool GetBeaconFound(bool defaultValue = default) => _zdo.GetBool(__beaconFound, defaultValue);
        public void SetBeaconFound(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__beaconFound, value); }

        public bool GetSacrifiedMegingjord(long playerID, bool defaultValue = default) => _zdo.GetBool($"player{playerID}_SacrifiedMegingjord", defaultValue);
        public void SetSacrifiedMegingjord(long playerID, bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set($"player{playerID}_SacrifiedMegingjord", value); }
        public bool GetSacrifiedCryptKey(long playerID, bool defaultValue = default) => _zdo.GetBool($"player{playerID}_SacrifiedCryptKey", defaultValue);
        public void SetSacrifiedCryptKey(long playerID, bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set($"player{playerID}_SacrifiedCryptKey", value); }
        public bool GetSacrifiedWishbone(long playerID, bool defaultValue = default) => _zdo.GetBool($"player{playerID}_SacrifiedWishbone", defaultValue);
        public void SetSacrifiedWishbone(long playerID, bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set($"player{playerID}_SacrifiedWishbone", value); }
        public bool GetSacrifiedTornSpirit(long playerID, bool defaultValue = default) => _zdo.GetBool($"player{playerID}_SacrifiedTornSpirit", defaultValue);
        public void SetSacrifiedTornSpirit(long playerID, bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set($"player{playerID}_SacrifiedTornSpirit", value); }
        //public float GetEstimatedSkillLevel(long playerID, Skills.SkillType skill, float defaultValue = default) => _zdo.GetFloat($"player{playerID}_EstimatedSkillLevel_{skill}", defaultValue);
        //public void SetEstimatedSkillLevel(long playerID, Skills.SkillType skill, float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set($"player{playerID}_EstimatedSkillLevel_{skill}", value); }
    }

    sealed class AdditionalData_(PrefabInfo prefabInfo)
    {
        public bool HasProcessors { get; private set; } = true;
        public IReadOnlyList<Processor> Processors
        {
            get => field ??= Processor.DefaultProcessors;
            private set { field = value; HasProcessors = value.Count > 0; }
        }

        public PrefabInfo PrefabInfo { get; } = prefabInfo;
        public ConcurrentDictionary<Type, object>? ComponentFieldAccessors { get; set; }
        public Dictionary<Processor, (uint Data, uint Owner)>? ProcessorDataRevisions { get; set; }
        public ZDOInventory? Inventory { get; set; }
        public bool? HasFields { get; set; }
        public RecreateHandler? Recreated { get; set; }
        public Action<ExtendedZDO>? Destroyed { get; set; }

        static readonly Dictionary<int, IReadOnlyList<Processor>> _processors = [];

        public void Ungregister(IReadOnlyList<Processor> processors)
        {
            var hash = 0;
            foreach (var processor in Processors.AsEnumerable())
            {
                var keep = true;
                foreach (var remove in processors.AsEnumerable())
                {
                    if (ReferenceEquals(processor, remove))
                    {
                        keep = false;
                        break;
                    }
                }
                if (keep)
                    hash = (hash, processor.GetType()).GetHashCode();
            }

            if (!_processors.TryGetValue(hash, out var newProcessors))
            {
                var list = new List<Processor>();
                _processors.Add(hash, newProcessors = list);
                foreach (var processor in Processors.AsEnumerable())
                {
                    var keep = true;
                    foreach (var remove in processors.AsEnumerable())
                    {
                        if (ReferenceEquals(processor, remove))
                        {
                            keep = false;
                            break;
                        }
                    }
                    if (keep)
                        list.Add(processor);
                }
            }

            Processors = newProcessors;

            if (ProcessorDataRevisions is not null)
            {
                foreach (var processor in processors.AsEnumerable())
                    ProcessorDataRevisions.Remove(processor);
            }
        }

        public void UnregisterAllExcept(Processor keep)
        {
            var hash = (0, keep.GetType()).GetHashCode();
            if (!_processors.TryGetValue(hash, out var processors))
                _processors.Add(hash, processors = [keep]);
            if (ProcessorDataRevisions is not null)
            {
                foreach (var processor in Processors.AsEnumerable())
                {
                    if (!ReferenceEquals(processor, keep))
                        ProcessorDataRevisions.Remove(processor);
                }
            }
            Processors = processors;
            return;
        }

        public void UnregisterAll() => Processors = [];
        public void ReregisterAll() => Processors = Processor.DefaultProcessors;

        public static AdditionalData_ Dummy { get; } = new(PrefabInfo.Dummy);
    }

    sealed class UnityObjectEqualityComparer<T> : EqualityComparer<T>
        where T : UnityEngine.Object
    {
        public static UnityObjectEqualityComparer<T> Instance { get; } = new();
        public override bool Equals(T x, T y) => x?.name == y?.name;
        public override int GetHashCode(T obj) => obj.name.GetHashCode();
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

        static class ExpressionCache<T> where T : notnull
        {
            static readonly Dictionary<(string, int), Expression<Func<TComponent, T>>> __cache = [];

            public static Expression<Func<TComponent, T>> Get(Func<Expression<Func<TComponent, T>>> factory, string callerFilePath, int callerLineNo)
            {
                if (!__cache.TryGetValue((callerFilePath, callerLineNo), out var result))
                    __cache.Add((callerFilePath, callerLineNo), result = factory());
                return result;
            }
        }

        delegate T GetHandler<T>(ZDO zdo, int hash, T defaultValue) where T : notnull;
        delegate void SetHandler<T>(ZDO zdo, int hash, T value) where T : notnull;
        delegate bool RemoveHandler<T>(ZDO zdo, int hash) where T : notnull;

        sealed class FieldReference<T> where T : notnull
        {
            //readonly Expression<Func<TComponent, T>> _fieldExpression;
            readonly int _hash;
            readonly Func<TComponent, T> _getFieldValue;
            static readonly Dictionary<string, FieldReference<T>> __cacheByFieldName = [];
            static readonly Dictionary<(string, int), FieldReference<T>> __cacheByLocation = [];

            static readonly (GetHandler<T> Getter, SetHandler<T> Setter, RemoveHandler<T>? Remover, IEqualityComparer<T> EqualityComparer) Accessors =
                new Func<(GetHandler<T>, SetHandler<T>, RemoveHandler<T>?, IEqualityComparer<T>)>(static () =>
            {
                if (typeof(T) == typeof(bool)) return (
                    (GetHandler<T>)(Delegate)new GetHandler<bool>(static (ZDO zdo, int hash, bool defaultValue) => zdo.GetBool(hash, defaultValue)),
                    (SetHandler<T>)(Delegate)new SetHandler<bool>(static (ZDO zdo, int hash, bool value) => zdo.Set(hash, value)),
                    (RemoveHandler<T>)(Delegate)new RemoveHandler<bool>(static (ZDO zdo, int hash) => zdo.RemoveInt(hash)),
                    (IEqualityComparer<T>)EqualityComparer<bool>.Default);

                if (typeof(T) == typeof(int)) return (
                    (GetHandler<T>)(Delegate)new GetHandler<int>(static (ZDO zdo, int hash, int defaultValue) => zdo.GetInt(hash, defaultValue)),
                    (SetHandler<T>)(Delegate)new SetHandler<int>(static (ZDO zdo, int hash, int value) => zdo.Set(hash, value)),
                    (RemoveHandler<T>)(Delegate)new RemoveHandler<int>(static (ZDO zdo, int hash) => zdo.RemoveInt(hash)),
                    (IEqualityComparer<T>)EqualityComparer<int>.Default);

                if (typeof(T) == typeof(float)) return (
                    (GetHandler<T>)(Delegate)new GetHandler<float>(static (ZDO zdo, int hash, float defaultValue) => zdo.GetFloat(hash, defaultValue)),
                    (SetHandler<T>)(Delegate)new SetHandler<float>(static (ZDO zdo, int hash, float value) => zdo.Set(hash, value)),
                    (RemoveHandler<T>)(Delegate)new RemoveHandler<float>(static (ZDO zdo, int hash) => zdo.RemoveFloat(hash)),
                    (IEqualityComparer<T>)EqualityComparer<float>.Default);

                if (typeof(T) == typeof(string)) return (
                    (GetHandler<T>)(Delegate)new GetHandler<string>(static (ZDO zdo, int hash, string defaultValue) => zdo.GetString(hash, defaultValue)),
                    (SetHandler<T>)(Delegate)new SetHandler<string>(static (ZDO zdo, int hash, string value) => zdo.Set(hash, value)),
                    null,
                    (IEqualityComparer<T>)EqualityComparer<string>.Default);

                if (typeof(T) == typeof(Vector3)) return (
                    (GetHandler<T>)(Delegate)new GetHandler<Vector3>(static (ZDO zdo, int hash, Vector3 defaultValue) => zdo.GetVec3(hash, defaultValue)),
                    (SetHandler<T>)(Delegate)new SetHandler<Vector3>(static (ZDO zdo, int hash, Vector3 value) => zdo.Set(hash, value)),
                    (RemoveHandler<T>)(Delegate)new RemoveHandler<Vector3>(static (ZDO zdo, int hash) => zdo.RemoveVec3(hash)),
                    (IEqualityComparer<T>)EqualityComparer<Vector3>.Default);

                if (typeof(T) == typeof(GameObject)) return (
                    (GetHandler<T>)(Delegate)new GetHandler<GameObject>(GetGameObject),
                    (SetHandler<T>)(Delegate)new SetHandler<GameObject>(static (ZDO zdo, int hash, GameObject value) => zdo.Set(hash, value.name)),
                    null,
                    (IEqualityComparer<T>)(object)UnityObjectEqualityComparer<GameObject>.Instance);

                if (typeof(T) == typeof(ItemDrop)) return (
                    (GetHandler<T>)(Delegate)new GetHandler<ItemDrop>(GetItemDrop),
                    (SetHandler<T>)(Delegate)new SetHandler<ItemDrop>(static (ZDO zdo, int hash, ItemDrop value) => zdo.Set(hash, value.name)),
                    null,
                    (IEqualityComparer<T>)(object)UnityObjectEqualityComparer<ItemDrop>.Instance);

                throw new NotSupportedException();

                static GameObject GetGameObject(ZDO zdo, int hash, GameObject defaultValue)
                {
                    var name = zdo.GetString(hash);
                    if (string.IsNullOrEmpty(name))
                        return defaultValue;
                    return ZNetScene.instance.GetPrefab(name) ?? defaultValue;
                }

                static ItemDrop GetItemDrop(ZDO zdo, int hash, ItemDrop defaultValue)
                {
                    var name = zdo.GetString(hash);
                    if (string.IsNullOrEmpty(name))
                        return defaultValue;
                    return ZNetScene.instance.GetPrefab(name)?.GetComponent<ItemDrop>() ?? defaultValue;
                }
            }).Invoke();

            FieldReference(FieldInfo field)
            {
#if DEBUG
                if (field.FieldType != typeof(T))
                    throw new Exception($"Field type {typeof(T).Name} expected, actual field type is {field.FieldType.Name}");
#endif
                _hash = Invariant($"{typeof(TComponent).Name}.{field.Name}").GetStableHashCode();

                var par = Expression.Parameter(typeof(TComponent));
                _getFieldValue = Expression.Lambda<Func<TComponent, T>>(Expression.Field(par, field), par).Compile();
            }

            public static FieldReference<T> Get(Func<Expression<Func<TComponent, T>>> factory, string callerFilePath, int callerLineNo)
            {
                if (!__cacheByLocation.TryGetValue((callerFilePath, callerLineNo), out var result))
                {
                    var expression = ExpressionCache<T>.Get(factory, callerFilePath, callerLineNo);
                    var body = (MemberExpression)expression.Body;
                    var field = (FieldInfo)body.Member;
                    if (!__cacheByFieldName.TryGetValue(field.Name, out result))
                        __cacheByFieldName.Add(field.Name, result = new(field));
                    __cacheByLocation.Add((callerFilePath, callerLineNo), result);
                }
                return result;
            }

            public T GetValue(ComponentFieldAccessor<TComponent> componentFieldAccessor)
            {
                var defaultValue = _getFieldValue(componentFieldAccessor._component);
                if (!componentFieldAccessor.HasFields)
                    return defaultValue;
                return Accessors.Getter(componentFieldAccessor._zdo, _hash, defaultValue);
            }

            public ComponentFieldAccessor<TComponent> SetValue(ComponentFieldAccessor<TComponent> componentFieldAccessor, T value)
            {
                if (Accessors.Remover is not null && Accessors.EqualityComparer.Equals(value, _getFieldValue(componentFieldAccessor._component)))
                    Accessors.Remover(componentFieldAccessor._zdo, _hash);
                else
                {
                    if (!componentFieldAccessor.HasFields)
                        componentFieldAccessor.SetHasFields(true);
                    Accessors.Setter(componentFieldAccessor._zdo, _hash, value);
                }
                return componentFieldAccessor;
            }

            public bool UpdateValue(ComponentFieldAccessor<TComponent> componentFieldAccessor, T value)
            {
                var defaultValue = _getFieldValue(componentFieldAccessor._component);
                if (Accessors.EqualityComparer.Equals(value, Accessors.Getter(componentFieldAccessor._zdo, _hash, defaultValue)))
                    return false;

                var isDefaultValue = Accessors.EqualityComparer.Equals(value, defaultValue);

                if (Accessors.Remover is not null && isDefaultValue)
                    Accessors.Remover(componentFieldAccessor._zdo, _hash);
                else
                {
                    if (!componentFieldAccessor.HasFields && !isDefaultValue)
                        componentFieldAccessor.SetHasFields(true);
                    Accessors.Setter(componentFieldAccessor._zdo, _hash, value);
                }
                return true;
            }

            public ComponentFieldAccessor<TComponent> ResetValue(ComponentFieldAccessor<TComponent> componentFieldAccessor)
            {
                if (!componentFieldAccessor.HasFields)
                    return componentFieldAccessor;

                if (Accessors.Remover is not null)
                    Accessors.Remover(componentFieldAccessor._zdo, _hash);
                else
                    Accessors.Setter(componentFieldAccessor._zdo, _hash, _getFieldValue(componentFieldAccessor._component));
                return componentFieldAccessor;
            }

            public bool UpdateResetValue(ComponentFieldAccessor<TComponent> componentFieldAccessor)
            {
                if (!componentFieldAccessor.HasFields)
                    return false;

                if (Accessors.Remover is not null)
                    return Accessors.Remover(componentFieldAccessor._zdo, _hash);

                var defaultValue = _getFieldValue(componentFieldAccessor._component);
                if (Accessors.EqualityComparer.Equals(Accessors.Getter(componentFieldAccessor._zdo, _hash, defaultValue), defaultValue))
                    return false;
                Accessors.Setter(componentFieldAccessor._zdo, _hash, defaultValue);
                return true;
            }
        }

        [MustBeOnUniqueLine]
        public bool GetBool(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<bool>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).GetValue(this);

        [MustBeOnUniqueLine]
        public float GetFloat(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<float>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).GetValue(this);

        [MustBeOnUniqueLine]
        public int GetInt(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<int>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).GetValue(this);

        [MustBeOnUniqueLine]
        public string GetString(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<string>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).GetValue(this);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, bool value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<bool>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, float value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<float>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, int value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<int>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, Vector3>>> fieldExpressionFactory, Vector3 value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<Vector3>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, string value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<string>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, GameObject>>> fieldExpressionFactory, GameObject value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<GameObject>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, ItemDrop>>> fieldExpressionFactory, ItemDrop value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<ItemDrop>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

        [MustBeOnUniqueLine]
        public bool SetIfChanged(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, bool value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<bool>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

        [MustBeOnUniqueLine]
        public bool SetIfChanged(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, float value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<float>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

        [MustBeOnUniqueLine]
        public bool SetIfChanged(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, int value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<int>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

        [MustBeOnUniqueLine]
        public bool SetIfChanged(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, string value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<string>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

        [MustBeOnUniqueLine]
        public bool SetIfChanged(Func<Expression<Func<TComponent, GameObject>>> fieldExpressionFactory, GameObject value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<GameObject>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

        [MustBeOnUniqueLine]
        public bool SetIfChanged(Func<Expression<Func<TComponent, ItemDrop>>> fieldExpressionFactory, ItemDrop value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<ItemDrop>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<bool>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<float>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<int>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<string>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, GameObject>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<GameObject>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);

        [MustBeOnUniqueLine]
        public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, ItemDrop>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<ItemDrop>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);


        [MustBeOnUniqueLine]
        public bool ResetIfChanged(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<bool>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

        [MustBeOnUniqueLine]
        public bool ResetIfChanged(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<float>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

        [MustBeOnUniqueLine]
        public bool ResetIfChanged(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<int>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

        [MustBeOnUniqueLine]
        public bool ResetIfChanged(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<string>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

        [MustBeOnUniqueLine]
        public bool ResetIfChanged(Func<Expression<Func<TComponent, GameObject>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<GameObject>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

        [MustBeOnUniqueLine]
        public bool ResetIfChanged(Func<Expression<Func<TComponent, ItemDrop>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => FieldReference<ItemDrop>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

        [MustBeOnUniqueLine]
        public bool SetOrReset(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, bool set, bool setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => set ? FieldReference<bool>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<bool>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

        [MustBeOnUniqueLine]
        public bool SetOrReset(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, bool set, float setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => set ? FieldReference<float>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<float>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

        [MustBeOnUniqueLine]
        public bool SetOrReset(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, bool set, int setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => set ? FieldReference<int>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<int>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

        [MustBeOnUniqueLine]
        public bool SetOrReset(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, bool set, string setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => set ? FieldReference<string>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<string>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

        [MustBeOnUniqueLine]
        public bool SetOrReset(Func<Expression<Func<TComponent, GameObject>>> fieldExpressionFactory, bool set, GameObject setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => set ? FieldReference<GameObject>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<GameObject>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

        [MustBeOnUniqueLine]
        public bool SetOrReset(Func<Expression<Func<TComponent, ItemDrop>>> fieldExpressionFactory, bool set, ItemDrop setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
            => set ? FieldReference<ItemDrop>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<ItemDrop>.Get(fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);
    }

    sealed class ZDOInventory(ExtendedZDO zdo) : IZDOInventory, IZDOInventoryReadOnly
    {
        public Inventory Inventory { get; private set; } = default!;
        public ExtendedZDO ZDO { get; private set; } = zdo;
        public int? PickupRange { get; set; }
        public int? FeedRange { get; set; }

        List<ItemDrop.ItemData>? _items;
        uint _dataRevision = uint.MaxValue;
        string? _lastData;

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
            if (_lastData == data)
                return this;

            var fields = ZDO.Fields<Container>();
            var w = fields.GetInt(static () => x => x.m_width);
            var h = fields.GetInt(static () => x => x.m_height);
            if (Inventory is null || Inventory.GetWidth() != w || Inventory.GetHeight() != h)
            {
                Inventory = new(ZDO.PrefabInfo.Container!.Value.Container.m_name, ZDO.PrefabInfo.Container!.Value.Container.m_bkg, w, h);
                _items = null;
            }

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
                if (ZDO.PrefabInfo.Container is { ZSyncTransform.Value: not null })
                    ZDO.DataRevision += 120;

                ZDOMan.instance.ForceSendZDO(ZDO.m_uid);
            }

            _dataRevision = ZDO.DataRevision;
            _lastData = data;
        }
    }
}
