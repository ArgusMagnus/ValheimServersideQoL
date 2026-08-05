using UnityEngine;
using static ServersideQoL.WorldOptions.Config;

namespace ServersideQoL.WorldOptions;

[Processor("edd9c927-c22e-413d-b6d9-a67c0a3268b9")]
public sealed class MisterProcessor : Processor<ProcessorPrefabInfo<Mister>>
{
  const string QueenDefeatedKey = "defeated_queen";
  bool _queenDefeated;
  readonly HashSet<ServersideQoLZDO> _misters = [];

  protected override void Initialize()
  {
    _queenDefeated = ZoneSystem.instance.GetGlobalKey(QueenDefeatedKey);
    _misters.Clear();

    ServersideQoLPlugin.Instance.GlobalKeysChanged -= OnGlobalKeysChanged;
    if (!_queenDefeated)
      ServersideQoLPlugin.Instance.GlobalKeysChanged += OnGlobalKeysChanged;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<Mister> prefabInfo)
  {
    switch (Config.Instance.RemoveMistlandsMist.Value)
    {
      case RemoveMistlandsMistOptions.Never:
        break;

      case RemoveMistlandsMistOptions.Always:
        if (zdo.Fields<Mister>().UpdateValue(static () => x => x.m_radius, float.MinValue))
          return ProcessResult.RecreateZDO | ProcessResult.UnregisterProcessor;
        break;

      case RemoveMistlandsMistOptions.AfterQueenKilled:
        if (_queenDefeated)
        {
          if (zdo.Fields<Mister>().UpdateValue(static () => x => x.m_radius, float.MinValue))
            return ProcessResult.RecreateZDO | ProcessResult.UnregisterProcessor;
          break;
        }
        else
        {
          _misters.Add(zdo);
          return default;
        }
    }

    return ProcessResult.UnregisterProcessor;
  }

  void OnGlobalKeysChanged()
  {
    _queenDefeated = ZoneSystem.instance.GetGlobalKey(QueenDefeatedKey);
    if (!_queenDefeated)
      return;
    ServersideQoLPlugin.Instance.GlobalKeysChanged -= OnGlobalKeysChanged;
    foreach (var zdo in _misters)
      ScheduleReprocessing(zdo);
    _misters.Clear();
  }
}
