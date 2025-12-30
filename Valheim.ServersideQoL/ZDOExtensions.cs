using Valheim.ZDOExtender;

namespace Valheim.ServersideQoL;

public static class ZDOExtensions
{
    static readonly Dictionary<int, IReadOnlyList<Processor>> _processors = [];

    extension(ZDO zdo)
    {
        public IReadOnlyList<Processor> GetProcessors()
                    {
            var extZdo = zdo.GetExtension<IZDOWithProcessors>();
            if (extZdo.Processors is not { } processors)
            {
                extZdo.Processors = processors = ServersideQoL.Processors;
                extZdo.HasNoProcessors = processors.Count is 0;
            }
            return extZdo.Processors;
        }

        public void Ungregister(IReadOnlyList<Processor> processors)
        {
            var extZdo = zdo.GetExtension<IZDOWithProcessors>();
            var zdoProcessors = extZdo.Processors;
            var hash = 0;
            foreach (var processor in zdoProcessors.AsEnumerable())
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
                foreach (var processor in zdoProcessors.AsEnumerable())
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

            extZdo.Processors = newProcessors;
            extZdo.HasNoProcessors = newProcessors.Count is 0;

            if (extZdo.ProcessorDataRevisions is { } dataRevisions)
            {
                foreach (var processor in processors.AsEnumerable())
                    dataRevisions.Remove(processor);
            }
        }

        public void UnregisterAllExcept(Processor keep)
        {
            var extZdo = zdo.GetExtension<IZDOWithProcessors>();
            var zdoProcessors = extZdo.Processors;
            var hash = (0, keep.GetType()).GetHashCode();
            if (!_processors.TryGetValue(hash, out var processors))
                _processors.Add(hash, processors = [keep]);
            if (extZdo.ProcessorDataRevisions is { } dataRevisions)
            {
                foreach (var processor in zdoProcessors.AsEnumerable())
                {
                    if (!ReferenceEquals(processor, keep))
                        dataRevisions.Remove(processor);
                }
            }
            extZdo.Processors = processors;
            extZdo.HasNoProcessors = processors.Count is 0;
            return;
        }

        public void UnregisterAll()
        {
            var extZdo = zdo.GetExtension<IZDOWithProcessors>();
            extZdo.Processors = [];
            extZdo.HasNoProcessors = true;
        }

        public void ReregisterAll()
        {
            var extZdo = zdo.GetExtension<IZDOWithProcessors>();
            var processors = ServersideQoL.Processors;
            extZdo.Processors = processors;
            extZdo.HasNoProcessors = processors.Count is 0;
        }
    }
}
