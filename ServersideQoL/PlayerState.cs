using System.Collections.ObjectModel;

namespace ServersideQoL;

public abstract class PlayerState
{
  private protected PlayerState() { }

  public abstract ServersideQoLZDO ZDO { get; }
  public abstract PlayerRegistryProcessor.PrefabInfo PrefabInfo { get; }
  public abstract long Owner { get; }
  public abstract long PlayerID { get; }
  public abstract string PlayerName { get; }
  public abstract bool IsAdmin { get; }

  /// <summary>
  /// Only updated if there is at least one subscriber to <see cref="PlayerRegistryProcessor.StaminaUpdated"/>
  /// </summary>
  public abstract int Stamina { get; }

  public abstract IReadOnlyDictionary<GlobalKey, bool> GlobalKeyModifications { get; }
  public abstract void AddGlobalKeyModification(GlobalKey key, bool add);
  public abstract void RemoveGlobalKeyModification(GlobalKey key);
}
