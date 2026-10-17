using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using UnityEngine;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;
using static Skills;

namespace ServersideQoL.Skills;

[Processor(Id)]
[DependsOn<PlayerRegistryProcessor>]
public sealed class WearNTearProcessor : Processor<WearNTearProcessor.PrefabInfo>
{
  public const string Id = "1a454569-4719-4a4e-9b0a-d404b4ac7d95";
  public sealed record PrefabInfo(WearNTear WearNTear, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  SectorDictionary<HashSet<ServersideQoLZDO>>? _pieces;
  readonly HashSet<ServersideQoLZDO> _ignore = [];
  Timestamp _clearIgnoredAfter;

  protected override void Initialize()
  {
    var range = MathF.Max(Config.Instance.Crafting.AreaRepairRangeAtMinSkill.Value, Config.Instance.Crafting.AreaRepairRangeAtMaxSkill.Value);
    _pieces = range > 0 ? new(range) : null;

    RPC.Intercept.UpdateInterception(RPC.RpcName.WearNTear.HealthChanged, RPC_HealthChanged, range > 0 || Config.Instance.Crafting.HealthMultiplierEnabled);
    Instance<PlayerRegistryProcessor>().EnableSkillLevelEstimation(range > 0 || Config.Instance.Crafting.HealthMultiplierEnabled);
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    var creator = zdo.Vars.GetCreator();
    if (creator.Value is 0)
      return ProcessResult.UnregisterProcessor;

    _pieces?.TryAdd(zdo);

    var result = ProcessResult.UnregisterProcessor;

    if (Config.Instance.Crafting.HealthMultiplierEnabled
      && Instance<PlayerRegistryProcessor>().GetStateForPlayerID(creator) is { } playerState
      && zdo.Vars.GetHealth(prefabInfo.WearNTear.m_health) == prefabInfo.WearNTear.m_health)
    {
      var skill = playerState.GetEstimatedSkillLevel(SkillType.Crafting);
      if (!float.IsNaN(skill))
      {
        if (zdo.Fields<WearNTear>().UpdateValue(static () => x => x.m_health, GetMaxHealth(skill, prefabInfo)))
        {
          zdo.Vars.RemoveHealth();
          result |= ProcessResult.RecreateZDO | ProcessResult.ReregisterOnRecreated;
        }
      }
    }

    return result;
  }

  float GetMaxHealth(float skill, PrefabInfo prefabInfo)
    => Mathf.Max(1, Mathf.Ceil(prefabInfo.WearNTear.m_health * Mathf.Lerp(
      Config.Instance.Crafting.HealthMultiplierAtMinSkill.Value,
      Config.Instance.Crafting.HealthMultiplierAtMaxSkill.Value, skill)));

  void RPC_HealthChanged(ZRoutedRpc.RoutedRPCData data, ServersideQoLZDO zdo, float health)
  {
    if (Instance<PlayerRegistryProcessor>().GetStateForPeerID(data.m_senderPeerID) is not { } playerState)
      return;

    var skill = playerState.GetEstimatedSkillLevel(SkillType.Crafting);
    if (float.IsNaN(skill))
      return;

    if (health < zdo.Fields<WearNTear>().GetFloat(static () => x => x.m_health))
      return;

    if (Config.Instance.Crafting.HealthMultiplierEnabled && GetProcessorPrefabInfo(zdo) is { } prefabInfo)
    {
      if (zdo.Fields<WearNTear>().UpdateValue(static () => x => x.m_health, GetMaxHealth(skill, prefabInfo)))
      {
        zdo.Vars.RemoveHealth();
        zdo = zdo.Recreate();
        _pieces?.TryAdd(zdo);
      }
    }

    if (_pieces is null)
      return;

    //if (playerState.LastUsedItem is not { m_itemData.m_shared.m_skillType: global::Skills.SkillType.Crafting })
    //  return;

    var now = Timestamp.Now;
    if (now > _clearIgnoredAfter)
      _ignore.Clear();
    else if (_ignore.Remove(zdo))
      return;

    var rangeSqr = Utils.Lerp(Config.Instance.Crafting.AreaRepairRangeAtMinSkill.Value, Config.Instance.Crafting.AreaRepairRangeAtMaxSkill.Value, skill);
    if (!(rangeSqr > 0))
      return;
    rangeSqr *= rangeSqr;

    var pos = zdo.ZDO.GetPosition();
    var count = 0;
    foreach (var pieces in _pieces.EnumerateAdjacent(pos))
    {
      foreach (var piece in pieces)
      {
        if (piece == zdo || Utils.DistanceSqr(pos, piece.ZDO.GetPosition()) > rangeSqr || piece.Vars.GetHealth() >= piece.Fields<WearNTear>().GetFloat(static () => x => x.m_health))
          continue;
        if (++count > Config.Instance.Advanced.Value.Crafting.AreaRepair.MaxRepairedAtOnce)
        {
          Logger.LogWarning($"AreaRepair exceeded max piece count ({Config.Instance.Advanced.Value.Crafting.AreaRepair.MaxRepairedAtOnce}) and is aborted. Player: {playerState.PlayerName}, Skill: {skill * 100:F0}, Range: {Mathf.Sqrt(rangeSqr)}");
          return;
        }
        _ignore.Add(piece);
        _clearIgnoredAfter = now.AddSeconds(Config.Instance.Advanced.Value.Crafting.AreaRepair.IgnoreRepairedForSeconds);
        piece.RPC.WearNTear.Repair();
      }
    }

    Logger.DevLog($"{count} pieces repaired via area repair. Player: {playerState.PlayerName}, Skill: {skill * 100:F0}, Range: {Mathf.Sqrt(rangeSqr)}", BepInEx.Logging.LogLevel.Info);
  }
}
