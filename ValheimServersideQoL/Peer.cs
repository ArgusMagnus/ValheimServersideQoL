using UnityEngine;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

struct Peer(ZNetPeer peer)
{
    readonly ZNetPeer? _peer = peer;
    public readonly long m_uid => _peer?.m_uid ?? default;
    public Vector3 m_refPos => Info?.PlayerZDO.GetPosition() ?? _peer?.m_refPos ?? default;
    public readonly ZDOID m_characterID => _peer?.m_characterID ?? default;
    //public readonly bool IsConnected => _peer?.m_socket.IsConnected() ?? default; // potentially takes a long time?
    public readonly bool IsServer => _peer?.m_server ?? false;
    public readonly string GetHostName() => _peer?.m_socket.GetHostName() ?? "";
    public readonly IReadOnlyDictionary<string, string> m_serverSyncedPlayerData => _peer is { m_server: false } ? _peer.m_serverSyncedPlayerData : ZNet.instance.m_serverSyncedPlayerData;

    public readonly override bool Equals(object obj) => Equals(_peer, obj);
    public readonly override int GetHashCode() => _peer?.GetHashCode() ?? default;
    public readonly bool IsDefault => _peer is null;

    public PlayerProcessor.IPeerInfo? Info => _peer is null ? null : field ??= Processor.Instance<PlayerProcessor>().GetPeerInfo(_peer.m_uid);
}