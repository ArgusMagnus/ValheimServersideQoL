using BepInEx.Configuration;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ServersideQoL.PrefabConfigurator;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");
  public FireplacesConfig Fireplaces { get; } = new(cfg);
  public BuildPiecesConfig BuildPieces { get; } = new(cfg);
  public PlantsConfig Plants { get; } = new(cfg);
  public CartsConfig Carts { get; } = new(cfg);
  public ShipsConfig Ships { get; } = new(cfg);
  public CraftingStationsConfig CraftingStations { get; } = new(cfg);
  public YamlConfigEntry<PrefabsConfig> Prefabs { get; } = BindYaml<PrefabsConfig>(cfg);

  /// <summary>
  /// Per-crafting-station range overrides, grouped by prefab in the generated cfg.
  /// Only emitted for stations that gate piece placement (a <see cref="Piece"/> references them
  /// via <see cref="Piece.m_craftingStation"/>), e.g. workbench or stonecutter — not cooking stations.
  /// <c>BuildRange_&lt;prefab&gt;</c>: build range; <c>-1</c> skips the station entirely (game default).
  /// <c>EnemySpawnRange_&lt;prefab&gt;</c>: enemy spawn exclusion radius (PlayerBase effect area);
  /// <c>-1</c> matches the configured build range.
  /// </summary>
  public sealed class CraftingStationsConfig
  {
    public IReadOnlyDictionary<int, ConfigEntry<float>> BuildRanges { get; } = new Dictionary<int, ConfigEntry<float>>();
    public IReadOnlyDictionary<int, ConfigEntry<float>> EnemySpawnRanges { get; } = new Dictionary<int, ConfigEntry<float>>();

    public CraftingStationsConfig(ConfigFile cfg, [CallerMemberName] string section = default!)
    {
      /// Stations that gate piece placement: a piece can only be built in range of
      /// the station its <see cref="Piece.m_craftingStation"/> points to.
      var buildStations = new HashSet<CraftingStation>(ZNetScene.instance.m_prefabs
        .Select(static x => x.GetComponent<Piece>()?.m_craftingStation)
        .Where(static x => x is not null)!);

      var buildRanges = (Dictionary<int, ConfigEntry<float>>)BuildRanges;
      var enemySpawnRanges = (Dictionary<int, ConfigEntry<float>>)EnemySpawnRanges;
      foreach (var prefab in ZNetScene.instance.m_prefabs.OrderBy(static x => x.name))
      {
        if (prefab.GetComponent<CraftingStation>() is not { } station || !buildStations.Contains(station))
          continue;

        var name = prefab.name;
        var hash = name.GetStableHashCode();
        var piece = prefab.GetComponent<Piece>();
        var localized = piece is null ? name : global::Localization.instance.Localize(piece.m_name);
        var spawnAreas = prefab.GetComponentsInChildren<EffectArea>(true)
          .Where(static a => (a.m_type & EffectArea.Type.PlayerBase) is not 0)
          .Select(static a => a.GetComponent<Collider>())
          .Where(static c => c is SphereCollider or CapsuleCollider)
          .ToList();

        buildRanges.Add(hash, cfg.Bind(section, Invariant($"BuildRange_{name}"), station.m_rangeBuild, Invariant($"""
          Build range for '{localized}' in meters. Game default: {station.m_rangeBuild}. -1 to skip this station and keep the game default.
          """)));

        if (spawnAreas.Count is not 0)
          enemySpawnRanges.Add(hash, cfg.Bind(section, Invariant($"EnemySpawnRange_{name}"), Radius(spawnAreas[0]), Invariant($"""
            Enemy spawn exclusion radius for '{localized}' in meters. Game default: {Radius(spawnAreas[0])}. 0 disables spawn exclusion.
            -1 to match 'BuildRange_{name}' (game default: {station.m_rangeBuild}).
            Note: this area also determines whether dropped items count as being inside a player base (they do not despawn there).
            """)));
      }
    }

    static float Radius(Collider collider) => collider switch
    {
      SphereCollider s => s.radius,
      CapsuleCollider c => c.radius,
      _ => 0
    };
  }

  public sealed class FireplacesConfig(ConfigFile cfg, [CallerMemberName] string section = default!)
  {
    public ConfigEntry<bool> MakeToggleable { get; } = BindEx(cfg, section, false, """
      True to make all fireplaces/lightsources (including torches, braziers, etc.) toggleable.
      BEWARE: toggleable fireplaces/lightsources will be toggled off automatically by rain/heavy wind.
      """);

    public ConfigEntry<bool> InfiniteFuel { get; } = BindEx(cfg, section, false,
      "True to make all fireplaces have infinite fuel");
  }

  public sealed class BuildPiecesConfig(ConfigFile cfg, [CallerMemberName] string section = default!)
  {
    public ConfigEntry<bool> DisableRainDamage { get; } = BindEx(cfg, section, false,
      "True to prevent rain from damaging build pieces");

    public ConfigEntry<DisableSupportRequirementsOptions> DisableSupportRequirements { get; } = BindEx(cfg, section, DisableSupportRequirementsOptions.None,
      "Ignore support requirements on build pieces", AcceptableEnum<DisableSupportRequirementsOptions>.Default);

    public ConfigEntry<bool> MakeIndestructible { get; } = BindEx(cfg, section, false,
      "True to make player-built pieces indestructible");

    public ConfigEntry<bool> EnableAsSupport { get; } = BindEx(cfg, section, false, """
      True to make player-built pieces valid support.
      For example, this will allow players to place chests or signs on other chests.
      """);


    [Flags]
    public enum DisableSupportRequirementsOptions
    {
      None,
      PlayerBuilt = 1 << 0,
      World = 1 << 1
    }
  }

  public sealed class PlantsConfig(ConfigFile cfg, [CallerMemberName] string section = default!)
  {
    public ConfigEntry<float> GrowTimeMultiplier { get; } = BindEx(cfg, section, 1f,
      "Multiply plant grow time by this factor. 0 to make them grow almost instantly.");
    public ConfigEntry<float> SpaceRequirementMultiplier { get; } = BindEx(cfg, section, 1f,
      "Multiply plant space requirement by this factor. 0 to disable space requirements.");
    public ConfigEntry<bool> DontDestroyIfCantGrow { get; } = BindEx(cfg, section, false,
      "True to keep plants that can't grow alive");
  }

  public sealed class CartsConfig(ConfigFile cfg, [CallerMemberName] string section = default!)
  {
    public ConfigEntry<float> ContentMassMultiplier { get; } = BindEx(cfg, section, 1f,
        "Multiplier for a carts content weight. E.g. set to 0 to ignore a cart's content weight");

    public ConfigEntry<bool> DeconstructWithHammer { get; } = BindEx(cfg, section, false,
        "If enabled, carts can be deconstructed with the build hammer");
  }

  public sealed class ShipsConfig(ConfigFile cfg, [CallerMemberName] string section = default!)
  {
    public ConfigEntry<bool> DeconstructWithHammer { get; } = BindEx(cfg, section, false,
        "If enabled, ships can be deconstructed with the build hammer");
  }

  public sealed class PrefabsConfig
  {
    public List<ComponentConfig> Entries { get => field ??= GetList(); init; }

    static readonly Dictionary<string, (Type Type, IReadOnlyDictionary<string, FieldInfo>)> __validComponents = [];
    [YamlIgnore]
    public IReadOnlyDictionary<string, (Type Type, IReadOnlyDictionary<string, FieldInfo>)> ValidComponents => __validComponents;

    public sealed class ComponentConfig
    {
      public required string Component { get; init; }
      public required bool Enabled { get; init; } = true;
      public required string[] PrefabNames { get; init; } = [];
      public required Dictionary<string, object?> Fields { get; init; }
    }

    static List<ComponentConfig> GetList()
    {
      /// <see cref="ZNetView.LoadFields"/>
      HashSet<Type> validFieldTypes = [typeof(int), typeof(float), typeof(bool), typeof(Vector3), typeof(string), typeof(GameObject), typeof(ItemDrop)];

      List <ComponentConfig> entries = [];
      foreach (var group in ZNetScene.instance.ZNetViews
        .SelectMany(static x => x.GetComponentsInChildren<MonoBehaviour>().Where(static x => x is not ZNetView).Select(c => (x.name, component: c)))
        .GroupBy(static x => x.component.GetType())
        .OrderBy(static x => x.Key.Name))
      {
        var componentType = group.Key;
        var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance)
          .Where(x => validFieldTypes.Contains(x.FieldType))
          .ToList();

        if (fields.Count is 0)
          continue;

        if (!__validComponents.ContainsKey(componentType.Name))
          __validComponents.Add(componentType.Name, (componentType, fields.ToDictionary(static x => x.Name)));

        foreach (var (name, component) in group)
        {
          entries.Add(new()
          {
            Component = componentType.Name,
            Enabled = false,
            PrefabNames = [name],
            Fields = fields.ToDictionary(static x => x.Name, x => Serialize(x.GetValue(component)))
          });
        }

        static object? Serialize(object? value) => value switch
        {
          UnityEngine.Object obj => obj.name,
          Vector3 vec => new Vector3Yaml { x = vec.x, y = vec.y, z = vec.z },
          _ => value
        };
      }

      return entries;
    }

    public sealed class Vector3Yaml
    {
      public float x { get; init; }
      public float y { get; init; }
      public float z { get; init; }
    }
  }
}
