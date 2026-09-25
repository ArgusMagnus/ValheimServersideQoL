using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.PhoenixPersons;

[Processor(Id)]
[DependsOn<PlayerRegistryProcessor>]
public sealed class TombStoneProcessor : Processor<ProcessorPrefabInfo<TombStone>>
{
  public const string Id = "ad7d3aa5-07a0-4618-a762-19697365e75c";

  readonly Dictionary<ServersideQoLZDO, State> _summoningPlayers = [];

  protected override void Initialize()
  {
    _summoningPlayers.Clear();

    Instance<PlayerRegistryProcessor>().EmoteDetected -= OnEmoteDetected;
    Instance<PlayerRegistryProcessor>().EmoteDetected += OnEmoteDetected;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<TombStone> prefabInfo)
  {
    var reprocessingDelay = Config.Instance.Advanced.Value.TombStoneProcessingIntervalSeconds;
    if (Instance<PlayerRegistryProcessor>().GetStateForPlayerID(zdo.Vars.GetOwner()) is not { } playerState)
      return ScheduleReprocessing(reprocessingDelay);

    if (Utils.DistanceXZ(zdo.ZDO.GetPosition(), playerState.ZDO.ZDO.GetPosition()) <= Config.Instance.SummoningRange.Value)
      return ScheduleReprocessing(reprocessingDelay);

    foreach (var peer in peers.Enumerate())
    {
      if (peer.PlayerState is not { } summonerState || !_summoningPlayers.TryGetValue(summonerState.ZDO, out var state))
        continue;

      if (summonerState.ZDO.Vars.GetEmote() != Config.Instance.SummoningEmote.Value)
      {
        _summoningPlayers.Remove(summonerState.ZDO);
        summonerState.ZDO.Destroyed -= OnPlayerDestroyed;
        continue;
      }

      if (Utils.DistanceXZ(zdo.ZDO.GetPosition(), summonerState.ZDO.ZDO.GetPosition()) > Config.Instance.SummoningRange.Value)
        continue;

      var delay = state.SummoningFinished.Seconds - Timestamp.Now.Seconds;
      if (delay > 0)
      {
        reprocessingDelay = Mathf.Min(1f, Mathf.Min(reprocessingDelay, delay));
        RPC.ShowMessage(peer, MessageHud.MessageType.Center, Config.Instance.Localization.Value.FormatSummoningMessage(delay));
        if (playerState.ZDO.Vars.GetEmote() == Config.Instance.SummoningEmote.Value)
          RPC.ShowMessage(playerState.Owner, MessageHud.MessageType.Center, Config.Instance.Localization.Value.FormatBeingSummonedMessage(summonerState.PlayerName, delay));
        else
          RPC.ShowMessage(playerState.Owner, MessageHud.MessageType.Center, Config.Instance.Localization.Value.FormatAcceptSummonMessage(summonerState.PlayerName, Config.Instance.SummoningEmote.Value));
      }
      else if (playerState.ZDO.Vars.GetEmote() != Config.Instance.SummoningEmote.Value)
      {
        reprocessingDelay = Mathf.Min(1f, reprocessingDelay);
        RPC.ShowMessage(playerState.Owner, MessageHud.MessageType.Center, Config.Instance.Localization.Value.FormatAcceptSummonMessage(summonerState.PlayerName, Config.Instance.SummoningEmote.Value));
        RPC.ShowMessage(peer, MessageHud.MessageType.Center, Config.Instance.Localization.Value.FormatAwaitingAcceptanceMessage(playerState.PlayerName));
      }
      else
      {
        var pos = summonerState.ZDO.ZDO.GetPosition();
        pos.y += 2;
        playerState.ZDO.RPC.Player.TeleportTo(pos, summonerState.ZDO.ZDO.GetRotation(), false);
        _summoningPlayers.Remove(summonerState.ZDO);
        summonerState.ZDO.Destroyed -= OnPlayerDestroyed;
      }
    }

    return ScheduleReprocessing(reprocessingDelay);
  }

  void OnEmoteDetected(PlayerState playerState, Emotes emote)
  {
    if (Config.Instance.SummoningEmote.Value != emote)
    {
      _summoningPlayers.Remove(playerState.ZDO);
      playerState.ZDO.Destroyed -= OnPlayerDestroyed;
      return;
    }

    if (_summoningPlayers.TryAdd(playerState.ZDO, new(Timestamp.Now.AddSeconds(Config.Instance.SummoningDurationSeconds.Value))))
      playerState.ZDO.Destroyed += OnPlayerDestroyed;
  }

  void OnPlayerDestroyed(ServersideQoLZDO zdo)
  {
    _summoningPlayers.Remove(zdo);
  }

  sealed class State(Timestamp summoningFinished)
  {
    public Timestamp SummoningFinished { get; } = summoningFinished;
  }
}
