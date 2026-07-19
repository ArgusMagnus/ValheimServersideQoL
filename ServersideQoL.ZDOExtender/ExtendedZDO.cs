namespace ServersideQoL.ZDOExtender;

public abstract class ExtendedZDO : ZDO, IExtendedZDO
{
    internal int _prevPrefab;
    ZDO IExtendedZDO.ZDO => this;

    internal ZDOEventHandler? _destroyed;
    public event ZDOEventHandler? Destroyed
    {
        add
        {
            IExtendedZDO.Events.EnsureDestroyedInitialized();
            _destroyed += value;
        }
        remove => _destroyed -= value;
    }
}
