using UnityEngine;

namespace Valheim.ServersideQoL;

record PrefabInfo(GameObject Prefab, IReadOnlyDictionary<Type, Component> Components)
{
    static T? Get<T>(IReadOnlyDictionary<Type, Component> prefabs) where T : Component => prefabs.TryGetValue(typeof(T), out var value) ? (T)value : null;
    public Sign? Sign { get; } = Get<Sign>(Components);
    public MapTable? MapTable { get; } = Get<MapTable>(Components);
    public Tameable? Tameable { get; } = Get<Tameable>(Components);
    public Character? Character { get; } = Get<Character>(Components);
    public Fireplace? Fireplace { get; } = Get<Fireplace>(Components);
    public Container? Container { get; } = Get<Container>(Components);
    public Ship? Ship { get; } = Get<Ship>(Components);
    public ItemDrop? ItemDrop { get; } = Get<ItemDrop>(Components);
    public Piece? Piece { get; } = Get<Piece>(Components);
    public Smelter? Smelter { get; } = Get<Smelter>(Components);
    public Windmill? Windmill { get; } = Get<Windmill>(Components);
    public Vagon? Vagon { get; } = Get<Vagon>(Components);
    public Player? Player { get; } = Get<Player>(Components);
    public TeleportWorld? TeleportWorld { get; } = Get<TeleportWorld>(Components);
}
