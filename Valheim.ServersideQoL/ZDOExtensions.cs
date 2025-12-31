using Valheim.ZDOExtender;

namespace Valheim.ServersideQoL;

public static partial class ZDOExtensions
{
    static readonly Dictionary<int, IReadOnlyList<Processor>> _processors = [];

    extension(ZDO @this)
    {
        public IReadOnlyList<Processor> GetProcessors()
                    {
            var extZdo = @this.GetExtension<IServersideQoLZDO>();
            if (extZdo.Processors is not { } processors)
            {
                extZdo.Processors = processors = ServersideQoL.Processors;
                extZdo.HasNoProcessors = processors.Count is 0;
            }
            return extZdo.Processors;
        }

        public void Ungregister(IReadOnlyList<Processor> processors)
        {
            var extZdo = @this.GetExtension<IServersideQoLZDO>();
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
            var extZdo = @this.GetExtension<IServersideQoLZDO>();
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
            var extZdo = @this.GetExtension<IServersideQoLZDO>();
            extZdo.Processors = [];
            extZdo.HasNoProcessors = true;
        }

        public void ReregisterAll()
        {
            var extZdo = @this.GetExtension<IServersideQoLZDO>();
            var processors = ServersideQoL.Processors;
            extZdo.Processors = processors;
            extZdo.HasNoProcessors = processors.Count is 0;
        }

        public void Reregister(IReadOnlyList<Processor> processors)
        {
            throw new NotImplementedException();
        }


        public void UpdateProcessorDataRevision(Processor processor)
            => (@this.GetExtension<IServersideQoLZDO>().ProcessorDataRevisions ??= [])[processor] = (@this.DataRevision, @this.OwnerRevision);

        public void ResetProcessorDataRevision(Processor processor)
            => @this.GetExtension<IServersideQoLZDO>().ProcessorDataRevisions?.Remove(processor);

        public bool CheckProcessorDataRevisionChanged(Processor processor)
        {
            var dataRevisions = @this.GetExtension<IServersideQoLZDO>().ProcessorDataRevisions;
            if (dataRevisions is null || !dataRevisions.TryGetValue(processor, out var revision) || revision != (@this.DataRevision, @this.OwnerRevision))
                return true;
            return false;
        }

        public void Destroy()
        {
            @this.ClaimOwnershipInternal();
            ZDOMan.instance.DestroyZDO(@this);
        }

        public ZDO CreateClone()
        {
            var prefab = @this.GetPrefab();
            var pos = @this.GetPosition();
            var owner = @this.GetOwner();
            var pkg = new ZPackage();
            @this.Serialize(pkg);

            var zdo = ZDOMan.instance.CreateNewZDO(pos, prefab);
            zdo.Deserialize(new(pkg.GetArray()));
            zdo.SetOwnerInternal(owner);
            return zdo;
        }

        public ZDO Recreate()
        {
            var zdo = @this.CreateClone();

            // Call before Destroy and thus before ZDOMan.instance.m_onZDODestroyed
            //_addData?.Recreated?.Invoke(this, zdo);

            @this.Destroy();
            return zdo;
        }

        public void ClaimOwnership() => @this.SetOwner(ZDOMan.GetSessionID());
        public void ClaimOwnershipInternal() => @this.SetOwnerInternal(ZDOMan.GetSessionID());
        public void ReleaseOwnership() => @this.SetOwner(0);
        public void ReleaseOwnershipInternal() => @this.SetOwnerInternal(0);

        public bool IsOwnerOrUnassigned() => !@this.HasOwner() || @this.IsOwner();

        public void SetModAsCreator(Processor.CreatorMarkers marker = Processor.CreatorMarkers.None) => @this.Vars.SetCreator((long)ServersideQoL.PluginGuidHash | (long)((ulong)marker << 32));
        public bool IsModCreator(out Processor.CreatorMarkers marker)
        {
            marker = Processor.CreatorMarkers.None;
            if ((int)@this.Vars.GetCreator() != ServersideQoL.PluginGuidHash)
                return false;
            marker = (Processor.CreatorMarkers)((ulong)@this.Vars.GetCreator() >> 32);
            return true;
        }
        public bool IsModCreator() => @this.IsModCreator(out _);
    }
}
