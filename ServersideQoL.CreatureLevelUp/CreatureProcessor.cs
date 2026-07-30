namespace ServersideQoL.CreatureLevelUp;

[Processor("0d2b80d6-03bc-49d7-87e0-6b97bd10925e")]
[RunAfter<CreatureLevelUpProcessor>]
public sealed class CreatureProcessor : Processor<CreatureProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Character Character, LevelEffects? LevelEffects, ZSyncTransform? ZSyncTransform) : ProcessorPrefabInfo
  {
    public bool SyncsInitialScale { get; } = Character.GetComponent<ZNetView>().m_syncInitialScale;
  }

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
    var level = zdo.Vars.GetLevel();
    if (level <= 1)
      return ProcessResult.UnregisterProcessor;

    var maxLevel = (prefabInfo.LevelEffects?.m_levelSetups.Count ?? 0) + 1;
    if (level <= maxLevel)
      return ProcessResult.UnregisterProcessor;

    var result = ProcessResult.Default;
    if (level > 3)
    {
      var fields = zdo.Fields<Character>();
      if (!Config.Instance.ShowHigherLevelStars.Value)
        fields.Reset(static () => x => x.m_name);
      else if (fields.UpdateValue(static () => x => x.m_name, $"<line-height=150%><voffset=-2em>{prefabInfo.Character.m_name}<size=70%><br><color=yellow>{string.Concat(Enumerable.Repeat("⭐", level - 1))}</color></size></voffset></line-height>"))
        result = ProcessResult.RecreateZDO;
    }

    if (prefabInfo.ZSyncTransform is null)
      return result | ProcessResult.UnregisterProcessor;

    if (zdo.Fields<ZSyncTransform>().UpdateValue(static () => x => x.m_syncScale, true))
      return ProcessResult.RecreateZDO;

    var scale = 1 + (level - maxLevel) * Config.Instance.SizeIncreasePerStar.Value;
    if (prefabInfo.SyncsInitialScale)
    {
      if (zdo.ZDO.GetFloat(ZDOVars.s_scaleScalarHash) == scale)
        return ProcessResult.UnregisterProcessor;
      zdo.ZDO.RemoveVec3(ZDOVars.s_scaleHash);
      zdo.ZDO.Set(ZDOVars.s_scaleScalarHash, scale);
      return ProcessResult.RecreateZDO;
    }

    if (scale is 1)
      return result | ProcessResult.UnregisterProcessor;

    _zdos.Add(zdo);
    if (!_zdosPrev.Contains(zdo))
    {
      // not present in previous run
      zdo.ZDO.RemoveVec3(ZDOVars.s_scaleHash);
      _nextCheck = DateTimeOffset.UtcNow.AddSeconds(1);
      //Logger.DevLog($"Releasing ownership");
      zdo.ZDO.Set(ZDOVars.s_scaleScalarHash, scale);
      zdo.ReleaseOwnership();
    }

    result |= ProcessResult.ScheduleReprocessing;

    if (!zdo.ZDO.HasOwner())
      return result;

    if (DateTimeOffset.UtcNow < _nextCheck)
      return result;

    if (!zdo.ZDO.GetVec3(ZDOVars.s_scaleHash, out var scaleVec) || scaleVec.x != scale)
      _zdos.Remove(zdo); // trigger in next run

    return result;
  }
}
