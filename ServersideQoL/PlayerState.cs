using System.Collections.ObjectModel;

namespace ServersideQoL;

public abstract class PlayerState
{
  private protected PlayerState() { }

  public abstract ServersideQoLZDO ZDO { get; }
  public abstract long PlayerID { get; }
  public abstract string PlayerName { get; }
  public abstract bool IsAdmin { get; }


  public abstract IReadOnlyDictionary<GlobalKey, bool> GlobalKeyModifications { get; }
  public abstract void AddGlobalKeyModification(GlobalKey key, bool add);
  public abstract void RemoveGlobalKeyModification(GlobalKey key);
}
