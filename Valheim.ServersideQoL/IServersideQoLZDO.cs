using UnityEngine;
using Valheim.ZDOExtender;

namespace Valheim.ServersideQoL;

interface IServersideQoLZDO : IExtendedZDO
{
    bool HasNoProcessors { get; set; }
    IReadOnlyList<Processor> Processors { get; set; }
    Dictionary<Processor, (uint Data, uint Owner)>? ProcessorDataRevisions { get; set; }
    bool HasFields { get; set; }
    IReadOnlyDictionary<Type, MonoBehaviour>? Components { get; set; }
    Dictionary<Type, object>? ComponentFieldAccessors { get; set; }
}
