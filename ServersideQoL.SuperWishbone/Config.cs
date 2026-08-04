using BepInEx.Configuration;
using UnityEngine;

namespace ServersideQoL.SuperWishbone;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "SuperWishbone";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<bool> FindDungeons { get; } = BindEx(cfg, Section, true,
    "True to make the wishbone find dungeons");
  public ConfigEntry<bool> FindVegvisir { get; } = BindEx(cfg, Section, true,
    "True to make the wishbone find vegvisirs");
  //public ConfigEntry<string> FindLocationObjectRegex { get; } = BindEx(cfg, Section, "", """
  //  The wishbone will find locations which contain an object whose (prefab) name matches this regular expression.
  //  Example: Beehive|goblin_totempole|giant_brain|dvergrprops_crate\w*
  //  """);
  public ConfigEntry<float> Range { get; } = BindEx(cfg, Section, Mathf.Max(Minimap.instance.m_exploreRadius, ZoneSystem.c_ZoneSize),
      "Radius in which the wishbone will react to dungeons/locations",
      new AcceptableValueRange<float>(0, ZoneSystem.c_ZoneSize * 2 * Mathf.Sqrt(2)));
}
