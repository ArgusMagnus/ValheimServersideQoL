using BepInEx.Configuration;
using UnityEngine;

namespace ServersideQoL.SuperWishbone;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> FindDungeons { get; } = BindEx(cfg, true,
    "True to make the wishbone find dungeons");
  public ConfigEntry<bool> FindVegvisir { get; } = BindEx(cfg, true,
    "True to make the wishbone find vegvisirs");
  public ConfigEntry<float> Range { get; } = BindEx(cfg, Mathf.Max(Minimap.instance.m_exploreRadius, ZoneSystem.c_ZoneSize),
      "Radius in which the wishbone will react to dungeons/locations",
      new AcceptableValueRange<float>(0, Mathf.Floor(ZoneSystem.c_ZoneSize * 2 * Mathf.Sqrt(2))));

  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class AdvancedConfig
  {
    public List<Entry>? Entries { get => field ??= GetDefaultEntries(); init; }

    public sealed class Entry
    {
      public required string PrefabNamePattern { get; init; }
      public required bool Enabled { get; init; } = true;
      public float Range { get; init; } = Mathf.Max(Minimap.instance.m_exploreRadius, ZoneSystem.c_ZoneSize);
      public int MinLevel { get; init; } = 1;
      public int MinQuality { get; init; } = 1;
    }

    static List<Entry> GetDefaultEntries() => [
      new(){ PrefabNamePattern = "Beehive", Enabled = false },
      new(){ PrefabNamePattern = "goblin_totempole", Enabled = false },
      new(){ PrefabNamePattern = "giant_brain", Enabled = false },
      new(){ PrefabNamePattern = "dvergrprops_crate*", Enabled = false },
      new(){ PrefabNamePattern = "Fish*", Enabled = false, MinQuality = 4 },
      new(){ PrefabNamePattern = "Serpent", Enabled = false }];
  }
}
