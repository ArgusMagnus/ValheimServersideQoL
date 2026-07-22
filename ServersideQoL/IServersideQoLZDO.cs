using ServersideQoL.ZDOExtender;

namespace ServersideQoL;

public sealed class ServersideQoLZDO(ZDO zdo)
{
  public ZDO ZDO { get; } = zdo;
  public PrefabInfo? PrefabInfo { get; internal set; }
  internal bool HasNoProcessors { get; set; }
  internal IReadOnlyList<Processor>? Processors { get; set; }
  internal bool HasNoCyclicProcessors { get; set; }
  internal IReadOnlyList<Processor>? CyclicProcessors { get; set; }
  internal Dictionary<Processor, (uint Data, uint Owner)>? ProcessorDataRevisions { get; set; }
  internal bool HasFields { get; set; }
  internal Dictionary<Type, object>? ComponentFieldAccessors { get; set; }
}

interface IServersideQoLZDO : IExtendedZDO
{
  PrefabInfo? PrefabInfo { get; set; }
  bool HasNoProcessors { get; set; }
  IReadOnlyList<Processor>? Processors { get; set; }
  bool HasNoCyclicProcessors { get; set; }
  IReadOnlyList<Processor>? CyclicProcessors { get; set; }
  Dictionary<Processor, (uint Data, uint Owner)>? ProcessorDataRevisions { get; set; }
  bool HasFields { get; set; }
  Dictionary<Type, object>? ComponentFieldAccessors { get; set; }
}
