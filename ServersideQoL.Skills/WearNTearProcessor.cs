using ServersideQoL.Processors;
using ServersideQoL.Utilities;

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

    Logger.DevLog($"Max range: {range}");

    RPC.Intercept.UpdateInterception(RPC.RpcName.WearNTear.HealthChanged, RPC_HealthChanged, range > 0);
    Instance<PlayerRegistryProcessor>().EnableSkillLevelEstimation(range > 0);
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (_pieces is not null && zdo.Vars.GetCreator().Value is not 0)
      _pieces.TryAdd(zdo);
    return ProcessResult.UnregisterProcessor;
  }

  void RPC_HealthChanged(ZRoutedRpc.RoutedRPCData data, ServersideQoLZDO zdo, float health)
  {
    if (Instance<PlayerRegistryProcessor>().GetStateForPeerID(data.m_senderPeerID) is not { } playerState || _pieces is null)
      return;
    //if (playerState.LastUsedItem is not { m_itemData.m_shared.m_skillType: global::Skills.SkillType.Crafting })
    //  return;
    if (Timestamp.Now > _clearIgnoredAfter)
      _ignore.Clear();
    else if (_ignore.Remove(zdo))
      return;
    if (health < zdo.Fields<WearNTear>().GetFloat(static () => x => x.m_health))
      return;

    var skill = playerState.GetEstimatedSkillLevel(global::Skills.SkillType.Crafting);
    var rangeSqr = Utils.Lerp(Config.Instance.Crafting.AreaRepairRangeAtMinSkill.Value, Config.Instance.Crafting.AreaRepairRangeAtMaxSkill.Value, skill);
    if (!(rangeSqr > 0))
      return;
    rangeSqr *= rangeSqr;

    var pos = zdo.ZDO.GetPosition();
    foreach (var pieces in _pieces.EnumerateAdjacent(pos))
    {
      foreach (var piece in pieces)
      {
        if (Utils.DistanceSqr(pos, piece.ZDO.GetPosition()) > rangeSqr)
          continue;
        _ignore.Add(piece);
        _clearIgnoredAfter = Timestamp.Now.AddSeconds(2);
        piece.RPC.WearNTear.Repair();
      }
    }
  }
}
