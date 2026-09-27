using BepInEx.Configuration;
using ServersideQoL.Utilities;

namespace ServersideQoL.AutoDoors;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");
  public ConfigEntry<float> AutoCloseMinPlayerDistance { get; } = BindEx(cfg, 4f,
    Invariant($"Minimum distance all players must have to the door before it is closed."));
  public ConfigEntry<float> AutoCloseMinOpenSeconds { get; } = BindEx(cfg, 2f,
    Invariant($"Minimum time the door must have been open before it is closed."));

  public IReadOnlyDictionary<Door, ConfigEntry<bool>> AutoClose { get; } = GetAutoClosePerDoor(cfg);

  static IReadOnlyDictionary<Door, ConfigEntry<bool>> GetAutoClosePerDoor(ConfigFile cfg)
  {
    Dictionary<Door, ConfigEntry<bool>> result = [];

    foreach (var prefab in ZNetScene.instance.m_prefabs)
    {
      if (prefab.GetComponentInChildren<Door>() is not { m_keyItem: null, m_canNotBeClosed: false } door || prefab.GetComponentInChildren<Piece>() is null)
        continue;
      if (!PieceTablesByPieceName.ContainsKey(prefab.name))
        continue;
      var name = SQoLUtils.ToPascalCase(door.name);
      var defaultValue = !name.Contains("Window") && !name.Contains("Drawbridge");
      result.Add(door, BindEx(cfg, "Doors", defaultValue, $"True to automatically close {Localization.instance.Localize(door.m_name)}",
        key: $"Close{name}"));
    }

    return result;
  }

  //public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);
  //public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  //public sealed class LocalizationConfig
  //{
  //}

  //public sealed class AdvancedConfig
  //{
  //  public float ResetTerrainRadius { get; init; } = 3;
  //}
}
