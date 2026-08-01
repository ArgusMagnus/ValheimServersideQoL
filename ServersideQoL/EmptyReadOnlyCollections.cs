namespace ServersideQoL;

public static class EmptyReadOnlyCollections<TKey, TValue>
{
  public static IReadOnlyDictionary<TKey, TValue> Dictionary { get; } = new System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>(0));
}
