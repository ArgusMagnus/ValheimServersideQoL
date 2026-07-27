using UnityEngine;

namespace ServersideQoL;

public sealed class Peer(ZNetPeer peer)
{
  public ZNetPeer ZNetPeer { get; } = peer;
  public Vector3 RefPos => PlayerState?.ZDO.ZDO.GetPosition() ?? ZNetPeer.m_refPos;
  public Vector2s GetSector() => ZoneSystem.GetZone(RefPos);
  //public bool IsConnected => _peer?.m_socket.IsConnected() ?? default; // potentially takes a long time?
  public string GetHostName() => ZNetPeer.m_socket.GetHostName() ?? "";
  public IReadOnlyDictionary<string, string> ServerSyncedPlayerData => ZNetPeer is { m_server: false } ? ZNetPeer.m_serverSyncedPlayerData : ZNet.instance.m_serverSyncedPlayerData;

  public override bool Equals(object obj) => Equals(ZNetPeer, obj);
  public override int GetHashCode() => ZNetPeer.GetHashCode();

  public PlayerState? PlayerState
  {
    get
    {
      if (field is null)
      {
        field = Processor.Instance<PlayerRegistryProcessor>().GetStateForPeerID(ZNetPeer.m_uid);
        field?.ZDO.Destroyed += _ => field = null;
      }
      return field;
    }
  }
}
