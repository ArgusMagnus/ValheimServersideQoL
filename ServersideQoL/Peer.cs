using System.Runtime.CompilerServices;
using UnityEngine;

namespace ServersideQoL;

public sealed class Peer
{
  readonly ZNetPeer _peer;
  public long m_uid => _peer.m_uid;

  public ServersideQoLZDO? CharacterZDO
  {
    get
    {
      if (field is null)
      {
        field = ZDOMan.instance.GetZDO(_peer.m_characterID)?.ServersideQoLZDO;
        field?.Destroyed += OnCharacterZDODestroyed;
      }

      return field;

      void OnCharacterZDODestroyed(ServersideQoLZDO zdo)
      {
        zdo.Destroyed -= OnCharacterZDODestroyed;
        field = null;
      }
    }
  }

  public Vector3 m_refPos => CharacterZDO?.ZDO.GetPosition() ?? _peer.m_refPos;
  public ZDOID m_characterID => _peer.m_characterID;
  //public bool IsConnected => _peer?.m_socket.IsConnected() ?? default; // potentially takes a long time?
  public bool IsServer => _peer.m_server;
  public string GetHostName() => _peer.m_socket.GetHostName() ?? "";
  public IReadOnlyDictionary<string, string> m_serverSyncedPlayerData => _peer is { m_server: false } ? _peer.m_serverSyncedPlayerData : ZNet.instance.m_serverSyncedPlayerData;

  public override bool Equals(object obj) => Equals(_peer, obj);
  public override int GetHashCode() => _peer.GetHashCode();

  static readonly ConditionalWeakTable<ZNetPeer, Peer> _cache = [];

  private Peer(ZNetPeer peer) => _peer = peer;

  public static Peer Get(ZNetPeer peer) => _cache.GetValue(peer, static x => new(x));
}
