using System.Runtime.CompilerServices;
using UnityEngine;

namespace Valheim.ServersideQoL;

sealed class PrefabInfo(GameObject prefab, IReadOnlyDictionary<Type, MonoBehaviour> components)
{
    public GameObject Prefab { get; } = prefab;
    public string PrefabName => Prefab?.name ?? "";
    public IReadOnlyDictionary<Type, MonoBehaviour> Components { get; } = components;

    public Sign? Sign { get; } = Get<Sign>(components);
    public MapTable? MapTable { get; } = Get<MapTable>(components);
    public (Tameable Tameable, MonsterAI MonsterAI)? Tameable { get; } = GetTuple<(Tameable, MonsterAI)>(components);
    public Fireplace? Fireplace { get; } = Get<Fireplace>(components);
    public (Container Container, Piece Piece, PieceTable PieceTable, Optional<Incinerator> Incinerator, Optional<ZSyncTransform> ZSyncTransform)? Container { get; } = GetTuple<(Container, Piece, PieceTable, Optional<Incinerator>, Optional<ZSyncTransform>)>(components);
    public (Ship Ship, Piece Piece)? Ship { get; } = GetTuple<(Ship, Piece)>(components);
    public (ItemDrop ItemDrop, Optional<Piece> Piece)? ItemDrop { get; } = GetTuple<(ItemDrop, Optional<Piece>)>(components);
    public Smelter? Smelter { get; } = Get<Smelter>(components);
    public ShieldGenerator? ShieldGenerator { get; } = Get<ShieldGenerator>(components);
    public Windmill? Windmill { get; } = Get<Windmill>(components);
    public Vagon? Vagon { get; } = Get<Vagon>(components);
    public Player? Player { get; } = Get<Player>(components);
    public TeleportWorld? TeleportWorld { get; } = Get<TeleportWorld>(components);
    public Door? Door { get; } = Get<Door>(components);
    public (Turret Turret, Piece Piece, PieceTable PieceTable)? Turret { get; } = GetTuple<(Turret, Piece, PieceTable)>(components);
    public (WearNTear WearNTear, Optional<Piece> Piece, Optional<PieceTable> PieceTable)? WearNTear { get; } = GetTuple<(WearNTear, Optional<Piece>, Optional<PieceTable>)>(components);
    public Trader? Trader { get; } = Get<Trader>(components);
    public Plant? Plant { get; } = Get<Plant>(components);
    public EggGrow? EggGrow { get; } = Get<EggGrow>(components);
    public Growup? Growup { get; } = Get<Growup>(components);
    public (Aoe Aoe, Piece Piece, PieceTable PieceTable, Optional<Trap> Trap)? Trap { get; } = GetTuple<(Aoe, Piece, PieceTable, Optional<Trap>)>(components);
    public Mister? Mister { get; } = Get<Mister>(components);
    //public CreatureSpawner? CreatureSpawner { get; } = Get<CreatureSpawner>(components);
    public (Humanoid Humanoid, Optional<CharacterDrop> CharacterDrop)? Humanoid { get; } = GetTuple<(Humanoid, Optional<CharacterDrop>)>(components);
    public EffectArea? EffectArea { get; } = Get<EffectArea>(components);
    //public ItemStand? ItemStand { get; } = Get<ItemStand>(components);

    public static PrefabInfo Dummy { get; } = new(null!, new Dictionary<Type, MonoBehaviour>(0));

    static T? Get<T>(IReadOnlyDictionary<Type, MonoBehaviour> prefabs)
        where T : MonoBehaviour
        => prefabs.TryGetValue(typeof(T), out var value) ? (T)value : null;

    static T? GetTuple<T>(IReadOnlyDictionary<Type, MonoBehaviour> prefabs)
        where T : struct, ITuple
    {
        var types = typeof(T).GenericTypeArguments;
        var args = new object[types.Length];
        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Optional<>))
            {
                if (!prefabs.TryGetValue(type, out var value))
                    return default;
                args[i] = value;
            }
            else
            {
                prefabs.TryGetValue(type.GenericTypeArguments[0], out var value);
                args[i] = Activator.CreateInstance(type, args: [value]);
            }
        }
        return (T)Activator.CreateInstance(typeof(T), args: args);
    }

    public record struct Optional<T>(T? Value) where T : MonoBehaviour;
}