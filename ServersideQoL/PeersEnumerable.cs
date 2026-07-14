using System.Collections;
using System.Runtime.CompilerServices;

namespace Valheim.ServersideQoL;

public sealed class PeersEnumerable(ZNetPeer? localPeer) : IEnumerable<Peer>
{
    readonly ZNetPeer? _localPeer = localPeer;
    List<ZNetPeer> _peers = [];

    public int Count => _peers.Count + (_localPeer is null ? 0 : 1);

    internal void Update()
    {
        if (_localPeer is not null)
            _localPeer.m_refPos = ZNet.instance.GetReferencePosition();
        _peers = ZNet.instance.GetPeers();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<Peer> IEnumerable<Peer>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(PeersEnumerable enumerable) : IEnumerator<Peer>
    {
        readonly PeersEnumerable _enumerable = enumerable;
        int _index = -2;

        public Peer Current { readonly get; private set; } = default!;
        readonly object IEnumerator.Current => Current;

        public void Dispose() => Current = default!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index < 0)
            {
                if (_enumerable._localPeer is null)
                    ++_index;
                else
                {
                    Current = Peer.Get(_enumerable._localPeer);
                    return true;
                }
            }

            if (_index < _enumerable._peers.Count)
            {
                Current = Peer.Get(_enumerable._peers[_index]);
                return true;
            }
            Current = default!;
            return false;
        }

        public void Reset() => _index = -2;
    }
}