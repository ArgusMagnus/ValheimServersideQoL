using BepInEx.Configuration;
using System.Reflection;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ServersideQoL.PrefabConfigurator;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "PrefabConfigurator";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");

  public YamlConfigEntry<PrefabsConfig> Prefabs { get; } = BindYaml<PrefabsConfig>(cfg);

  public sealed class PrefabsConfig
  {
    public List<ComponentConfig> Entries { get => field ??= GetList(); init; }

    static readonly Dictionary<string, (Type Type, IReadOnlyDictionary<string, FieldInfo>)> __validComponents = [];
    [YamlIgnore]
    public IReadOnlyDictionary<string, (Type Type, IReadOnlyDictionary<string, FieldInfo>)> ValidComponents => __validComponents;

    public sealed class ComponentConfig
    {
      public required string Component { get; init; }
      public required string[] PrefabNames { get; init; } = [];
      public required Dictionary<string, object?> Fields { get; init; }
    }

    static List<ComponentConfig> GetList()
    {
      /// <see cref="ZNetView.LoadFields"/>
      HashSet<Type> validFieldTypes = [typeof(int), typeof(float), typeof(bool), typeof(Vector3), typeof(string), typeof(GameObject), typeof(ItemDrop)];

      List <ComponentConfig> entries = [];
      foreach (var group in ZNetScene.instance.m_prefabs
        .SelectMany(static x => x.GetComponent<ZNetView>().GetComponentsInChildren<MonoBehaviour>().Where(static x => x is not ZNetView).Select(c => (x.name, component: c)))
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
