using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using static ZoneSystem;

namespace ServersideQoL;

public static class PrivateAccessor
{
  const BindingFlags AccessFlags = BindingFlags.NonPublic
#if !DEBUG
         | BindingFlags.Public
#endif
      ;

  [Obsolete(null, true)]
  public static Dictionary<ZDOID, ZDO>.ValueCollection GetObjects(ZDOMan instance) => instance.m_objectsByID.Values;

  static Func<Localization, IReadOnlyDictionary<string, string>> GetLocalizationStrings
#if DEBUG
  { get; } =
#else
    => field ??=
#endif
      Expression.Lambda<Func<Localization, IReadOnlyDictionary<string, string>>>(
      Expression.Field(
          Expression.Parameter(typeof(Localization)) is var par1 ? par1 : throw new Exception(),
          typeof(Localization).GetField("m_translations", AccessFlags | BindingFlags.Instance)),
      par1).Compile();

  public static IReadOnlyDictionary<string, string> GetStrings(this Localization instance) => GetLocalizationStrings(instance);

  static Func<RandEventSystem, RandomEvent?> GetCurrentEventFunc
#if DEBUG
  { get; } =
#else
    => field ??=
#endif
      Expression.Lambda<Func<RandEventSystem, RandomEvent>>(
      Expression.Field(
          Expression.Parameter(typeof(RandEventSystem)) is var par1 ? par1 : throw new Exception(),
          typeof(RandEventSystem).GetField("m_randomEvent", AccessFlags | BindingFlags.Instance)),
      par1).Compile();

  [Obsolete(null, true)]
  public static RandomEvent? GetCurrentEvent(RandEventSystem instance) => GetCurrentEventFunc(instance);

  static Func<ZoneSystem, IReadOnlyDictionary<int, ZoneLocation>> GetLocationsByHashFunc
#if DEBUG
  { get; } =
#else
    => field ??=
#endif
      Expression.Lambda<Func<ZoneSystem, IReadOnlyDictionary<int, ZoneLocation>>>(
      Expression.Field(
          Expression.Parameter(typeof(ZoneSystem)) is var par1 ? par1 : throw new Exception(),
          typeof(ZoneSystem).GetField("m_locationsByHash", AccessFlags | BindingFlags.Instance)),
      par1).Compile();

  [Obsolete(null, true)]
  public static IReadOnlyDictionary<int, ZoneLocation> GetLocationsByHash(ZoneSystem instance) => GetLocationsByHashFunc(instance);

  public static Utilities.Location GetAndLoadLocationByHash(this ZoneSystem instance, int hash)
    => instance.m_locationsByHash.TryGetValue(hash, out var loc) ? new(loc) : default;

  static Func<ZoneSystem, ZoneLocation, int, Vector3, Quaternion, SpawnMode, List<GameObject>, bool, GameObject> SpawnLocationFunc
#if DEBUG
  { get; } =
#else
    => field ??=
#endif
      Expression.Lambda<Func<ZoneSystem, ZoneLocation, int, Vector3, Quaternion, SpawnMode, List<GameObject>, bool, GameObject>>(
      Expression.Call(
          Expression.Parameter(typeof(ZoneSystem)) is var par1 ? par1 : throw new Exception(),
          typeof(ZoneSystem).GetMethod("SpawnLocation", AccessFlags | BindingFlags.Instance),
          Expression.Parameter(typeof(ZoneLocation)) is var par2 ? par2 : throw new Exception(),
          Expression.Parameter(typeof(int)) is var par3 ? par3 : throw new Exception(),
          Expression.Parameter(typeof(Vector3)) is var par4 ? par4 : throw new Exception(),
          Expression.Parameter(typeof(Quaternion)) is var par5 ? par5 : throw new Exception(),
          Expression.Parameter(typeof(SpawnMode)) is var par6 ? par6 : throw new Exception(),
          Expression.Parameter(typeof(List<GameObject>)) is var par7 ? par7 : throw new Exception(),
          Expression.Parameter(typeof(bool)) is var par8 ? par8 : throw new Exception()),
      par1, par2, par3, par4, par5, par6, par7, par8).Compile();

  [Obsolete(null, true)]
  public static GameObject SpawnLocation(ZoneSystem instance, ZoneLocation location, int seed, Vector3 pos, Quaternion rot, SpawnMode mode, List<GameObject>? spawnedGhostObjects = null, bool cheated = false)
      => SpawnLocationFunc(instance, location, seed, pos, rot, mode, spawnedGhostObjects ?? [], cheated);

  [Obsolete(null, true)]
  public static void SendGlobalKeys(ZoneSystem instance, long peerID) => instance.SendGlobalKeys(peerID);


  [Obsolete(null, true)]
  public static int ZSyncAnimationZDOSalt => ZSyncAnimation.c_ZDOSalt;

  [Obsolete(null, true)]
  public static int CharacterAnimationHashEncumbered => Character.s_encumbered;

  [Obsolete(null, true)]
  public static int CharacterAnimationHashInWater => Character.s_inWater;

  [Obsolete(null, true)]
  public static int PlayerAnimationHashCrouching => Player.s_crouching;
}
