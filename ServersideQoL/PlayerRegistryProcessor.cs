using System.Text.RegularExpressions;
using UnityEngine;

namespace ServersideQoL;

[Processor("b5107f88-1c1f-4323-bfce-9205ae4dfcd9", OnlyWhenDependedOn = true)]
public sealed class PlayerRegistryProcessor : Processor<PlayerRegistryProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Player Player) : ProcessorPrefabInfo;

  readonly Dictionary<long, PlayerStateImpl> _statesByPeerID = [];
  readonly Dictionary<long, PlayerStateImpl> _statesByPlayerID = [];
  readonly Dictionary<ZDOID, PlayerStateImpl> _statesByCharacterID = [];

  public event Action<PlayerState, Emotes>? EmoteDetected;

  public delegate void StaminaUpdatedHandler(PlayerState state, bool staminaValueChanged);
  public event StaminaUpdatedHandler? StaminaUpdated;

  public delegate void ItemUsedHandler(PlayerState state, string animationTriggerName);
  ItemUsedHandler? _itemUsed;
  public event ItemUsedHandler? ItemUsed
  {
    add
    {
      if (_itemUsed is null && value is not null)
        RPC.Intercept.UpdateInterception("SetTrigger", OnZSyncAnimationSetTrigger, true);
      _itemUsed += value;
    }
    remove
    {
      var wasNotNull = _itemUsed is not null;
      _itemUsed -= value;
      if (_itemUsed is null && wasNotNull)
        RPC.Intercept.UpdateInterception("SetTrigger", OnZSyncAnimationSetTrigger, false);
    }
  }

  public PlayerState? GetStateForPeerID(long peerID) => _statesByPeerID.TryGetValue(peerID, out var state) ? state : null;
  public PlayerState? GetStateForPlayerID(long playerID) => _statesByPlayerID.TryGetValue(playerID, out var state) ? state : null;
  public PlayerState? GetStateForCharacterID(ZDOID characterID) => _statesByCharacterID.TryGetValue(characterID, out var state) ? state : null;
  public IReadOnlyCollection<PlayerState> PlayerStates => _statesByPeerID.Values;

  public PlayerState GetState(ServersideQoLZDO playerZdo)
  {
    var prefabInfo = GetPrefabInfo(playerZdo);
    System.Diagnostics.Debug.Assert(prefabInfo is not null);
    return GetStateCore(playerZdo, prefabInfo);
  }

  protected internal override void Initialize()
  {
    _statesByPeerID.Clear();
    _statesByPlayerID.Clear();
    _statesByCharacterID.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
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

    if (StaminaUpdated is not null)
    {
      var now = DateTimeOffset.UtcNow;
      if (state.NextStaminaCheck < now)
      {
        state.NextStaminaCheck = now.AddSeconds(Config.Instance.Advanced.Value.Players.UpdateStaminaInterval);
        var stamina = Mathf.FloorToInt(zdo.Vars.GetStamina());
        StaminaUpdated(state, state.UpdateStamina(stamina, now));
      }
    }

    state.SendGlobalKeyModifications();

    return default;
  }

  PlayerStateImpl GetStateCore(ServersideQoLZDO zdo, PrefabInfo prefabInfo)
  {
    var peerID = zdo.ZDO.GetOwner();
    if (!_statesByPeerID.TryGetValue(peerID, out var state))
    {
      _statesByPeerID.Add(peerID, state = new(zdo, prefabInfo));
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
    if (Instance<PlayerRegistryProcessor>().GetStateForCharacterID(data.m_targetZDO) is not PlayerStateImpl state || _itemUsed is null)
      return;

    var item = GetItem(state.ZDO.Vars.GetRightItem(), state, name) ?? GetItem(state.ZDO.Vars.GetLeftItem(), state, name);
    if (item is null)
      return;

    state.SetLastUsedItem(item);
    _itemUsed(state, name);
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

  sealed class PlayerStateImpl(ServersideQoLZDO zdo, PrefabInfo prefabInfo) : PlayerState
  {
    readonly ServersideQoLZDO _zdo = zdo;
    readonly PrefabInfo _prefabInfo = prefabInfo;

    public override ServersideQoLZDO ZDO => _zdo;
    public override PrefabInfo PrefabInfo => _prefabInfo;

    readonly ZNetPeer? _peer = ZNet.instance.GetPeer(zdo.ZDO.GetOwner());
    public override long Owner { get; } = zdo.ZDO.GetOwner();
    public override long PlayerID { get; } = zdo.Vars.GetPlayerID();
    string? _playerName;
    public override string PlayerName => _playerName ??= ZDO.Vars.GetPlayerName();
    bool? _isAdmin;
    public override bool IsAdmin => _isAdmin ??= (Player.m_localPlayer?.GetZDOID() == ZDO.ZDO.m_uid || ZNet.instance.IsAdmin(_peer?.m_socket.GetHostName() ?? ""));

    public int LastEmoteId { get; set; } = 0; // Ignore first 'Sit' when logging in

    public DateTimeOffset NextStaminaCheck { get; set; }
    int _stamina;
    DateTimeOffset _staminaTimestamp = DateTimeOffset.UtcNow;
    public override int Stamina => _stamina;
    public DateTimeOffset StaminaTimestamp => _staminaTimestamp;
    public bool UpdateStamina(int value, DateTimeOffset timestamp)
    {
      if (_stamina == value)
        return false;
      _stamina = value;
      _staminaTimestamp = timestamp;
      return true;
    }

    ItemDrop? _lastUsedItem;
    public override ItemDrop? LastUsedItem => _lastUsedItem;
    public void SetLastUsedItem(ItemDrop item) => _lastUsedItem = item;

    bool _hasChangedGlobalKeyModifications;
    Dictionary<GlobalKey, bool>? _globalKeyModifications;
    public override IReadOnlyDictionary<GlobalKey, bool> GlobalKeyModifications => _globalKeyModifications ?? EmptyReadOnlyCollections<GlobalKey, bool>.Dictionary;

    public override void AddGlobalKeyModification(GlobalKey key, bool add)
    {
      _globalKeyModifications ??= [];
      if (_globalKeyModifications.TryAdd(key, add))
        _hasChangedGlobalKeyModifications = true;
    }

    public override void RemoveGlobalKeyModification(GlobalKey key)
    {
      if (_globalKeyModifications?.Remove(key) is true)
        _hasChangedGlobalKeyModifications = true;
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
