#if DEBUG
using UnityEngine;

namespace ServersideQoL;

[Processor("66bef8b3-dabf-48f6-a756-955fd999c4e9")]
public sealed class TestProcessor : Processor<TestProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(BaseAI BaseAI, ZSyncTransform ZSyncTransform) : ProcessorPrefabInfo;

  HashSet<ServersideQoLZDO> _zdos = [];
  HashSet<ServersideQoLZDO> _zdosPrev = [];

  DateTimeOffset _nextCheck;

  protected override void PreProcess(PeersEnumerable peers)
  {
    (_zdos, _zdosPrev) = (_zdosPrev, _zdos);
    _zdos.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (zdo.PrefabInfo is not { PrefabName: "Lox" })
      return ProcessResult.UnregisterProcessor;

    if (zdo.PrefabInfo?.Prefab.GetComponent<ZNetView>() is { m_syncInitialScale: true })
      return ProcessResult.UnregisterProcessor;
    if (zdo.Fields<ZSyncTransform>().UpdateValue(static () => x => x.m_syncScale, true))
      return ProcessResult.RecreateZDO;

    // Not too happy with this implementation, but the only thing that worked so far (after logout/login or leaving/entering zones through portal)
    const float Scale = 0.5f;

    _zdos.Add(zdo);

    if (!_zdosPrev.Contains(zdo))
    {
      // not present in previous run
      zdo.ZDO.RemoveVec3(ZDOVars.s_scaleHash);
      _nextCheck = DateTimeOffset.UtcNow.AddSeconds(1);
        Logger.DevLog($"Releasing ownership");
      zdo.ZDO.Set(ZDOVars.s_scaleScalarHash, Scale);
      zdo.ReleaseOwnership();
    }

    if (!zdo.ZDO.HasOwner())
      return ProcessResult.ScheduleReprocessing;

    if (DateTimeOffset.UtcNow < _nextCheck)
      return ProcessResult.ScheduleReprocessing;

    if (!zdo.ZDO.GetVec3(ZDOVars.s_scaleHash, out var scaleVec) || scaleVec.x != Scale)
      _zdos.Remove(zdo); // trigger in next run

    return ProcessResult.ScheduleReprocessing;
  }
}
#endif
