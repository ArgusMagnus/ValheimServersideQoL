using UnityEngine;

namespace Valheim.ServersideQoL;

sealed class PrefabInfo(IReadOnlyDictionary<Type, MonoBehaviour> components, PieceTable? pieceTable)
{
    public IReadOnlyDictionary<Type, MonoBehaviour> Components { get; } = components;

    static T? Get<T>(IReadOnlyDictionary<Type, MonoBehaviour> prefabs) where T : MonoBehaviour => prefabs.TryGetValue(typeof(T), out var value) ? (T)value : null;
    public Sign? Sign { get; } = Get<Sign>(components);
    public MapTable? MapTable { get; } = Get<MapTable>(components);
    public Tameable? Tameable { get; } = Get<Tameable>(components);
    public Character? Character { get; } = Get<Character>(components);
    public Fireplace? Fireplace { get; } = Get<Fireplace>(components);
    public Container? Container { get; } = Get<Container>(components);
    public Ship? Ship { get; } = Get<Ship>(components);
    public ItemDrop? ItemDrop { get; } = Get<ItemDrop>(components);
    public Piece? Piece { get; } = Get<Piece>(components);
    public PieceTable? PieceTable { get; } = pieceTable;
    public Smelter? Smelter { get; } = Get<Smelter>(components);
    public Windmill? Windmill { get; } = Get<Windmill>(components);
    public Vagon? Vagon { get; } = Get<Vagon>(components);
    public Player? Player { get; } = Get<Player>(components);
    public TeleportWorld? TeleportWorld { get; } = Get<TeleportWorld>(components);
    public Door? Door { get; } = Get<Door>(components);
    public ZSyncTransform? ZSyncTransform { get; } = Get<ZSyncTransform>(components);
    public Turret? Turret { get; } = Get<Turret>(components);
    public WearNTear? WearNTear { get; } = Get<WearNTear>(components);
    public Trader? Trader { get; } = Get<Trader>(components);
    public Plant? Plant { get; } = Get<Plant>(components);

    public static PrefabInfo Dummy { get; } = new(new Dictionary<Type, MonoBehaviour>(0), null);
}