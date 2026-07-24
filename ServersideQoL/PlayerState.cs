namespace ServersideQoL;

public abstract class PlayerState
{
  private protected PlayerState() { }

  public abstract ServersideQoLZDO ZDO { get; }
}
