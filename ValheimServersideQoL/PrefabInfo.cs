using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Valheim.ServersideQoL;

sealed record PrefabInfo(GameObject Prefab, IReadOnlyDictionary<Type, MonoBehaviour> Components)
{
    public string PrefabName => Prefab?.name ?? "";

    public Sign? Sign { get; } = Get<Sign>(Components);
    public MapTable? MapTable { get; } = Get<MapTable>(Components);
    public (Tameable Tameable, MonsterAI MonsterAI)? Tameable { get; } = GetTuple<(Tameable, MonsterAI)>(Components);
    public Fireplace? Fireplace { get; } = Get<Fireplace>(Components);
    public (Container Container, Piece Piece, PieceTable PieceTable, Optional<Incinerator> Incinerator, Optional<ZSyncTransform> ZSyncTransform)? Container { get; } = GetTuple<(Container, Piece, PieceTable, Optional<Incinerator>, Optional<ZSyncTransform>)>(Components);
    public (Ship Ship, Piece Piece)? Ship { get; } = GetTuple<(Ship, Piece)>(Components);
    public (ItemDrop ItemDrop, Optional<ZSyncTransform> ZSyncTransform, Optional<Piece> Piece)? ItemDrop { get; } = GetTuple<(ItemDrop, Optional<ZSyncTransform>, Optional<Piece>)>(Components);
    public Smelter? Smelter { get; } = Get<Smelter>(Components);
    public ShieldGenerator? ShieldGenerator { get; } = Get<ShieldGenerator>(Components);
    public Windmill? Windmill { get; } = Get<Windmill>(Components);
    public Vagon? Vagon { get; } = Get<Vagon>(Components);
    public Player? Player { get; } = Get<Player>(Components);
    public TeleportWorld? TeleportWorld { get; } = Get<TeleportWorld>(Components);
    public Door? Door { get; } = Get<Door>(Components);
    public (Turret Turret, Piece Piece, PieceTable PieceTable)? Turret { get; } = GetTuple<(Turret, Piece, PieceTable)>(Components);
    public (WearNTear WearNTear, Optional<Piece> Piece, Optional<PieceTable> PieceTable)? WearNTear { get; } = GetTuple<(WearNTear, Optional<Piece>, Optional<PieceTable>)>(Components);
    public CraftingStation? CraftingStation { get; } = Get<CraftingStation>(Components);
    public StationExtension? StationExtension { get; } = Get<StationExtension>(Components);
    public Trader? Trader { get; } = Get<Trader>(Components);
    public Plant? Plant { get; } = Get<Plant>(Components);
    public EggGrow? EggGrow { get; } = Get<EggGrow>(Components);
    public Growup? Growup { get; } = Get<Growup>(Components);
    public (Aoe Aoe, Piece Piece, PieceTable PieceTable, Optional<Trap> Trap)? Trap { get; } = GetTuple<(Aoe, Piece, PieceTable, Optional<Trap>)>(Components);
    public Mister? Mister { get; } = Get<Mister>(Components);
    public CreatureSpawner? CreatureSpawner { get; } = Get<CreatureSpawner>(Components);
    public SpawnArea? SpawnArea { get; } = Get<SpawnArea>(Components);
    public (Humanoid Humanoid, Optional<MonsterAI> MonsterAI, Optional<CharacterDrop> CharacterDrop)? Humanoid { get; } = GetTuple<(Humanoid, Optional<MonsterAI>, Optional<CharacterDrop>)>(Components);
    public (Character Character, Optional<CharacterDrop> CharacterDrop)? Character { get; } = GetTuple<(Character, Optional<CharacterDrop>)>(Components);
    public EffectArea? EffectArea { get; } = Get<EffectArea>(Components);
    //public ItemStand? ItemStand { get; } = Get<ItemStand>(Components);
    public CookingStation? CookingStation { get; } = Get<CookingStation>(Components);
    public Fermenter? Fermenter { get; } = Get<Fermenter>(Components);

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