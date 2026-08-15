namespace ServersideQoL.Utilities;

public static class SingletonCache<T>
  where T : class, new()
{
  [ThreadStatic]
  static readonly T __instance = new();

  public static T Instance => __instance;
}
