using UnityEngine;

namespace ServersideQoL;

[Processor(Id, OnlyWhenDependedOn = true)]
public sealed class TameableRegistryProcessor : Processor<TameableRegistryProcessor.PrefabInfo>
{
  public const string Id = "c79b3771-b9a8-46e9-b3eb-5ffe6c9708b4";
  public sealed record PrefabInfo(Tameable Tameable, MonsterAI MonsterAI) : ProcessorPrefabInfo;

  readonly Dictionary<ServersideQoLZDO, TameableStateImpl> _states = [];

  public SectorDictionary<HashSet<ServersideQoLZDO>> Tameables { get; } = new(ZoneSystem.c_ZoneSize);

  public TameableState? GetState(ServersideQoLZDO zdo) => _states.TryGetValue(zdo, out var state) ? state : null;

  protected internal override void Initialize()
  {
    _states.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!_states.TryGetValue(zdo, out var state))
    {
      _states.Add(zdo, state = new(zdo, prefabInfo));
      zdo.Destroyed += x => _states.Remove(x);
      Tameables.Add(state.LastKey = zdo.ZDO.GetPosition(), zdo);
    }
    else if (Tameables.TryAdd(zdo))
    {
      Tameables[state.LastKey].Remove(zdo);
      state.LastKey = zdo.ZDO.GetPosition();
    }

    if (zdo.Vars.GetTamed())
      state.SetState(TameableState.States.Tamed);
    else
    {
      /// <see cref="Tameable.GetRemainingTime()"/>
      var tameTime = zdo.Fields<Tameable>().GetFloat(static () => x => x.m_tamingTime);
      var tameTimeLeft = zdo.Vars.GetTameTimeLeft(tameTime);
      if (tameTimeLeft < tameTime)
        state.SetState(TameableState.States.Taming);
      else
        state.SetState(TameableState.States.Wild);
    }

    return ProcessResult.WaitForZDORevisionChange;
  }

  sealed class TameableStateImpl(ServersideQoLZDO zdo, PrefabInfo prefabInfo) : TameableState
  {
    readonly ServersideQoLZDO _zdo = zdo;
    readonly PrefabInfo _prefabInfo = prefabInfo;
    States _state;
    public override PrefabInfo PrefabInfo => _prefabInfo;
    public override States State => _state;
    public Vector3 LastKey { get; set; }

    public void SetState(States state) => _state = state;
  }
}
