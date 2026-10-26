using BepInEx.Configuration;
using YamlDotNet.Serialization;

namespace ServersideQoL.LetItFloat;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");

  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg, static x => x.Refresh());

  public sealed class AdvancedConfig
  {
    public Dictionary<string, bool> Exclude { get => field ??= GetDefaultExclude(); init; }

    [YamlIgnore]
    internal HashSet<int> ExcludedPrefabHashes { get; } = [];

    Dictionary<string, bool> GetDefaultExclude()
    {
      Dictionary<string, bool> result = [];
      foreach (var prefab in ObjectDB.instance.m_items)
      {
        if (prefab.GetComponent<ItemDrop>() is { IsItem: true } && prefab.GetComponent<Floating>() is null && prefab.GetComponent<Fish>() is null)
          result.Add(prefab.name, false);
      }
      return result;
    }

    internal void Refresh()
    {
      ExcludedPrefabHashes.Clear();
      foreach (var (name, excluded) in Exclude)
      {
        if (!excluded)
          continue;
        var str = name?.Trim();
        if (!string.IsNullOrEmpty(str))
          ExcludedPrefabHashes.Add(str.GetStableHashCode());
      }
    }
  }
}
