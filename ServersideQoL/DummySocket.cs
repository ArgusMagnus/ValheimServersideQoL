namespace Valheim.ServersideQoL;

sealed class DummySocket : ISocket
{
    public ISocket Accept()
    {
        throw new NotImplementedException();
    }

    public void Close() { }

    public void Dispose() { }

    public bool Flush()
    {
        throw new NotImplementedException();
    }

    public void GetAndResetStats(out int totalSent, out int totalRecv)
    {
        throw new NotImplementedException();
    }

    public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
    {
        throw new NotImplementedException();
    }

    public int GetCurrentSendRate()
    {
        throw new NotImplementedException();
    }

    public string GetEndPointString()
    {
        throw new NotImplementedException();
    }

    public string GetHostName() => "";

    public int GetHostPort()
    {
        throw new NotImplementedException();
    }

    public int GetSendQueueSize()
    {
        throw new NotImplementedException();
    }

    public bool GotNewData()
    {
        throw new NotImplementedException();
    }

    public bool IsConnected() => true;

    public bool IsHost()
    {
        throw new NotImplementedException();
    }

    public ZPackage Recv()
    {
        throw new NotImplementedException();
    }

    public void Send(ZPackage pkg)
    {
        throw new NotImplementedException();
    }

    public void VersionMatch()
    {
        throw new NotImplementedException();
    }
}