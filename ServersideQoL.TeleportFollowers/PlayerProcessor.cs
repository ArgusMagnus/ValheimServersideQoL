using ServersideQoL.Processors;
using UnityEngine;

namespace ServersideQoL.TeleportFollowers;

[Processor(Id)]
[DependsOn<TameableRegistryProcessor>]
[RunAfter<PlayerRegistryProcessor>]
public sealed class PlayerProcessor : Processor<ProcessorPrefabInfo<Player>>
{
  public const string Id = "72a96de3-a080-4177-ba59-c0142d4632c9";

  readonly Dictionary<ServersideQoLZDO, State> _states = [];

  protected override void Initialize()
  {
    _states.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<Player> prefabInfo)
  {
    if (!_states.TryGetValue(zdo, out var state))
    {
      _states.Add(zdo, state = new(Instance<PlayerRegistryProcessor>().GetState(zdo)));
      zdo.Destroyed += x => _states.Remove(x);
    }

    if (!Character.InInterior(zdo.ZDO.GetPosition()))
      state.InitialInInteriorPosition = null;
    else if (state.InitialInInteriorPosition is null)
      state.InitialInInteriorPosition = zdo.ZDO.GetPosition();

    if (Instance<TameableRegistryProcessor>().GetFollowers(state.PlayerState.PlayerName) is not { Count: > 0 } followers)
      return default;

    var playerZone = zdo.ZDO.GetSector();

    foreach (var tameState in followers)
    {
      var tameZone = tameState.ZDO.ZDO.GetSector();
      if (!ShouldTeleport(playerZone, tameZone, zdo, tameState.ZDO, state))
        continue;

      /// <see cref="TeleportWorld.Teleport"/>
      var targetPos = zdo.ZDO.GetPosition();
      var direction = zdo.ZDO.GetRotation() * Vector3.forward;
      var p = Config.Instance.Advanced.Value.TeleportFollowPositioning;
      targetPos += Quaternion.Euler(0, UnityEngine.Random.Range(-p.HalfArcXZ, p.HalfArcXZ), 0) * direction * UnityEngine.Random.Range(p.MinDistXZ, p.MaxDistXZ);
      targetPos.y += UnityEngine.Random.Range(p.MinOffsetY, p.MaxOffsetY);
      tameState.ZDO.ZDO.SetPosition(targetPos);
      tameState.ZDO.Recreate();
    }

    return default;
  }

  bool ShouldTeleport(in Vector2s playerZone, in Vector2s tameZone, ServersideQoLZDO player, ServersideQoLZDO tame, State state)
  {
    if (Config.Instance.TakeIntoDungeons.Value && Character.InInterior(player.ZDO.GetPosition()) != Character.InInterior(tame.ZDO.GetPosition()))
    {
      if (state.InitialInInteriorPosition is null)
        return true;
      // Workaround because the player position/rotation is not correctly updated until the player moves a bit after entering a dungeon
      if (Utils.DistanceXZ(state.InitialInInteriorPosition.Value, player.ZDO.GetPosition()) > 0.5f)
        return true;
      return false;
    }

    if (!Character.InInterior(player.ZDO.GetPosition()))
    {
      if (Utils.DistanceXZ(player.ZDO.GetPosition(), tame.ZDO.GetPosition()) >= Config.Instance.MinDistance.Value)
        return true;
      return false;
    }

    return false;
  }

  sealed class State(PlayerState playerState)
  {
    public PlayerState PlayerState { get; } = playerState;
    public Vector3? InitialInInteriorPosition { get; set; }
  }
}
