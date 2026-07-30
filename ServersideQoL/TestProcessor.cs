#if DEBUG
namespace ServersideQoL;

[Processor("66bef8b3-dabf-48f6-a756-955fd999c4e9")]
public sealed class TestProcessor : Processor<TestProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(BaseAI BaseAI, ZSyncTransform ZSyncTransform) : ProcessorPrefabInfo;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    //if (prefabInfo.ZSyncTransform.m_syncScale)
    //  return ProcessResult.UnregisterProcessor;

    const float Scale = 0.2f;
    zdo.ZDO.RemoveVec3(ZDOVars.s_scaleHash);
    if (zdo.ZDO.GetFloat(ZDOVars.s_scaleScalarHash) != Scale || !zdo.IsOwnerOrUnassigned())
    {
      Logger.DevLog($"Shrinking {zdo.PrefabInfo!.PrefabName}...");
      zdo.ZDO.Set(ZDOVars.s_scaleScalarHash, Scale);
      zdo.ReleaseOwnership();
      zdo.ZDO.DataRevision += 100;
    }

    if (zdo.Fields<ZSyncTransform>().UpdateValue(static () => x => x.m_syncScale, true))
      return ProcessResult.RecreateZDO;

    return default;
  }
}
#endif
