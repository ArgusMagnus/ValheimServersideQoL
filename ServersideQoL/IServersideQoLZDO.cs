using ServersideQoL.ZDOExtender;

namespace ServersideQoL;

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
