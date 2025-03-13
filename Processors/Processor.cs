using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

abstract class Processor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState)
{
    public static IReadOnlyList<Processor> CreateInstances(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState)
    {
        return typeof(Processor).Assembly.GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(Processor).IsAssignableFrom(x))
            .Select(x => (Processor)Activator.CreateInstance(x, args: [logger, cfg, sharedState]))
            .ToList();
    }

    protected ManualLogSource Logger { get; } = logger;
    protected ModConfig Config { get; } = cfg;
    protected SharedProcessorState SharedState { get; } = sharedState;

    readonly System.Diagnostics.Stopwatch _watch = new();

    public TimeSpan ProcessingTime => _watch.Elapsed;
    long _totalProcessingTimeTicks;
    public TimeSpan TotalProcessingTime => new(_totalProcessingTimeTicks + _watch.ElapsedTicks);

    public virtual void Initialize() { }
    public virtual void PreProcess()
    {
        _totalProcessingTimeTicks += _watch.ElapsedTicks;
        _watch.Reset();
    }

    protected abstract void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers);
    public void Process(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        _watch.Start();
        ProcessCore(ref zdo, prefabInfo, peers);
        _watch.Stop();
    }

    protected bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo)
        => CheckMinDistance(peers, zdo, Config.General.MinPlayerDistance.Value);

    protected bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo, float minDistance)
        => peers.Min(x => Utils.DistanceSqr(x.m_refPos, zdo.GetPosition())) >= minDistance * minDistance;
}
