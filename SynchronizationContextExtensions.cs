using System.Runtime.CompilerServices;

namespace Valheim.ServersideQoL;

static class SynchronizationContextExtensions
{
    public readonly struct SynchronizationContextAwaiter : INotifyCompletion
    {
        readonly SynchronizationContext? _context;
        public void GetResult() { }
        public bool IsCompleted => _context == null || _context == SynchronizationContext.Current;
        public void OnCompleted(Action continuation)
        {
            if (continuation == null)
                throw new ArgumentNullException(nameof(continuation));
            _context!.Post(static x => ((Action)x).Invoke(), continuation);
        }

        internal SynchronizationContextAwaiter(SynchronizationContext? context) => _context = context;
    }

    public static SynchronizationContextAwaiter GetAwaiter(this SynchronizationContext? context) => new(context);
}