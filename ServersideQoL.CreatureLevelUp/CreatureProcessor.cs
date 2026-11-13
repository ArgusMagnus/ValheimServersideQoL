using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.CreatureLevelUp;

[Processor("0d2b80d6-03bc-49d7-87e0-6b97bd10925e")]
[RunAfter<CreatureLevelUpProcessor>]
public sealed class CreatureProcessor : Processor<CreatureProcessor.PrefabInfo>
{
  public const string Id = "0d2b80d6-03bc-49d7-87e0-6b97bd10925e";
  public sealed record PrefabInfo(Character Character, LevelEffects? LevelEffects, ZSyncTransform? ZSyncTransform) : ProcessorPrefabInfo
  {
    public bool SyncsInitialScale { get; } = Character.GetComponent<ZNetView>().m_syncInitialScale;
    public int MaxLevel { get; } = (LevelEffects?.m_levelSetups.Count ?? 0) + 1;
  }

  Dictionary<ServersideQoLZDO, State> _states = [];

  protected override void Initialize()
  {
    _states.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    const float ScaleEpsilon = 1e-5f;
    const float NoiseScale = 1e-3f;

    var level = zdo.Vars.GetLevel();
    if (level <= prefabInfo.MaxLevel)
      return ProcessResult.UnregisterProcessor;

    if (!_states.TryGetValue(zdo, out var state))
    {
      _states.Add(zdo, state = new());
      zdo.Destroyed += zdo => _states.Remove(zdo);
    }
    var levelChanged = state.Level != level;
    state.Level = level;

    var result = ProcessResult.Default;
    if (level > 3 && levelChanged)
    {
      var fields = zdo.Fields<Character>();
      if (!Config.Instance.ShowHigherLevelStars.Value)
        fields.Reset(static () => x => x.m_name);
      else if (fields.UpdateValue(static () => x => x.m_name, Config.Instance.Advanced.Value.HigherLevelStarName(prefabInfo.Character, level)))
        result = ProcessResult.RecreateZDO;
    }

    if (prefabInfo.ZSyncTransform is null)
      return result | ProcessResult.UnregisterProcessor;

    if (levelChanged)
    {
      state.CalculatedScale = Config.Instance.Advanced.Value.ScaleSizeExponentially ?
        Mathf.Pow(1 + Config.Instance.SizeIncreasePerStar.Value, level - 1) :
        (1 + (level - 1) * Config.Instance.SizeIncreasePerStar.Value);
    }

    if (state.CalculatedScale is 1)
      zdo.Fields<ZSyncTransform>().Reset(static () => x => x.m_syncScale);
    else if (zdo.Fields<ZSyncTransform>().UpdateValue(static () => x => x.m_syncScale, true))
      return ProcessResult.RecreateZDO;

    if (prefabInfo.SyncsInitialScale)
    {
      if (SQoLUtils.ApproximatelyEqual(zdo.Vars.GetScaleScalar(), state.CalculatedScale, ScaleEpsilon))
        return ProcessResult.UnregisterProcessor;
      zdo.Vars.RemoveScale();
      zdo.Vars.SetScaleScalar(state.CalculatedScale);
      return ProcessResult.RecreateZDO;
    }

    if (state.CalculatedScale is 1)
      return result | ProcessResult.UnregisterProcessor;

    /// We need to make sure clients called <see cref="ZSyncTransform.ClientSync"/> at least once with our <see cref="ZDOVars.s_scaleScalarHash"/> value
    /// <see cref="ZSyncTransform.OwnerSync"/> will set <see cref="ZDOVars.s_scaleHash"/> if it worked

    if (!zdo.ZDO.HasOwner())
      return result;

    var now = Timestamp.Now;
    if (levelChanged || (state.NextAttempt is not null && state.LastProcessed < now.AddSeconds(-2)))
    {
      Logger.DevLog($"{prefabInfo.PrefabInfo.PrefabName}: Reset");
      state.ScaleConfirmed = false;
      if (state.ExpectedScale is 0)
        state.ExpectedScale = zdo.Vars.GetScale().x;
      float expectedScale;
      do expectedScale = state.CalculatedScale + UnityEngine.Random.Range(-NoiseScale, NoiseScale);
      while (SQoLUtils.ApproximatelyEqual(expectedScale, state.ExpectedScale, ScaleEpsilon));
      state.ExpectedScale = expectedScale;
      state.NextAttempt = default(Timestamp);
    }
    state.LastProcessed = now;

    if (state.ScaleConfirmed)
      return result;

    var actualScale = zdo.Vars.GetScale().x;
    if (SQoLUtils.ApproximatelyEqual(actualScale, state.ExpectedScale, ScaleEpsilon))
    {
      state.ScaleConfirmed = true;
      state.NextAttempt = default(Timestamp);
      Logger.DevLog($"{prefabInfo.PrefabInfo.PrefabName}: Scale confirmed: {actualScale}");
    }
    else if (state.NextAttempt is null)
      state.NextAttempt = now.AddSeconds(2f);
    else if (state.NextAttempt < now)
    {
      Logger.DevLog($"{prefabInfo.PrefabInfo.PrefabName}: Actual={actualScale} Expected={state.ExpectedScale}");
      //zdo.Vars.RemoveScale();
      zdo.Vars.SetScale(Vector3.zero);
      zdo.Vars.SetScaleScalar(state.ExpectedScale);
      zdo.ReleaseOwnership();
      //zdo.ZDO.DataRevision += 100; // necessary?
      //ForceSend(zdo); // necessary?
      state.NextAttempt = null;
    }

    return result;
  }

  sealed class State
  {
    public int Level { get; set; } = -1;
    public Timestamp LastProcessed { get; set; }
    public bool ScaleConfirmed { get; set; }
    public float CalculatedScale { get; set; }
    public float ExpectedScale { get; set; }
    public Timestamp? NextAttempt { get; set; } = default(Timestamp);
  }
}
