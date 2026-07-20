namespace ServersideQoL;

public abstract class TameableState
{
    private protected TameableState() { }
    public abstract TameableRegistryProcessor.PrefabInfo PrefabInfo { get; }
    public abstract States State { get; }

    public enum States
    {
        Wild,
        Taming,
        Tamed
    }
}
