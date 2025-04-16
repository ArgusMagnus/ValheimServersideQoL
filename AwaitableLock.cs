using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Valheim.ServersideQoL;

sealed class AwaitableLock
{
    int _waiting;
    AsyncLocal<int> _allowReentry = new();
    readonly ConcurrentQueue<Action> _continuations = new();
    static readonly ConditionalWeakTable<object, AwaitableLock> __locks = new();

    public HandleAwaitable Acquire() => new(this);
    public static HandleAwaitable Acquire<T>(T obj) where T : class => __locks.GetOrCreateValue(obj).Acquire();

    public readonly struct HandleAwaitable : INotifyCompletion
    {
        readonly AwaitableLock _lock;

        public Handle GetResult() => new Handle(_lock);
        public bool IsCompleted { get; }

        internal HandleAwaitable(AwaitableLock asyncLock)
        {
            _lock = asyncLock;
            IsCompleted = Interlocked.Increment(ref _lock._waiting) == 1 || _lock._allowReentry.Value > 0;
        }

        void INotifyCompletion.OnCompleted(Action continuation)
        {
            _lock._continuations.Enqueue(GetContinuationScheduler(continuation, SynchronizationContext.Current));
        }

        public HandleAwaitable GetAwaiter() => this;

        static Action GetContinuationScheduler(Action continuation, SynchronizationContext? context)
        {
            // Get the current SynchronizationContext, and if there is one,
            // post the continuation to it.  However, treat the base type
            // as if there wasn't a SynchronizationContext, since that's what it
            // logically represents.
            if (context is not null && context.GetType() == typeof(SynchronizationContext))
                return () => context.Post(static x => ((Action)x).Invoke(), continuation);

            // If we're targeting the default scheduler, queue to the thread pool, so that we go into the global
            // queue.  As we're going into the global queue, we might as well use QUWI, which for the global queue is
            // just a tad faster than task, due to a smaller object getting allocated and less work on the execution path.
            var scheduler = TaskScheduler.Current;
            if (scheduler == TaskScheduler.Default)
                return () => ThreadPool.QueueUserWorkItem(static x => ((Action)x).Invoke(), continuation);
            else
                return () => Task.Factory.StartNew(continuation, default, TaskCreationOptions.PreferFairness, scheduler);
        }
    }

    public readonly struct Handle : IDisposable
    {
        readonly AwaitableLock _lock;

        internal Handle(AwaitableLock asyncLock)
        {
            _lock = asyncLock;
            _lock._allowReentry.Value++;
        }

        void IDisposable.Dispose()
        {
            _lock._allowReentry.Value--;
            if (Interlocked.Decrement(ref _lock._waiting) > 0 && _lock._allowReentry.Value is 0)
            {
                // if _lock._waiting > 0 there must always be a continuation, if there isn't, it's because it has not been inserted yet
                while (true)
                {
                    if (_lock._continuations.TryDequeue(out var continuation))
                    {
                        continuation();
                        break;
                    }
                }
            }
        }
    }
}