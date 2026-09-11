using static ServersideQoL.PrefabConfigurator.Config.BuildPiecesConfig;

namespace ServersideQoL.PrefabConfigurator;

[Processor(Id)]
[RunBefore<PrefabProcessor>]
public sealed class BuildPieceProcessor : Processor<BuildPieceProcessor.PrefabInfo>
{
  public const string Id = "3a6fd652-03b4-4b37-b031-fba0f999a6e7";
  public sealed record PrefabInfo(WearNTear WearNTear, Piece? Piece, PieceTable? PieceTable) : ProcessorPrefabInfo;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    var result = ProcessResult.UnregisterProcessor;
    var fields = zdo.Fields<WearNTear>();
    var isPlayerBuilt = prefabInfo is { Piece: not null, PieceTable: not null } && zdo.Vars.GetCreator() != default;
    if (isPlayerBuilt)
    {
      if (!Config.Instance.BuildPieces.DisableRainDamage.Value)
        fields.Reset(static () => x => x.m_noRoofWear);
      else if (fields.UpdateValue(static () => x => x.m_noRoofWear, false))
        result |= ProcessResult.RecreateZDO;

      if (!Config.Instance.BuildPieces.MakeIndestructible.Value)
      {
        if (fields.UpdateResetValue(static () => x => x.m_health))
          zdo.Vars.RemoveHealth();
      }
      else if (fields.UpdateValue(static () => x => x.m_health, -1))
      {
        zdo.Vars.SetHealth(-1);
        result |= ProcessResult.RecreateZDO;
      }
    }

    var disableSupportRequirements = isPlayerBuilt ?
      (Config.Instance.BuildPieces.DisableSupportRequirements.Value & DisableSupportRequirementsOptions.PlayerBuilt) is not 0 :
      (Config.Instance.BuildPieces.DisableSupportRequirements.Value & DisableSupportRequirementsOptions.World) is not 0;

    if (!disableSupportRequirements)
      fields.Reset(static () => x => x.m_noSupportWear);
    else if (fields.UpdateValue(static () => x => x.m_noSupportWear, false))
      result |= ProcessResult.RecreateZDO;

    return result;
  }
}
