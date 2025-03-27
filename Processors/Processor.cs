using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

abstract class Processor(ManualLogSource logger, ModConfig cfg)
{
    static IReadOnlyList<Processor>? _defaultProcessors;
    public static IReadOnlyList<Processor> DefaultProcessors => _defaultProcessors ??= typeof(Processor).Assembly.GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(Processor).IsAssignableFrom(x))
            .Select(x => (Processor)Activator.CreateInstance(x, args: [Main.Logger, Main.Config]))
            .ToList();

    protected ManualLogSource Logger { get; } = logger;
    protected ModConfig Config { get; } = cfg;

    public bool DestroyZdo { get; protected set; }
    public bool RecreateZdo { get; protected set; }
    public bool UnregisterZdoProcessor { get; protected set; }

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

    protected abstract bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers);
    public void Process(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        _watch.Start();

        DestroyZdo = false;
        RecreateZdo = false;
        UnregisterZdoProcessor = false;

        if (zdo.CheckProcessorDataRevisionChanged(this))
        {
            if (ProcessCore(zdo, peers))
                zdo.UpdateProcessorDataRevision(this);
        }

        _watch.Stop();
    }

    protected bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo)
        => CheckMinDistance(peers, zdo, Config.General.MinPlayerDistance.Value);

    protected static bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo, float minDistance)
        => peers.Min(x => Utils.DistanceSqr(x.m_refPos, zdo.GetPosition())) >= minDistance * minDistance;

    protected static class RPC
    {
        public static void ShowMessage(IEnumerable<ZNetPeer> peers, MessageHud.MessageType type, string message)
        {
            /// Invoke <see cref="MessageHud.RPC_ShowMessage"/>
            foreach (var peer in peers)
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "ShowMessage", (int)type, message);
        }

        public static void UseStamina(ExtendedZDO playerZdo, float value)
        {
            /// <see cref="Player.UseStamina(float)"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(playerZdo.GetOwner(), playerZdo.m_uid, "UseStamina", value);
        }
    }
}
