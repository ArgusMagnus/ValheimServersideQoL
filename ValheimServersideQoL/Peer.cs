using System.Runtime.CompilerServices;
using UnityEngine;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

sealed class Peer
{
    readonly ZNetPeer _peer;
    public long m_uid => _peer.m_uid;
    public Vector3 m_refPos => Info?.PlayerZDO.GetPosition() ?? _peer.m_refPos;
    public ZDOID m_characterID => _peer.m_characterID;
    //public bool IsConnected => _peer?.m_socket.IsConnected() ?? default; // potentially takes a long time?
    public bool IsServer => _peer.m_server;
    public string GetHostName() => _peer.m_socket.GetHostName() ?? "";
    public IReadOnlyDictionary<string, string> m_serverSyncedPlayerData => _peer is { m_server: false } ? _peer.m_serverSyncedPlayerData : ZNet.instance.m_serverSyncedPlayerData;

    public override bool Equals(object obj) => Equals(_peer, obj);
    public override int GetHashCode() => _peer.GetHashCode();

    public PlayerProcessor.IPeerInfo? Info => field ??= Processor.Instance<PlayerProcessor>().GetPeerInfo(_peer.m_uid);

    static readonly ConditionalWeakTable<ZNetPeer, Peer> _cache = [];

    private Peer(ZNetPeer peer) => _peer = peer;

    public static Peer Get(ZNetPeer peer) => _cache.GetValue(peer, static x => new(x));
}