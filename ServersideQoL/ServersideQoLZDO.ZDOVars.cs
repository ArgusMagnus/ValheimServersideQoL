using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ServersideQoL;

partial class ServersideQoLZDO
{
  public readonly struct ZDOVars(ZDO zdo)
  {
    readonly ZDO _zdo = zdo;

    void ValidateOwnership(string filePath, int lineNo)
    {
#if !DEBUG
      if (!ServersideQoLPlugin.Instance.Config.DiagnosticLogs.Value)
        return;
#endif
      //if (_zdo.PrefabInfo.Container is null || _zdo.IsOwnerOrUnassigned() || _zdo.IsModCreator())
      //    return;

      //Main.Instance.Logger.LogWarning($"{Path.GetFileName(filePath)} L{lineNo}: Container was modified while it is owned by a client, which can lead to the loss of items.");
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
    public byte[]? GetItems(byte[]? defaultValue = null) => _zdo.GetByteArray(global::ZDOVars.s_items, defaultValue);
    public void SetItems(byte[]? value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_items, value); }
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
    public Emotes GetEmote(Emotes defaultValue = ConfigBase.DisabledEmote) => Enum.TryParse<Emotes>(_zdo.GetString(global::ZDOVars.s_emote), true, out var e) ? e : defaultValue;
    public bool GetAnimationIsEncumbered(bool defaultValue = default) => _zdo.GetBool(PrivateAccessor.ZSyncAnimationZDOSalt + PrivateAccessor.CharacterAnimationHashEncumbered, defaultValue);
    public bool GetAnimationInWater(bool defaultValue = default) => _zdo.GetBool(PrivateAccessor.ZSyncAnimationZDOSalt + PrivateAccessor.CharacterAnimationHashInWater, defaultValue);
    public bool GetAnimationIsCrouching(bool defaultValue = default) => _zdo.GetBool(PrivateAccessor.ZSyncAnimationZDOSalt + PrivateAccessor.PlayerAnimationHashCrouching, defaultValue);
    static readonly int _animationCraftingHash = ZSyncAnimation.GetHash("crafting");
    public int GetAnimationCrafting(int defaultValue = default) => _zdo.GetInt(PrivateAccessor.ZSyncAnimationZDOSalt + _animationCraftingHash, defaultValue);
    public DateTime GetTameLastFeeding(DateTime defaultValue = default) => new(_zdo.GetLong(global::ZDOVars.s_tameLastFeeding, defaultValue.Ticks));
    public void SetTameLastFeeding(DateTime value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_tameLastFeeding, value.Ticks); }
    public bool GetEventCreature(bool defaultValue = default) => _zdo.GetBool(global::ZDOVars.s_eventCreature, defaultValue);
    public bool GetInBed(bool defaultValue = default) => _zdo.GetBool(global::ZDOVars.s_inBed, defaultValue);
    public int GetLocation(int defaultValue = default) => _zdo.GetInt(global::ZDOVars.s_location, defaultValue);
    public int GetSeed(int defaultValue = default) => _zdo.GetInt(global::ZDOVars.s_seed, defaultValue);
    public bool GetAttachJoint(bool defaultValue = default) => _zdo.GetBool(global::ZDOVars.s_attachJointHash, defaultValue);
    public long GetUser(long defaultValue = 0) => _zdo.GetLong(global::ZDOVars.s_user, defaultValue);
    public bool GetIsDead(bool defaultValue = default) => _zdo.GetBool(global::ZDOVars.s_dead, defaultValue);
    public long GetOwner(long defaultValue = 0) => _zdo.GetLong(global::ZDOVars.s_owner, defaultValue);
    public void SetOwner(long value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_owner, value); }
    public string GetOwnerName(string defaultValue = "") => _zdo.GetString(global::ZDOVars.s_ownerName, defaultValue);
    public void SetOwnerName(string value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_ownerName, value); }
    public float GetScaleScalar(float defaultValue = default) => _zdo.GetFloat(global::ZDOVars.s_scaleScalarHash, defaultValue);
    public void SetScaleScalar(float value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_scaleScalarHash, value); }
    public Vector3 GetScale(Vector3 defaultValue = default) => _zdo.GetVec3(global::ZDOVars.s_scaleHash, defaultValue);
    public void SetScale(Vector3 value, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.Set(global::ZDOVars.s_scaleHash, value); }
    public void RemoveScale([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNo = 0) { ValidateOwnership(filePath, lineNo); _zdo.RemoveVec3(global::ZDOVars.s_scaleHash); }
    public bool GetEnabled(bool defaultValue = default) => _zdo.GetBool(global::ZDOVars.s_enabled, defaultValue);

#if DEBUG
    static readonly IReadOnlyDictionary<int, string> __namesByHash = new Func<IReadOnlyDictionary<int, string>>(static () =>
    {
      var result = new Dictionary<int, string>();
      foreach (var (hash, name) in typeof(global::ZDOVars).GetFields(BindingFlags.Public | BindingFlags.Static)
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
          ServersideQoLPlugin.Logger.DevLog($"Duplicate hash: {existing}, {name} (hash = {hash})");
        else
          result.Add(hash, name);
      }
      return result;
    }).Invoke();

    static string GetName(int hash) => __namesByHash.TryGetValue(hash, out var name) ? name : $"Unkown ({hash})";

    public string ToDebugString()
    {
      return "";
      //var ints = ZDOExtraData.GetInts(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
      //var longs = ZDOExtraData.GetLongs(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
      //var floats = ZDOExtraData.GetFloats(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
      //var quats = ZDOExtraData.GetQuaternions(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
      //var strings = ZDOExtraData.GetStrings(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
      //var vecs = ZDOExtraData.GetVec3s(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
      //var byteArrays = ZDOExtraData.GetByteArrays(_zdo.m_uid).Select(static x => (Name: GetName(x.Key), x.Value)).OrderBy(static x => x.Name).ToList();
      //return $"""
      //    ints ({ints.Count}):{string.Join($"{Environment.NewLine}  ", ints.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
      //    longs ({longs.Count}):{string.Join($"{Environment.NewLine}  ", longs.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
      //    floats ({floats.Count}):{string.Join($"{Environment.NewLine}  ", floats.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
      //    quats ({quats.Count}):{string.Join($"{Environment.NewLine}  ", quats.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
      //    strings ({strings.Count}):{string.Join($"{Environment.NewLine}  ", strings.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
      //    vecs ({vecs.Count}):{string.Join($"{Environment.NewLine}  ", vecs.Select(static x => $"{x.Name}: {x.Value}").Prepend(""))}
      //    byte arrays ({byteArrays.Count}):{string.Join($"{Environment.NewLine}  ", byteArrays.Select(static x => $"{x.Name}: Length={x.Value.Length}").Prepend(""))}
      //    """;
    }
#endif
  }
}
