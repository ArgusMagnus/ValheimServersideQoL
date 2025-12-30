using UnityEngine;

namespace Valheim.ServersideQoL;

public abstract record PrefabInfoBase
{
    public int PrefabHash { get; internal set; }
    public string PrefabName { get; internal set; } = default!;
    public IReadOnlyDictionary<Type, MonoBehaviour> Components { get; internal set; } = default!;
}