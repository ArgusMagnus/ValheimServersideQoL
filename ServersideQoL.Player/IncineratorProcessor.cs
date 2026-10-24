using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using static ItemSets;
using static UnityEngine.Random;

namespace ServersideQoL.Player;

[Processor(Id)]
[RunAfter<ContainerRegistryProcessor>]
[DependsOn<PlayerRegistryProcessor>]
public sealed class IncineratorProcessor : Processor<IncineratorProcessor.PrefabInfo>
{
  public const string Id = "30cb1ca8-ae2d-4982-af60-f47e0207d603";
  public sealed record PrefabInfo(Incinerator Incinerator, Container Container, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  readonly Dictionary<string, SacrificeInfo> _sacrifices = [];
  readonly Dictionary<ServersideQoLZDO, State> _states = [];
  bool _enabled;

  protected override void Initialize()
  {
    _enabled =
      Config.Instance.CanSacrificeMegingjord.Value ||
      Config.Instance.CanSacrificeCryptKey.Value ||
      Config.Instance.CanSacrificeWishbone.Value ||
      Config.Instance.CanSacrificeTornSpirit.Value;

    RPC.Intercept.UpdateInterception(RPC.RpcName.Incinerator.RequestIncinerate, RPC_RequestIncinerate, _enabled);
    RPC.Intercept.UpdateInterception(RPC.RpcName.Incinerator.AnimateLever, RPC_AnimateLever, _enabled);
    RPC.Intercept.UpdateInterception(RPC.RpcName.Incinerator.AnimateLeverReturn, RPC_AnimateLeverReturn, _enabled);

    _sacrifices.Clear();
    if (Config.Instance.CanSacrificeMegingjord.Value)
      _sacrifices.Add(PrefabNames.Megingjord, new(static x => PlayerProcessor.SetSacrifiedMegingjord(x, true), StatusEffects.Megingjord, Config.Instance.Localization.Value.SacrificedMegingjord));
    if (Config.Instance.CanSacrificeCryptKey.Value)
      _sacrifices.Add(PrefabNames.CryptKey, new(static x => PlayerProcessor.SetSacrifiedCryptKey(x, true), null, Config.Instance.Localization.Value.SacrificedCryptKey));
    if (Config.Instance.CanSacrificeWishbone.Value)
      _sacrifices.Add(PrefabNames.Wishbone, new(static x => PlayerProcessor.SetSacrifiedWishbone(x, true), StatusEffects.Wishbone, Config.Instance.Localization.Value.SacrificedWishbone));
    if (Config.Instance.CanSacrificeTornSpirit.Value)
      _sacrifices.Add(PrefabNames.TornSpirit, new(static x => PlayerProcessor.SetSacrifiedTornSpirit(x, true), StatusEffects.Demister, Config.Instance.Localization.Value.SacrificedTornSpirit));
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!_enabled)
      return ProcessResult.UnregisterProcessor;

    if (_states.TryGetValue(zdo, out var state) && !state.LeverReturned && (!state.LeverPulled || zdo.Vars.GetInUse()))
      state.Sacrifices.Clear();

    var containerState = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo.Container);
    var inventory = containerState.GetInventory();
    var found = false;
    foreach (var item in inventory.Items)
    {
      if (item.m_dropPrefab?.name is not { } name || !_sacrifices.TryGetValue(name, out var sacrificeInfo))
        continue;

      if (state is null)
      {
        _states.Add(zdo, state = new() { Revision = zdo.ZDO.DataRevision });
        zdo.Destroyed += RemoveState;
      }
      else if (state.Revision != zdo.ZDO.DataRevision)
      {
        state.Revision = zdo.ZDO.DataRevision;
        state.Sacrifices.Clear();
      }

      state.RequestingPlayer ??= Instance<PlayerRegistryProcessor>().GetStateForPeerID(zdo.ZDO.GetOwner());
      if (state.RequestingPlayer is null)
        continue;

      sacrificeInfo.ItemName ??= Localization.instance.Localize(item.m_shared.m_name);
      state.Sacrifices.Add(sacrificeInfo);
      found = true;
      Logger.DevLog($"{name} found");
    }

    var result = ProcessResult.Default;
    if (found)
    {
      state.Sign ??= PlacePiece(zdo.ZDO.GetPosition(), Prefabs.Sign, zdo.ZDO.GetRotation());
      var text = string.Join("<br>", state.Sacrifices.Select(static x => x.ItemName).Prepend($"{state.RequestingPlayer!.PlayerName}:"));
      state.Sign.Vars.SetText($"<mark=#000000><color=white>{text}<size=45><br><br>");
    }
    else if (state is { LeverReturned: true, RequestingPlayer: not null })
    {
      foreach (var sacrifice in state.Sacrifices)
      {
        sacrifice.SetSacrificed(state.RequestingPlayer.PlayerID);
        if (sacrifice.StatusEffect is { } statusEffect)
        state.RequestingPlayer.ZDO.RPC.Character.AddStatusEffect(statusEffect);
        RPC.ShowMessage(state.RequestingPlayer.Owner, MessageHud.MessageType.Center, sacrifice.Message);
      }
      RemoveState(zdo);
    }
    else if (state?.Sacrifices.Count is 0)
    {
      RemoveState(zdo);
    }

    return result;
  }

  void RemoveState(ServersideQoLZDO zdo)
  {
    zdo.Destroyed -= RemoveState;
    if (_states.Remove(zdo, out var state) && state.Sign is not null)
      DestroyObject(state.Sign);
  }

  void RPC_RequestIncinerate(ServersideQoLZDO zdo, ZRoutedRpc.RoutedRPCData data, PlayerID playerID)
  {
    var playerState = Instance<PlayerRegistryProcessor>().GetStateForPeerID(data.m_senderPeerID)
      ?? Instance<PlayerRegistryProcessor>().GetStateForPlayerID(playerID);

    if (playerState is null)
    {
      Logger.LogWarning($"Player state for peerID={data.m_senderPeerID} or playerID={playerID.Value} not found");
      RemoveState(zdo);
    }
    else if (_states.TryGetValue(zdo, out var state))
      state.RequestingPlayer = playerState;
    else
      _states.Add(zdo, new() { RequestingPlayer = playerState, Revision = 0 });
  }

  void RPC_AnimateLever(ServersideQoLZDO zdo)
  {
    if (_states.TryGetValue(zdo, out var state))
      state.LeverPulled = true;
  }

  void RPC_AnimateLeverReturn(ServersideQoLZDO zdo)
  {
    if (_states.TryGetValue(zdo, out var state))
      state.LeverReturned = true;
  }
  sealed record SacrificeInfo(Action<PlayerID> SetSacrificed, int? StatusEffect, string Message)
  {
    public string? ItemName { get; set; }
  }

  sealed class State
  {
    public PlayerState? RequestingPlayer { get; set; }
    public required uint Revision { get; set; }
    public HashSet<SacrificeInfo> Sacrifices { get; } = [];
    public bool LeverPulled { get; set; }
    public bool LeverReturned { get; set; }
    public ServersideQoLZDO? Sign { get; set; }
  }
}
