using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Valheim.ZDOExtender;

namespace Valheim.ServersideQoL;

public static class ZDOExtensions
{
    static readonly Dictionary<int, IReadOnlyList<Processor>> _processors = [];

    extension(ZDO @this)
    {
        public ZDOVars Vars => new(@this);

        public IReadOnlyList<Processor> GetProcessors()
                    {
            var extZdo = @this.GetExtension<IZDOWithProcessors>();
            if (extZdo.Processors is not { } processors)
            {
                extZdo.Processors = processors = ServersideQoL.Processors;
                extZdo.HasNoProcessors = processors.Count is 0;
            }
            return extZdo.Processors;
        }

        public void Ungregister(IReadOnlyList<Processor> processors)
        {
            var extZdo = @this.GetExtension<IZDOWithProcessors>();
            var zdoProcessors = extZdo.Processors;
            var hash = 0;
            foreach (var processor in zdoProcessors.AsEnumerable())
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
                foreach (var processor in zdoProcessors.AsEnumerable())
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

            extZdo.Processors = newProcessors;
            extZdo.HasNoProcessors = newProcessors.Count is 0;

            if (extZdo.ProcessorDataRevisions is { } dataRevisions)
            {
                foreach (var processor in processors.AsEnumerable())
                    dataRevisions.Remove(processor);
            }
        }

        public void UnregisterAllExcept(Processor keep)
        {
            var extZdo = @this.GetExtension<IZDOWithProcessors>();
            var zdoProcessors = extZdo.Processors;
            var hash = (0, keep.GetType()).GetHashCode();
            if (!_processors.TryGetValue(hash, out var processors))
                _processors.Add(hash, processors = [keep]);
            if (extZdo.ProcessorDataRevisions is { } dataRevisions)
            {
                foreach (var processor in zdoProcessors.AsEnumerable())
                {
                    if (!ReferenceEquals(processor, keep))
                        dataRevisions.Remove(processor);
                }
            }
            extZdo.Processors = processors;
            extZdo.HasNoProcessors = processors.Count is 0;
            return;
        }

        public void UnregisterAll()
        {
            var extZdo = @this.GetExtension<IZDOWithProcessors>();
            extZdo.Processors = [];
            extZdo.HasNoProcessors = true;
        }

        public void ReregisterAll()
        {
            var extZdo = @this.GetExtension<IZDOWithProcessors>();
            var processors = ServersideQoL.Processors;
            extZdo.Processors = processors;
            extZdo.HasNoProcessors = processors.Count is 0;
        }

        public void Reregister(IReadOnlyList<Processor> processors)
        {
            throw new NotImplementedException();
        }


        public void UpdateProcessorDataRevision(Processor processor)
            => (@this.GetExtension<IZDOWithProcessors>().ProcessorDataRevisions ??= [])[processor] = (@this.DataRevision, @this.OwnerRevision);

        public void ResetProcessorDataRevision(Processor processor)
            => @this.GetExtension<IZDOWithProcessors>().ProcessorDataRevisions?.Remove(processor);

        public bool CheckProcessorDataRevisionChanged(Processor processor)
        {
            var dataRevisions = @this.GetExtension<IZDOWithProcessors>().ProcessorDataRevisions;
            if (dataRevisions is null || !dataRevisions.TryGetValue(processor, out var revision) || revision != (@this.DataRevision, @this.OwnerRevision))
                return true;
            return false;
        }

        public void Destroy()
        {
            @this.ClaimOwnershipInternal();
            ZDOMan.instance.DestroyZDO(@this);
        }

        public ZDO CreateClone()
        {
            var prefab = @this.GetPrefab();
            var pos = @this.GetPosition();
            var owner = @this.GetOwner();
            var pkg = new ZPackage();
            @this.Serialize(pkg);

            var zdo = ZDOMan.instance.CreateNewZDO(pos, prefab);
            zdo.Deserialize(new(pkg.GetArray()));
            zdo.SetOwnerInternal(owner);
            return zdo;
        }

        public ZDO Recreate()
        {
            var zdo = @this.CreateClone();

            // Call before Destroy and thus before ZDOMan.instance.m_onZDODestroyed
            //_addData?.Recreated?.Invoke(this, zdo);

            @this.Destroy();
            return zdo;
        }

        public void ClaimOwnership() => @this.SetOwner(ZDOMan.GetSessionID());
        public void ClaimOwnershipInternal() => @this.SetOwnerInternal(ZDOMan.GetSessionID());
        public void ReleaseOwnership() => @this.SetOwner(0);
        public void ReleaseOwnershipInternal() => @this.SetOwnerInternal(0);

        public bool IsOwnerOrUnassigned() => !@this.HasOwner() || @this.IsOwner();
    }

    public readonly struct ZDOVars(ZDO zdo)
    {
        readonly ZDO _zdo = zdo;

        void ValidateOwnership(string filePath, int lineNo)
        {
#if !DEBUG
            if (!ServersideQoL.Instance.Config.General.DiagnosticLogs.Value)
                return;
#endif
            //if (_zdo.PrefabInfo.Container is null || _zdo.IsOwnerOrUnassigned() || _zdo.IsModCreator())
            //    return;

            //ServersideQoL.Instance.Logger.LogWarning($"{Path.GetFileName(filePath)} L{lineNo}: Container was modified while it is owned by a client, which can lead to the loss of items.");
        }

        public int GetState(int defaultValue = default) => _zdo.GetInt(global::ZDOVars.s_state, defaultValue);
        public void SetState(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_state, value); }
        public long GetCreator(long defaultValue = default) => _zdo.GetLong(global::ZDOVars.s_creator, defaultValue);
        public void SetCreator(long value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_creator, value); }
        public bool GetInUse(bool defaultValue = default) => _zdo.GetBool(global::ZDOVars.s_inUse, defaultValue);
        public void SetInUse(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_inUse, value); }
        public float GetFuel(float defaultValue = default) => _zdo.GetFloat(global::ZDOVars.s_fuel, defaultValue);
        public void SetFuel(float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_fuel, value); }
        public bool GetPiece(bool defaultValue = default) => _zdo.GetBool(global::ZDOVars.s_piece, defaultValue);
        public void SetPiece(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_piece, value); }
        public string GetItems(string defaultValue = "") => _zdo.GetString(global::ZDOVars.s_items, defaultValue);
        public void SetItems(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_items, value); }
        public string GetTag(string defaultValue = "") => _zdo.GetString(global::ZDOVars.s_tag, defaultValue);
        public void SetTag(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_tag, value); }
        public byte[]? GetData(byte[]? defaultValue = null) => _zdo.GetByteArray(global::ZDOVars.s_data, defaultValue);
        public void SetData(byte[]? value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_data, value); }
        public float GetStamina(float defaultValue = default) => _zdo.GetFloat(global::ZDOVars.s_stamina, defaultValue);
        public float GetEitr(float defaultValue = default) => _zdo.GetFloat(global::ZDOVars.s_eitr, defaultValue);
        public long GetPlayerID(long defaultValue = default) => _zdo.GetLong(global::ZDOVars.s_playerID, defaultValue);
        public void SetPlayerID(long value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_playerID, value); }
        public string GetPlayerName(string defaultValue = "") => _zdo.GetString(global::ZDOVars.s_playerName, defaultValue);
        public void SetPlayerName(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_playerName, value); }
        public string GetFollow(string defaultValue = "") => _zdo.GetString(global::ZDOVars.s_follow, defaultValue);
        public void SetFollow(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_follow, value); }
        public int GetRightItem(int defaultValue = default) => _zdo.GetInt(global::ZDOVars.s_rightItem, defaultValue);
        public int GetLeftItem(int defaultValue = default) => _zdo.GetInt(global::ZDOVars.s_leftItem, defaultValue);
        public string GetText(string defaultValue = "") => _zdo.GetString(global::ZDOVars.s_text, defaultValue);
        public void SetText(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_text, value); }
        public string GetItem(string defaultValue = "") => _zdo.GetString(global::ZDOVars.s_item, defaultValue);
        public void SetItem(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_item, value); }
        public string GetItem(int idx, string defaultValue = "") => _zdo.GetString(Invariant($"item{idx}"), defaultValue);
        public void SetItem(int idx, string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(Invariant($"item{idx}"), value); }
        public int GetQueued(int defaultValue = default) => _zdo.GetInt(global::ZDOVars.s_queued, defaultValue);
        public void SetQueued(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_queued, value); }
        public bool GetTamed(bool defaultValue = default) => _zdo.GetBool(global::ZDOVars.s_tamed, defaultValue);
        public void SetTamed(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_tamed, value); }
        public float GetTameTimeLeft(float defaultValue = default) => _zdo.GetFloat(global::ZDOVars.s_tameTimeLeft, defaultValue);
        public void SetTameTimeLeft(float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_tameTimeLeft, value); }
        public int GetAmmo(int defaultValue = default) => _zdo.GetInt(global::ZDOVars.s_ammo, defaultValue);
        public void SetAmmo(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_ammo, value); }
        public string GetAmmoType(string defaultValue = "") => _zdo.GetString(global::ZDOVars.s_ammoType, defaultValue);
        public void SetAmmoType(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_ammoType, value); }
        public float GetGrowStart(float defaultValue = default) => _zdo.GetFloat(global::ZDOVars.s_growStart, defaultValue);
        public void SetGrowStart(float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_growStart, value); }
        public DateTime GetSpawnTime(DateTime defaultValue = default) => new(_zdo.GetLong(global::ZDOVars.s_spawnTime, defaultValue.Ticks));
        public void SetSpawnTime(DateTime value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_spawnTime, value.Ticks); }
        public float GetHealth(float defaultValue = default) => _zdo.GetFloat(global::ZDOVars.s_health, defaultValue);
        public void SetHealth(float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_health, value); }
        public string GetHealthString(string defaultValue = "") => _zdo.GetString(global::ZDOVars.s_health, defaultValue);
        public void SetHealth(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_health, value); }
        public void RemoveHealth([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.RemoveFloat(global::ZDOVars.s_health); }
        public int GetPermitted(int defaultValue = default) => _zdo.GetInt(global::ZDOVars.s_permitted, defaultValue);
        public void SetPermitted(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_permitted, value); }
        public int GetLevel(int defaultValue = 1) => _zdo.GetInt(global::ZDOVars.s_level, defaultValue);
        public void SetLevel(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_level, value); }
        public bool GetPatrol(bool defaultValue = default) => _zdo.GetBool(global::ZDOVars.s_patrol, defaultValue);
        public void SetPatrol(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_patrol, value); }
        public Vector3 GetPatrolPoint(Vector3 defaultValue = default) => _zdo.GetVec3(global::ZDOVars.s_patrolPoint, defaultValue);
        public void SetPatrolPoint(Vector3 value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_patrolPoint, value); }
        public Vector3 GetSpawnPoint(Vector3 defaultValue = default) => _zdo.GetVec3(global::ZDOVars.s_spawnPoint, defaultValue);
        public void SetSpawnPoint(Vector3 value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_spawnPoint, value); }
        public int GetEmoteID(int defaultValue = default) => _zdo.GetInt(global::ZDOVars.s_emoteID, defaultValue);
        //public Emotes GetEmote(Emotes defaultValue = ModConfigBase.PlayersConfig.DisabledEmote) => Enum.TryParse<Emotes>(_zdo.GetString(global::ZDOVars.s_emote), true, out var e) ? e : defaultValue;
        //public bool GetAnimationIsEncumbered(bool defaultValue = default) => _zdo.GetBool(PrivateAccessor.ZSyncAnimationZDOSalt + PrivateAccessor.CharacterAnimationHashEncumbered, defaultValue);
        //public bool GetAnimationInWater(bool defaultValue = default) => _zdo.GetBool(PrivateAccessor.ZSyncAnimationZDOSalt + PrivateAccessor.CharacterAnimationHashInWater, defaultValue);
        //public bool GetAnimationIsCrouching(bool defaultValue = default) => _zdo.GetBool(PrivateAccessor.ZSyncAnimationZDOSalt + PrivateAccessor.PlayerAnimationHashCrouching, defaultValue);
        public DateTime GetTameLastFeeding(DateTime defaultValue = default) => new(_zdo.GetLong(global::ZDOVars.s_tameLastFeeding, defaultValue.Ticks));
        public void SetTameLastFeeding(DateTime value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_tameLastFeeding, value.Ticks); }
        public bool GetEventCreature(bool defaultValue = default) => _zdo.GetBool(global::ZDOVars.s_eventCreature, defaultValue);
        public bool GetInBed(bool defaultValue = default) => _zdo.GetBool(global::ZDOVars.s_inBed, defaultValue);
        public int GetLocation(int defaultValue = default) => _zdo.GetInt(global::ZDOVars.s_location, defaultValue);
        public int GetSeed(int defaultValue = default) => _zdo.GetInt(global::ZDOVars.s_seed, defaultValue);

        static int __processorId = $"{ServersideQoL.PluginGuid}.ProcessorId".GetStableHashCode();
        public Guid GetProcessorId(Guid defaultValue = default) => _zdo.GetByteArray(__processorId, Array.Empty<byte>()) is { Length: > 0 } arr ? new(arr) : defaultValue;
        public void SetProcessorId(Guid value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__processorId, value == default ? Array.Empty<byte>() : value.ToByteArray()); }

        static int __intTag = $"{ServersideQoL.PluginGuid}.IntTag".GetStableHashCode();
        public int GetIntTag(int defaultValue = default) => _zdo.GetInt(__intTag, defaultValue);
        public void SetIntTag(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__intTag, value); }
        public void RemoveIntTag([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.RemoveInt(__intTag); }

        static int __lastSpawnedTime = $"{ServersideQoL.PluginGuid}.LastSpawnedTime".GetStableHashCode();
        public DateTimeOffset GetLastSpawnedTime(DateTimeOffset defaultValue = default) => new(_zdo.GetLong(__lastSpawnedTime, defaultValue.Ticks), default);
        public void SetLastSpawnedTime(DateTimeOffset value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__lastSpawnedTime, value.Ticks - value.Offset.Ticks); }

        static int __spawnedByTrophy = $"{ServersideQoL.PluginGuid}.SpawnedByTrophy".GetStableHashCode();
        public bool GetSpawnedByTrophy(bool defaultValue = default) => _zdo.GetBool(__spawnedByTrophy, defaultValue);
        public void SetSpawnedByTrophy(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__spawnedByTrophy, value); }

        static int __portalHubId = $"{ServersideQoL.PluginGuid}.PortalHubId".GetStableHashCode();
        public int GetPortalHubId(int defaultValue = default) => _zdo.GetInt(__portalHubId, defaultValue);
        public void SetPortalHubId(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__portalHubId, value); }

        static int __returnContentToCreator = $"{ServersideQoL.PluginGuid}.ReturnContentToCreator".GetStableHashCode();
        public bool GetReturnContentToCreator(bool defaultValue = default) => _zdo.GetBool(__returnContentToCreator, defaultValue);
        public void SetReturnContentToCreator(bool value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__returnContentToCreator, value); }

        static int __initialLevel = $"{ServersideQoL.PluginGuid}.InitialLevel".GetStableHashCode();
        public int GetInitialLevel(int defaultValue = default) => _zdo.GetInt(__initialLevel, defaultValue);
        public void SetInitialLevel(int value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(__initialLevel, value); }
        public void RemoveInitialLevel([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.RemoveInt(__initialLevel); }

        static int __beaconFound = $"{ServersideQoL.PluginGuid}.BeaconState".GetStableHashCode();
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
        public float GetEstimatedSkillLevel(long playerID, Skills.SkillType skill, float defaultValue = default) => _zdo.GetFloat($"player{playerID}_EstimatedSkillLevel_{skill}", defaultValue);
        public void SetEstimatedSkillLevel(long playerID, Skills.SkillType skill, float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set($"player{playerID}_EstimatedSkillLevel_{skill}", value); }

#if DEBUG
        static readonly IReadOnlyDictionary<int, string> __namesByHash = new Func<IReadOnlyDictionary<int, string>>(static () =>
        {
            var result = new Dictionary<int, string>();
            foreach (var (hash, name) in typeof(ZDOVars).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Concat(typeof(ZDOVars).GetFields(BindingFlags.NonPublic | BindingFlags.Static))
                .Where(static x => x.FieldType == typeof(int))
                .Select(static x => ((int)x.GetValue(null), x.Name))
                .Append((ZNetView.CustomFieldsStr.GetStableHashCode(), ZNetView.CustomFieldsStr))
                .Concat(ZNetScene.instance.m_prefabs.SelectMany(static x => x.GetComponent<ZNetView>()?.GetComponentsInChildren<MonoBehaviour>().Select(static x => x.GetType()) ?? []).Distinct()
                    .SelectMany(static x => x.GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Select(static x => $"{x.ReflectedType.Name}.{x.Name}")
                        .Prepend($"{ZNetView.CustomFieldsStr}{x.Name}"))
                    .Select(static x => (x.GetStableHashCode(), x))))
            {
                if (result.TryGetValue(hash, out var existing))
                    ServersideQoL.Logger.DevLog($"Duplicate hash: {existing}, {name} (hash = {hash})");
                else
                    result.Add(hash, name);
            }
            return result;
        }).Invoke();

        static string GetName(int hash) => __namesByHash.TryGetValue(hash, out var name) ? name : $"Unkown ({hash})";

        public string ToDebugString()
        {
            var ints = ZDOExtraData.GetInts(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
            var longs = ZDOExtraData.GetLongs(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
            var floats = ZDOExtraData.GetFloats(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
            var quats = ZDOExtraData.GetQuaternions(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
            var strings = ZDOExtraData.GetStrings(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
            var vecs = ZDOExtraData.GetVec3s(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
            var byteArrays = ZDOExtraData.GetByteArrays(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
            return $"""
                ints ({ints.Count}):{string.Join($"{Environment.NewLine}  ", ints.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
                longs ({longs.Count}):{string.Join($"{Environment.NewLine}  ", longs.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
                floats ({floats.Count}):{string.Join($"{Environment.NewLine}  ", floats.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
                quats ({quats.Count}):{string.Join($"{Environment.NewLine}  ", quats.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
                strings ({strings.Count}):{string.Join($"{Environment.NewLine}  ", strings.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
                vecs ({vecs.Count}):{string.Join($"{Environment.NewLine}  ", vecs.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
                byte arrays ({byteArrays.Count}):{string.Join($"{Environment.NewLine}  ", byteArrays.Select(static x => $"{x.Name}: Length={x.Value.Length}").Prepend(""))}
                """;
        }
#endif
    }
}
