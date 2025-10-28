using System.Collections.Concurrent;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

partial class ExtendedZDO
{
    sealed class AdditionalData(PrefabInfo prefabInfo)
    {
        public bool HasProcessors { get; private set; } = true;
        public IReadOnlyList<Processor> Processors
        {
            get => field ??= Processor.DefaultProcessors;
            private set { field = value; HasProcessors = value.Count > 0; }
        }

        public PrefabInfo PrefabInfo { get; } = prefabInfo;
        public ConcurrentDictionary<Type, object>? ComponentFieldAccessors { get; set; }
        public Dictionary<Processor, (uint Data, uint Owner)>? ProcessorDataRevisions { get; set; }
        public ZDOInventory? Inventory { get; set; }
        public bool? HasFields { get; set; }
        public RecreateHandler? Recreated { get; set; }
        public Action<ExtendedZDO>? Destroyed { get; set; }

        static readonly Dictionary<int, IReadOnlyList<Processor>> _processors = [];

        public void Ungregister(IReadOnlyList<Processor> processors)
        {
            var hash = 0;
            foreach (var processor in Processors.AsEnumerable())
            {
                var keep = true;
                foreach (var remove in processors.AsEnumerable())
                {
                    if (ReferenceEquals(processor, remove))
                    {
                        keep = false;
                        break;
                    }
                }
                if (keep)
                    hash = (hash, processor.GetType()).GetHashCode();
            }

            if (!_processors.TryGetValue(hash, out var newProcessors))
            {
                var list = new List<Processor>();
                _processors.Add(hash, newProcessors = list);
                foreach (var processor in Processors.AsEnumerable())
                {
                    var keep = true;
                    foreach (var remove in processors.AsEnumerable())
                    {
                        if (ReferenceEquals(processor, remove))
                        {
                            keep = false;
                            break;
                        }
                    }
                    if (keep)
                        list.Add(processor);
                }
            }

            Processors = newProcessors;

            if (ProcessorDataRevisions is not null)
            {
                foreach (var processor in processors.AsEnumerable())
                    ProcessorDataRevisions.Remove(processor);
            }
        }

        public void UnregisterAllExcept(Processor keep)
        {
            var hash = (0, keep.GetType()).GetHashCode();
            if (!_processors.TryGetValue(hash, out var processors))
                _processors.Add(hash, processors = [keep]);
            if (ProcessorDataRevisions is not null)
            {
                foreach (var processor in Processors.AsEnumerable())
                {
                    if (!ReferenceEquals(processor, keep))
                        ProcessorDataRevisions.Remove(processor);
                }
            }
            Processors = processors;
            return;
        }

        public void UnregisterAll() => Processors = [];
        public void ReregisterAll() => Processors = Processor.DefaultProcessors;

        public static AdditionalData Dummy { get; } = new(PrefabInfo.Dummy);
    }
}
