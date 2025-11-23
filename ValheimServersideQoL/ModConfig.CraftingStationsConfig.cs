using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class CraftingStationsConfig(ConfigFile cfg, string section)
    {
        public IReadOnlyDictionary<CraftingStation, StationCfg> StationConfig { get; } = new Func<IReadOnlyDictionary<CraftingStation, StationCfg>>(() =>
        {
            Dictionary<CraftingStation, bool> dict = new();
            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab.GetComponent<CraftingStation>() is { } station)
                {
                    if (station.m_areaMarker is not null && !dict.ContainsKey(station))
                        dict.Add(station, false);
                }
                else if (prefab.GetComponent<StationExtension>() is { } extension)
                {
                    station = extension.m_craftingStation;
                    dict[station] = true;
                }
            }
            return dict.ToDictionary(static x => x.Key, x => new StationCfg(cfg, section, NormalizeName(x.Key.name), x.Key, x.Value));
        }).Invoke();

        public sealed class StationCfg(ConfigFile cfg, string section, string prefix, CraftingStation station, bool hasExtensions)
        {
            public ConfigEntry<float>? BuildRange { get; } = station.m_areaMarker is null ? null :
            cfg.Bind(section, $"{prefix}{nameof(BuildRange)}", station.m_rangeBuild, $"Build range of {(global::Localization.instance.Localize(station.m_name))}");
            public ConfigEntry<float>? ExtraBuildRangePerLevel { get; } = station.m_areaMarker is null || !hasExtensions ? null :
            cfg.Bind(section, $"{prefix}{nameof(ExtraBuildRangePerLevel)}", station.m_extraRangePerLevel, $"Additional build range per level of {(global::Localization.instance.Localize(station.m_name))}");
            public ConfigEntry<float>? MaxExtensionDistance { get; } = !hasExtensions ? null :
            cfg.Bind(section, $"{prefix}{nameof(MaxExtensionDistance)}", float.NaN, Invariant($"""
                 Max distance an extension can have to the corresponding {(global::Localization.instance.Localize(station.m_name))} to increase its level.
                 Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional {(global::Localization.instance.Localize(station.m_name))} to be able to place the extension.
                 {float.NaN} to use the game's default range. 
                 """));
        }

        static string NormalizeName(string name)
        {
            if (char.IsUpper(name[0]))
                return name;
            name = name.Replace("piece_", "");
            return $"{char.ToUpperInvariant(name[0])}{name[1..]}";
        }
    }
}