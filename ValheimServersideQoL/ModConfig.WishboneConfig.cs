using BepInEx.Configuration;
using UnityEngine;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class WishboneConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> FindDungeons { get; } = cfg.BindEx(section, false,
            "True to make the wishbone find dungeons");
        public ConfigEntry<bool> FindVegvisir { get; } = cfg.BindEx(section, false,
            "True to make the wishbone find vegvisirs");
        public ConfigEntry<string> FindLocationObjectRegex { get; } = cfg.BindEx(section, "", """
             The wishbone will find locations which contain an object whose (prefab) name matches this regular expression.
             Example: Beehive|goblin_totempole|giant_brain|dvergrprops_crate\w*
             """);
        public ConfigEntry<float> Range { get; } = cfg.BindEx(section, Mathf.Max(Minimap.instance.m_exploreRadius, ZoneSystem.c_ZoneSize),
            "Radius in which the wishbone will react to dungeons/locations",
            new AcceptableValueRange<float>(0, ZoneSystem.c_ZoneSize * 2 * Mathf.Sqrt(2)));
    }
}