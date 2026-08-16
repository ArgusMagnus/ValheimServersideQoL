using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.TameAssist;

[Processor(Id)]
public sealed class GrowingProcessor : Processor<GrowingProcessor.PrefabInfo>
{
  public const string Id = "b8e21b8f-0264-4f45-9b64-b167627a9079";

  public sealed record PrefabInfo(EggGrow? EggGrow, Growup? Growup) : ProcessorPrefabInfo;

  readonly Dictionary<ServersideQoLZDO, State> _states = [];

  static float MessageDelay => DamageText.instance.m_textDuration;

  protected override void Initialize()
  {
    _states.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (Config.Instance.GrowingProgressMessageType.Value is MessageTypes.None)
      return ProcessResult.UnregisterProcessor;

    if (!_states.TryGetValue(zdo, out var state))
    {
      _states.Add(zdo, state = new());
      zdo.Destroyed += x => _states.Remove(x);
    }

    var now = Timestamp.Now;
    var delay = state.NextMessage.Seconds - now.Seconds;
    if (delay > 0)
      return ScheduleReprocessing(delay);

    var growStart = prefabInfo.EggGrow is not null ? zdo.Vars.GetGrowStart() : new TimeSpan(zdo.Vars.GetSpawnTime().Ticks).TotalSeconds;
    if (growStart is 0)
      return default;

    var growUpTime = prefabInfo.EggGrow?.m_growTime ?? prefabInfo.Growup!.m_growTime;
    var growTime = (float)(ZNet.instance.GetTimeSeconds() - growStart);
    var progress = (int)(100 * Mathf.Clamp01(growTime / growUpTime));
    if (state.Progress != progress)
    {
      state.NextMessage = now.AddSeconds(MessageDelay);
      state.Progress = progress;
      ShowMessage(peers, zdo, Config.Instance.Localization.Value.FormatGrowing(progress), Config.Instance.GrowingProgressMessageType.Value);
    }
    return ScheduleReprocessing(MessageDelay);
  }

  sealed class State
  {
    public Timestamp NextMessage { get; set; }
    public int Progress { get; set; } = -1;
  }
}
