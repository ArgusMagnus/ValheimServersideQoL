﻿using UnityEngine;

namespace Valheim.ServersideQoL;

readonly struct Peer(ZNetPeer peer)
{
    readonly ZNetPeer? _peer = peer;
    public long m_uid => _peer?.m_uid ?? default;
    public Vector3 m_refPos => _peer?.m_refPos ?? default;
    public ZDOID m_characterID => _peer?.m_characterID ?? default;
    public bool IsConnected => _peer?.m_socket.IsConnected() ?? default;
    public string GetHostName() => _peer?.m_socket.GetHostName() ?? "";

    public override bool Equals(object obj) => Equals(_peer, obj);
    public override int GetHashCode() => _peer?.GetHashCode() ?? default;
    public bool IsDefault => _peer is null;
}