namespace Valheim.ServersideQoL;

static class ReadOnlyDictionary<TKey, TValue>
{
    public static IReadOnlyDictionary<TKey, TValue> Empty { get; } = new System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>(0));
}
