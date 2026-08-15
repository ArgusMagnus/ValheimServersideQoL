namespace ServersideQoL.Processors;

public abstract class TameableState
{
  private protected TameableState() { }

  public abstract TameableRegistryProcessor.PrefabInfo PrefabInfo { get; }
  public abstract ServersideQoLZDO ZDO { get; }
  public abstract States State { get; }
  public abstract string FollowPlayerName { get; }

  public enum States
  {
    Wild,
    Taming,
    Tamed
  }
}
