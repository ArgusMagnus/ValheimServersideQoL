using UnityEngine;

namespace Valheim.ServersideQoL;

sealed class PrefabInfo(IReadOnlyDictionary<Type, MonoBehaviour> components)
{
    public IReadOnlyDictionary<Type, MonoBehaviour> Components { get; } = components;

    public Sign? Sign { get; } = Get<Sign>(components);
    public MapTable? MapTable { get; } = Get<MapTable>(components);
    public Tameable? Tameable { get; } = Get<Tameable>(components);
    public Fireplace? Fireplace { get; } = Get<Fireplace>(components);
    public (Container Container, Piece Piece, PieceTable PieceTable)? Container { get; } = Get<Container, Piece, PieceTable>(components);
    public (Ship Ship, Piece Piece)? Ship { get; } = Get<Ship, Piece>(components);
    public (ItemDrop ItemDrop, Optional<Piece> Piece)? ItemDrop { get; } = Get<ItemDrop, Piece>(components);
    public Smelter? Smelter { get; } = Get<Smelter>(components);
    public ShieldGenerator? ShieldGenerator { get; } = Get<ShieldGenerator>(components);
    public Windmill? Windmill { get; } = Get<Windmill>(components);
    public Vagon? Vagon { get; } = Get<Vagon>(components);
    public Player? Player { get; } = Get<Player>(components);
    public TeleportWorld? TeleportWorld { get; } = Get<TeleportWorld>(components);
    public Door? Door { get; } = Get<Door>(components);
    public ZSyncTransform? ZSyncTransform { get; } = Get<ZSyncTransform>(components);
    public (Turret Turret, Piece Piece, PieceTable PieceTable)? Turret { get; } = Get<Turret, Piece, PieceTable>(components);
    public (WearNTear WearNTear, Optional<Piece> Piece, Optional<PieceTable> PieceTable)? WearNTear { get; } = Get<WearNTear, Piece, PieceTable>(components);
    public Trader? Trader { get; } = Get<Trader>(components);
    public Plant? Plant { get; } = Get<Plant>(components);
    public EggGrow? EggGrow { get; } = Get<EggGrow>(components);
    public Growup? Growup { get; } = Get<Growup>(components);
    public (Trap Trap, Aoe Aoe, Piece Piece, PieceTable PieceTable)? Trap { get; } = Get<Trap, Aoe, Piece, PieceTable>(components);

    public static PrefabInfo Dummy { get; } = new(new Dictionary<Type, MonoBehaviour>(0));

    static T? Get<T>(IReadOnlyDictionary<Type, MonoBehaviour> prefabs)
        where T : MonoBehaviour
        => prefabs.TryGetValue(typeof(T), out var value) ? (T)value : null;

    static (T1 F1, Optional<T2> F2)? Get<T1, T2>(IReadOnlyDictionary<Type, MonoBehaviour> prefabs)
        where T1 : MonoBehaviour where T2 : MonoBehaviour
    {
        if (!prefabs.TryGetValue(typeof(T1), out var f1))
            return null;
        return ((T1)f1, new(prefabs.TryGetValue(typeof(T2), out var f2) ? (T2)f2 : null));
    }

    static (T1 F1, Optional<T2> F2, Optional<T3> F3)? Get<T1, T2, T3>(IReadOnlyDictionary<Type, MonoBehaviour> prefabs)
        where T1 : MonoBehaviour where T2 : MonoBehaviour where T3 : MonoBehaviour
    {
        if (!prefabs.TryGetValue(typeof(T1), out var f1))
            return null;
        return ((T1)f1, new(prefabs.TryGetValue(typeof(T2), out var f2) ? (T2)f2 : null), new(prefabs.TryGetValue(typeof(T3), out var f3) ? (T3)f3 : null));
    }

    static (T1 F1, Optional<T2> F2, Optional<T3> F3, Optional<T4> F4)? Get<T1, T2, T3, T4>(IReadOnlyDictionary<Type, MonoBehaviour> prefabs)
        where T1 : MonoBehaviour where T2 : MonoBehaviour where T3 : MonoBehaviour where T4 : MonoBehaviour
    {
        if (!prefabs.TryGetValue(typeof(T1), out var f1))
            return null;
        return ((T1)f1, new(prefabs.TryGetValue(typeof(T2), out var f2) ? (T2)f2 : null),
            new(prefabs.TryGetValue(typeof(T3), out var f3) ? (T3)f3 : null), new(prefabs.TryGetValue(typeof(T4), out var f4) ? (T4)f4 : null));
    }

    public record struct Optional<T>(T? Value) where T : MonoBehaviour
    {
        public static implicit operator T(in Optional<T> value) => value.Value ?? throw new ArgumentNullException();
    }
}