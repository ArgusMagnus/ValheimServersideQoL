using BepInEx.Logging;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Sources;
using UnityEngine;
using static Skills;

namespace ServersideQoL;

[Processor("b5107f88-1c1f-4323-bfce-9205ae4dfcd9", OnlyWhenDependedOn = true)]
public sealed class PlayerRegistryProcessor : Processor<ProcessorPrefabInfo<Player>>
{
  readonly Dictionary<long, PlayerStateImpl> _statesByPeerID = [];
  readonly Dictionary<long, PlayerStateImpl> _statesByPlayerID = [];
  readonly Dictionary<ZDOID, PlayerStateImpl> _statesByCharacterID = [];
  readonly HashSet<(string, int)> _estimateSkillLevelsFor = [];
  bool _estimateSkillLevels = false;

  public event Action<PlayerState, Emotes>? EmoteDetected;
  public event Action<PlayerState>? StaminaUpdated;

  public delegate void ItemUsedHandler(PlayerState state, string animationTriggerName);
  ItemUsedHandler? _itemUsed;
  public event ItemUsedHandler? ItemUsed
  {
    add
    {
      if (_itemUsed is null && value is not null && !_estimateSkillLevels)
        RPC.Intercept.UpdateInterception("SetTrigger", OnZSyncAnimationSetTrigger, true);
      _itemUsed += value;
    }
    remove
    {
      var wasNotNull = _itemUsed is not null;
      _itemUsed -= value;
      if (_itemUsed is null && wasNotNull && !_estimateSkillLevels)
        RPC.Intercept.UpdateInterception("SetTrigger", OnZSyncAnimationSetTrigger, false);
    }
  }

  public PlayerState? GetStateForPeerID(long peerID) => _statesByPeerID.TryGetValue(peerID, out var state) ? state : null;
  public PlayerState? GetStateForPlayerID(long playerID) => _statesByPlayerID.TryGetValue(playerID, out var state) ? state : null;
  public PlayerState? GetStateForCharacterID(ZDOID characterID) => _statesByCharacterID.TryGetValue(characterID, out var state) ? state : null;
  public IReadOnlyCollection<PlayerState> PlayerStates => _statesByPeerID.Values;

  public PlayerState GetState(ServersideQoLZDO playerZdo)
  {
    var prefabInfo = GetProcessorPrefabInfo(playerZdo);
    System.Diagnostics.Debug.Assert(prefabInfo is not null);
    return GetStateCore(playerZdo, prefabInfo);
  }

  public void EnableSkillLevelEstimation(bool enable, [CallerMemberName] string filename = default!, [CallerLineNumber] int lineNo = 0)
  {
    if (enable ? _estimateSkillLevelsFor.Add((filename, lineNo)) : _estimateSkillLevelsFor.Remove((filename, lineNo)))
    {
      if (_estimateSkillLevels == (_estimateSkillLevelsFor.Count is not 0))
        return;
      _estimateSkillLevels = !_estimateSkillLevels;
      if (_estimateSkillLevels && _itemUsed is null)
        RPC.Intercept.UpdateInterception("SetTrigger", OnZSyncAnimationSetTrigger, true);
      else if (!_estimateSkillLevels && _itemUsed is null)
        RPC.Intercept.UpdateInterception("SetTrigger", OnZSyncAnimationSetTrigger, false);
    }
  }

  protected internal override void Initialize()
  {
    _statesByPeerID.Clear();
    _statesByPlayerID.Clear();
    _statesByCharacterID.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<Player> prefabInfo)
  {
    var state = GetStateCore(zdo, prefabInfo);

    if (EmoteDetected is not null)
    {
      /// <see cref="Emote.DoEmote(Emotes)"/> <see cref="Player.StartEmote(string, bool)"/>
      if (zdo.Vars.GetEmoteID() is var emoteId && emoteId != state.LastEmoteId)
      {
        state.LastEmoteId = emoteId;
        var emote = zdo.Vars.GetEmote();
        if (emote is not ConfigBase.DisabledEmote)
          EmoteDetected(state, emote);
      }
    }

    if (StaminaUpdated is not null || _estimateSkillLevels)
    {
      var now = Timestamp.Now;
      if (state.NextStaminaCheck < now)
      {
        state.NextStaminaCheck = now.AddSeconds(Config.Instance.Advanced.Value.Players.UpdateStaminaInterval);
        var stamina = Mathf.FloorToInt(zdo.Vars.GetStamina());
        if (state.UpdateStamina(stamina, now))
          StaminaUpdated?.Invoke(state);

        state.UpdateEitr(Mathf.FloorToInt(zdo.Vars.GetEitr()), now);
      }
    }

    if (_estimateSkillLevels && state.CheckSkillItem is not null)
    {
      var usesEitr = state.CheckSkillItem.m_itemData.m_shared.m_attack.m_attackEitr > 0;
      var staminaOrEitr = usesEitr ? zdo.Vars.GetEitr() : zdo.Vars.GetStamina();

      if (staminaOrEitr < state.CheckSkillStaminaEitr)
      {
        var shared = state.CheckSkillItem.m_itemData.m_shared;
        var max = usesEitr ? shared.m_attack.m_attackEitr : shared.m_attack.m_attackStamina;
        var eff = state.CheckSkillStaminaEitr - staminaOrEitr;
        var diff = max - eff;
        var estSkill = diff / (max * 0.33f);
        if (estSkill is >= 0f and <= 1f)
        {
          const int HalfHistoryWindow = 3;
          const int HistoryWindow = 2 * HalfHistoryWindow + 1;

          var prevEstSkill = state.GetEstimatedSkillLevel(shared.m_skillType);
          if (!state.EstimatedSkillLevelHistories.TryGetValue(shared.m_skillType, out var history))
          {
            state.EstimatedSkillLevelHistories.Add(shared.m_skillType, history = (new(HistoryWindow), new(HistoryWindow)));
            if (!float.IsNaN(prevEstSkill))
            {
              for (int i = 0; i < HistoryWindow; i++)
              {
                history.Queue.Enqueue(prevEstSkill);
                history.List.Add(prevEstSkill);
              }
            }
          }

          while (history.Queue.Count >= HistoryWindow)
            history.List.Remove(history.Queue.Dequeue());

          history.Queue.Enqueue(estSkill);
          history.List.InsertSorted(estSkill);
          // median
          estSkill = history.List[history.List.Count / 2];

          state.EstimatedSkillLevels[shared.m_skillType] = estSkill;
          SetEstimatedSkillLevel(state.PlayerID, shared.m_skillType, estSkill);
          var intSkill = Mathf.Floor(estSkill * 100);
          var intPrevSkill = Mathf.Floor(prevEstSkill * 100);
          if (intSkill != intPrevSkill)
            Logger.Log(intSkill - intPrevSkill > 1f ? LogLevel.Warning : LogLevel.Info, $"Player {state.PlayerName}: Estimated {shared.m_skillType} skill level: {intSkill}, Previous estimate: {intPrevSkill} (Item: {state.CheckSkillItem.name}, max stamina: {max}, used stamina: {eff})");
        }
        state.CheckSkillItem = null;
      }
    }

    state.SendGlobalKeyModifications();

    return default;
  }

  PlayerStateImpl GetStateCore(ServersideQoLZDO zdo, ProcessorPrefabInfo<Player> prefabInfo)
  {
    var peerID = zdo.ZDO.GetOwner();
    if (!_statesByPeerID.TryGetValue(peerID, out var state))
    {
      _statesByPeerID.Add(peerID, state = new(zdo, prefabInfo, this));
      _statesByPlayerID[state.PlayerID] = state;
      _statesByCharacterID[zdo.ZDO.m_uid] = state;
      zdo.Destroyed += OnPlayerDestroyed;
    }
    return state;
  }

  void OnPlayerDestroyed(ServersideQoLZDO zdo)
  {
    // zdo.GetOwner() is no longer valid here, so use zdo.m_uid.UserID instead
    if (!_statesByPeerID.Remove(zdo.ZDO.m_uid.UserID, out var state))
      return;

    _statesByCharacterID.Remove(zdo.ZDO.m_uid);
    if (_statesByPlayerID.Remove(state.PlayerID, out var state2) && state2 != state)
      _statesByPlayerID.Add(state.PlayerID, state2);
  }

  /// <see cref="ZSyncAnimation.SetTrigger(string)"/>
  void OnZSyncAnimationSetTrigger(ZRoutedRpc.RoutedRPCData data, string name)
  {
    if (Instance<PlayerRegistryProcessor>().GetStateForCharacterID(data.m_targetZDO) is not PlayerStateImpl state)
      return;

    var item = GetItem(state.ZDO.Vars.GetRightItem(), state, name) ?? GetItem(state.ZDO.Vars.GetLeftItem(), state, name);
    if (item is null)
      return;

    state.SetLastUsedItem(item);
    _itemUsed?.Invoke(state, name);

    if (_estimateSkillLevels)
    {
      state.CheckSkillItem = null;
      var now = Timestamp.Now;
      if (item.m_itemData.m_shared is { m_attack.m_attackStamina: > 0 } and ({ m_skillType: not SkillType.Swords } or { m_damages.m_slash: > 0 }))
      {
        if (state.StaminaTimestamp < now.AddSeconds(-1.5f * state.PrefabInfo.Component.m_staminaRegenDelay))
        {
          var stamina = state.ZDO.Vars.GetStamina();
          var floored = Mathf.FloorToInt(stamina);
          if (state.UpdateStamina(floored, now))
            StaminaUpdated?.Invoke(state);
          else if (stamina >= 2 * item.m_itemData.m_shared.m_attack.m_attackStamina) // infinite stamina feature might interfere
          {
            state.CheckSkillStaminaEitr = stamina;
            state.CheckSkillItem = item;
          }
        }
      }
      else if (item.m_itemData.m_shared.m_attack.m_attackEitr > 0)
      {
        if (state.EitrTimestamp < now.AddSeconds(-1.5f * state.PrefabInfo.Component.m_eitrRegenDelay))
        {
          var eitr = state.ZDO.Vars.GetEitr();
          var floored = Mathf.FloorToInt(eitr);
          if (!state.UpdateEitr(floored, now))
          {
            state.CheckSkillStaminaEitr = eitr;
            state.CheckSkillItem = item;
          }
        }
      }
    }
  }

  ItemDrop? GetItem(int prefab, PlayerStateImpl state, string triggerName)
  {
    if (prefab is 0)
      return null;

    if (ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>() is not { } item)
    {
      Logger.LogWarning($"Player {state.PlayerName}: SetTrigger({triggerName}): Item prefab '{prefab}' not found");
      return null;
    }

    if (item?.m_itemData.m_shared.m_attack is not { } attack)
      return null;

    /// <see cref="Attack.Start"/>
    if (attack.m_attackChainLevels > 1 || attack.m_attackRandomAnimations >= 2)
    {
      if (Regex.IsMatch(triggerName, $@"^{Regex.Escape(attack.m_attackAnimation)}\d+$"))
        return item;
    }
    else if (triggerName == attack.m_attackAnimation)
      return item;

    return null;
  }

  static float GetEstimatedSkillLevel(long playerID, SkillType skill, float defaultValue = default) => DataZDO.ZDO.GetFloat($"player{playerID}_EstimatedSkillLevel_{skill}", defaultValue);
  static void SetEstimatedSkillLevel(long playerID, SkillType skill, float value) => DataZDO.ZDO.Set($"player{playerID}_EstimatedSkillLevel_{skill}", value);


  sealed class PlayerStateImpl(ServersideQoLZDO zdo, ProcessorPrefabInfo<Player> prefabInfo, PlayerRegistryProcessor processor) : PlayerState
  {
    readonly ServersideQoLZDO _zdo = zdo;
    readonly ProcessorPrefabInfo<Player> _prefabInfo = prefabInfo;
    readonly PlayerRegistryProcessor _processor = processor;

    public override ServersideQoLZDO ZDO => _zdo;
    public override ProcessorPrefabInfo<Player> PrefabInfo => _prefabInfo;

    readonly ZNetPeer? _peer = ZNet.instance.GetPeer(zdo.ZDO.GetOwner());
    public override long Owner { get; } = zdo.ZDO.GetOwner();
    public override long PlayerID { get; } = zdo.Vars.GetPlayerID();
    string? _playerName;
    public override string PlayerName => _playerName ??= ZDO.Vars.GetPlayerName();
    bool? _isAdmin;
    public override bool IsAdmin => _isAdmin ??= (Player.m_localPlayer?.GetZDOID() == ZDO.ZDO.m_uid || ZNet.instance.IsAdmin(_peer?.m_socket.GetHostName() ?? ""));

    public int LastEmoteId { get; set; } = 0; // Ignore first 'Sit' when logging in

    public Timestamp NextStaminaCheck { get; set; }
    int _stamina;
    Timestamp _staminaTimestamp;
    public override int Stamina => _stamina;
    public Timestamp StaminaTimestamp => _staminaTimestamp;
    public bool UpdateStamina(int value, Timestamp timestamp)
    {
      if (_stamina == value)
        return false;
      _stamina = value;
      _staminaTimestamp = timestamp;
      return true;
    }

    int _eitr;
    Timestamp _eitrTimestamp;
    public override int Eitr => _eitr;
    public Timestamp EitrTimestamp => _eitrTimestamp;
    public bool UpdateEitr(int value, Timestamp timestamp)
    {
      if (_eitr == value)
        return false;
      _eitr = value;
      _eitrTimestamp = timestamp;
      return true;
    }

    ItemDrop? _lastUsedItem;
    public override ItemDrop? LastUsedItem => _lastUsedItem;
    public void SetLastUsedItem(ItemDrop item) => _lastUsedItem = item;

    public ItemDrop? CheckSkillItem { get; set; }
    public float CheckSkillStaminaEitr { get; set; }
    public Dictionary<SkillType, float> EstimatedSkillLevels => field ??= [];
    public Dictionary<SkillType, (Queue<float> Queue, List<float> List)> EstimatedSkillLevelHistories => field ??= [];

    public override float GetEstimatedSkillLevel(SkillType skillType)
    {
      if (!_processor._estimateSkillLevels)
        throw new InvalidOperationException($"{nameof(EnableSkillLevelEstimation)}(true) must be called before calling {nameof(GetEstimatedSkillLevel)}");
      if (!EstimatedSkillLevels.TryGetValue(skillType, out var level))
        EstimatedSkillLevels.Add(skillType, level = PlayerRegistryProcessor.GetEstimatedSkillLevel(PlayerID, skillType, float.NaN));
      return level;
    }

    bool _hasChangedGlobalKeyModifications;
    Dictionary<GlobalKey, (bool? Add, float? Value)>? _globalKeyModifications;
    public override IReadOnlyDictionary<GlobalKey, (bool? Add, float? Value)> GlobalKeyModifications => _globalKeyModifications ?? EmptyReadOnlyCollections<GlobalKey, (bool? Add, float? Value)>.Dictionary;

    void OnGlobalKeyModificationChanged(string callerFilePath)
    {
      _hasChangedGlobalKeyModifications = true;
      if (ZNet.instance.IsDedicated())
        return;

      var context = Path.GetFileName(Path.GetDirectoryName(callerFilePath));
      ServersideQoLPlugin.Logger.LogWarning($"Features depending on modifying global keys don't work for the host or in single player (context: {context })");
    }

    public override void AddGlobalKeyModification(GlobalKey key, bool add, string callerFilePath)
    {
      _globalKeyModifications ??= [];
      if (_globalKeyModifications.TryAdd(key, (add, null)))
        OnGlobalKeyModificationChanged(callerFilePath);
    }

    public override void AddGlobalKeyModification(GlobalKey key, float value, string callerFilePath)
    {
      _globalKeyModifications ??= [];
      if (_globalKeyModifications.TryAdd(key, (null, value)))
        OnGlobalKeyModificationChanged(callerFilePath);
    }

    public override void RemoveGlobalKeyModification(GlobalKey key, string callerFilePath)
    {
      if (_globalKeyModifications?.Remove(key) is true)
        OnGlobalKeyModificationChanged(callerFilePath);
    }

    public void SendGlobalKeyModifications()
    {
      if (!_hasChangedGlobalKeyModifications)
        return;
      ZoneSystem.instance.SendGlobalKeys(ZDO.ZDO.GetOwner());
      _hasChangedGlobalKeyModifications = false;
    }
  }
}
