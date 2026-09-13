using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.Processors;

[Processor("c79b3771-b9a8-46e9-b3eb-5ffe6c9708b4", OnlyWhenDependedOn = true)]
public sealed class TameableRegistryProcessor : Processor<TameableRegistryProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Tameable Tameable, MonsterAI MonsterAI, Humanoid Humanoid) : ProcessorPrefabInfo;

  readonly Dictionary<ServersideQoLZDO, TameableStateImpl> _states = [];
  readonly Dictionary<string, List<TameableState>> _tameablesByFollowPlayerName = [];

  public SectorDictionary<HashSet<ServersideQoLZDO>> Tameables { get; } = new(ZoneSystem.c_ZoneSize);

  public TameableState? GetState(ServersideQoLZDO zdo) => _states.TryGetValue(zdo, out var state) ? state : null;
  public IReadOnlyList<TameableState>? GetFollowers(string playerName)
    => _tameablesByFollowPlayerName.TryGetValue(playerName, out var list) ? list : null;

  protected internal override void Initialize()
  {
    _states.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!_states.TryGetValue(zdo, out var state))
    {
      _states.Add(zdo, state = new(zdo, prefabInfo));
      zdo.Destroyed += OnTameableDestroyed;
      Tameables.Add(state.LastKey = zdo.ZDO.GetPosition(), zdo);
    }
    else if (Tameables.TryAdd(zdo))
    {
      Tameables[state.LastKey].Remove(zdo);
      state.LastKey = zdo.ZDO.GetPosition();
    }

    if (zdo.Vars.GetTamed())
    {
      state.SetTamed();
      if (state.UpdateFollow(zdo.Vars.GetFollow(), out var oldFollow))
      {
        if (_tameablesByFollowPlayerName.TryGetValue(oldFollow, out var list))
          list.Remove(state);
        if (!_tameablesByFollowPlayerName.TryGetValue(state.FollowPlayerName, out list))
          _tameablesByFollowPlayerName.Add(state.FollowPlayerName, list = []);
        list.Add(state);
      }
    }
    else
    {
      /// <see cref="Tameable.GetRemainingTime()"/>
      var tameTime = zdo.Fields<Tameable>().GetFloat(static () => x => x.m_tamingTime);
      var tameTimeLeft = zdo.Vars.GetTameTimeLeft(tameTime);
      state.Update(tameTime, tameTimeLeft);
    }

    return default;
  }

  void OnTameableDestroyed(ServersideQoLZDO zdo)
  {
    if (!_states.Remove(zdo, out var state))
      return;

    if (_tameablesByFollowPlayerName.TryGetValue(state.FollowPlayerName, out var list))
      list.Remove(state);
  }

  sealed class TameableStateImpl(ServersideQoLZDO zdo, PrefabInfo prefabInfo) : TameableState
  {
    readonly ServersideQoLZDO _zdo = zdo;
    readonly PrefabInfo _prefabInfo = prefabInfo;
    States _state;
    Action<TameableState>? _stateChanged;
    float _tameness;
    string _followPlayerName = "";
    public override PrefabInfo PrefabInfo => _prefabInfo;
    public override ServersideQoLZDO ZDO => _zdo;
    public override States State => _state;
    public override event Action<TameableState>? StateChanged
    {
      add => _stateChanged += value;
      remove => _stateChanged -= value;
    }
    public override float Tameness => _tameness;
    public override string FollowPlayerName => _followPlayerName;
    public Vector3 LastKey { get; set; }

    public void SetTamed()
    {
      if (_state is States.Tamed)
        return;
      _state = States.Tamed;
      _tameness = 1;
      _stateChanged?.Invoke(this);
    }

    public void Update(float tameTime, float tameTimeLeft)
    {
      var state = tameTimeLeft < tameTime ? States.Taming : States.Wild;
      _tameness = 1f - Mathf.Clamp01(tameTimeLeft / tameTime);
      if (state == _state)
        return;
      _state = state;
      _stateChanged?.Invoke(this);
    }

    public bool UpdateFollow(string playerName, out string oldValue)
    {
      oldValue = _followPlayerName;
      if (_followPlayerName == playerName)
        return false;
      _followPlayerName = playerName;
      return true;
    }
  }
}
