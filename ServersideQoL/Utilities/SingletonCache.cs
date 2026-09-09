namespace ServersideQoL.Utilities;

public static class SingletonCache<T>
  where T : class, new()
{
  [ThreadStatic]
  static T? __instance;

  public static T Instance => __instance ??= new();
}
