namespace ServersideQoL;

public abstract class PlayerState
{
  private protected PlayerState() { }

  public abstract ServersideQoLZDO ZDO { get; }
  public abstract long PlayerID { get; }
  public abstract string PlayerName { get; }
  public abstract bool IsAdmin { get; }
}
