using System.Collections;
using System.Runtime.CompilerServices;

namespace ServersideQoL;

public sealed class PeersEnumerable : IEnumerable<Peer>
{
  public static PeersEnumerable Instance { get => field ??= new(null); private set; }
  readonly ZNetPeer? _localPeer;
  List<ZNetPeer> _peers = [];

  public int Count => _peers.Count + (_localPeer is null ? 0 : 1);

  internal PeersEnumerable(ZNetPeer? localPeer)
  {
    _localPeer = localPeer;
    Instance = this;
  }

  internal void Update()
  {
    if (_localPeer is not null)
    {
      _localPeer.m_refPos = ZNet.instance.GetReferencePosition();
      _localPeer.m_characterID = Player.m_localPlayer?.GetZDOID() ?? ZDOID.None;
    }
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
    Peer? _current;
    public readonly Peer Current => _current!;
    readonly object IEnumerator.Current => _current!;
    readonly void IDisposable.Dispose() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
      if (++_index < 0)
      {
        if (_enumerable._localPeer is null)
          _index++;
        else
        {
          _current = _enumerable._localPeer.ServersideQoLPeer;
          return true;
        }
      }

      while (_index < _enumerable._peers.Count)
      {
        var peer = _enumerable._peers[_index];
        if (peer.IsReady())
        {
          _current = peer.ServersideQoLPeer;
          return true;
        }
        _index++;
      }
      _current = null;
      return false;
    }

    public void Reset()
    {
      _index = -2;
      _current = null;
    }
  }
}
