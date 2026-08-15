using ServersideQoL.Processors;
using ServersideQoL.Utilities;

namespace ServersideQoL.WorldOptions;

[Processor(Id)]
[DependsOn<PlayerRegistryProcessor>]
public sealed class TraderProcessor : Processor<ProcessorPrefabInfo<Trader>>
{
  public const string Id = "a4dbfa59-0d8c-4910-8f02-5097a0449786";

  readonly record struct GlobalKeyModification(GlobalKey Key, bool Add);

  readonly Dictionary<Trader, IReadOnlyList<GlobalKeyModification>> _globalKeyModifications = [];

  protected override void Initialize()
  {
    ServersideQoLPlugin.Instance.GlobalKeysChanged -= OnGlobalKeysChanged;
    OnGlobalKeysChanged();
    ServersideQoLPlugin.Instance.GlobalKeysChanged += OnGlobalKeysChanged;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<Trader> prefabInfo)
  {
    if (!_globalKeyModifications.TryGetValue(prefabInfo.Component, out var modifications))
      return ScheduleReprocessing();

    var minDistSqr = prefabInfo.Component.m_standRange * prefabInfo.Component.m_standRange;

    foreach (var peer in peers.Enumerate())
    {
      if (peer.PlayerState is not { } playerState)
        continue;

      if (Utils.DistanceSqr(peer.RefPos, zdo.ZDO.GetPosition()) < minDistSqr)
      {
        foreach (var (key, add) in modifications)
          playerState.AddGlobalKeyModification(key, add);
      }
      else
      {
        foreach (var (key, _) in modifications)
          playerState.RemoveGlobalKeyModification(key);
      }
    }

    return ScheduleReprocessing();
  }

  void OnGlobalKeysChanged()
  {
    if (!Config.Instance.Enabled.Value)
    {
      ServersideQoLPlugin.Instance.GlobalKeysChanged -= OnGlobalKeysChanged;
      return;
    }

    _globalKeyModifications.Clear();
    foreach (var (trader, cfgList) in Config.Instance.TaderProgressRequirements)
    {
      List<GlobalKeyModification>? modifications = null;
      foreach (var entry in cfgList)
      {
        if (entry.Value.Equals(entry.BoxedValue))
          continue;

        var key = (string)entry.DefaultValue;
        var isSet = ZoneSystem.instance.GetGlobalKey(key);
        if (isSet == (entry.Value is Config.GlobalKeyNone || ZoneSystem.instance.GetGlobalKey(entry.Value)))
          continue;

        var add = !isSet;

        if (modifications is null)
          modifications = [new(new(key), add)];
        else
        {
          var idx = modifications.FindIndex(x => x.Key == key);
          if (idx < 0)
            modifications.Add(new(new(key), add));
          else if (modifications[idx].Add != add)
          {
            Logger.LogWarning($"[{entry.Definition.Section}].[{entry.Definition.Key}]: Conflicting requirements for global key {key}");
            if (add)
              modifications[idx] = new(new(key), add);
          }
        }
      }

      if (modifications is not null)
        _globalKeyModifications.Add(trader, modifications);
    }
  }
}
