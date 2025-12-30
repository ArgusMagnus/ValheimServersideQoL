using Valheim.ZDOExtender;

namespace Valheim.ServersideQoL;

interface IZDOWithProcessors : IExtendedZDO
{
    public bool HasNoProcessors { get; set; }
    public IReadOnlyList<Processor> Processors { get; set; }
    public Dictionary<Processor, (uint Data, uint Owner)>? ProcessorDataRevisions { get; set; }
}
