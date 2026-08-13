using BepInEx.Configuration;

namespace ServersideQoL.AutoMapTables;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "AutoMapTables";

  static readonly AcceptableEnum<Minimap.PinType> __acceptablePins = new([Minimap.PinType.None, Minimap.PinType.Icon0, Minimap.PinType.Icon1, Minimap.PinType.Icon2, Minimap.PinType.Icon3, Minimap.PinType.Icon4]);

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");
  public ConfigEntry<float> MapTableRange { get; } = BindEx(cfg, Section, 4f,
    "If a player enters this range around a map table, their discovered information (portal/ship/ore deposits/etc. position) is transfered to the map table.");
  public ConfigEntry<Minimap.PinType> PortalsPinType { get; } = BindEx(cfg, Section, Minimap.PinType.Icon4,
    "The pin type for portals on the map table", __acceptablePins);
  public ConfigEntry<string> PortalsExclude { get; } = BindEx(cfg, Section, "",
    "Portals with a tag that matches this filter are not added to map tables");
  public ConfigEntry<string> PortalsInclude { get; } = BindEx(cfg, Section, "*",
    "Only portals with a tag that matches this filter are added to map tables");
  public ConfigEntry<Minimap.PinType> ShipsPinType { get; } = BindEx(cfg, Section, Minimap.PinType.Player,
    "The pin type for ships on the map table", new AcceptableEnum<Minimap.PinType>([..__acceptablePins.AcceptableValues, Minimap.PinType.Player]));
  //public ConfigEntry<Minimap.PinType> DungeonsPinType { get; } = BindEx(cfg, Section, Minimap.PinType.Icon1, """
  //  The pin type for dungeons on the map table.
  //  Dungeons will only be added to the map table after they've been entered.
  //  """, __acceptablePins);
    
  public IReadOnlyDictionary<int, ConfigEntry<Minimap.PinType>> AutoUpdateOreDeposits { get; } = GetPrefabPinConfig(cfg, logger) ?? EmptyReadOnlyCollections<int, ConfigEntry<Minimap.PinType>>.Dictionary;

  public ConfigEntry<float> OreDepositsDiscoverRange { get; } = BindEx(cfg, Section, ZoneSystem.c_ZoneSizeHalf,
    "An ore deposit is considered 'discovered by a player' when that player was within this range around the deposit while it was struck by a pickaxe");

  public ConfigEntry<MessageTypes> UpdatedMessageType { get; } = BindEx(cfg, Section, MessageTypes.None,
    "Type of message to show when a map table is updated", AcceptableEnum<MessageTypes>.Default);
  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);
  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);

  public sealed class LocalizationConfig
  {
    public string Updated { get; init; } = "$msg_mapsaved";
  }

  public sealed class AdvancedConfig
  {
    public float MapTableUpdateInterval { get; init; } = 5;
  }

  static IReadOnlyDictionary<int, ConfigEntry<Minimap.PinType>>? GetPrefabPinConfig(ConfigFile cfg, Logger logger)
  {
    var smelterInputs = new Dictionary<ItemDrop, ConfigEntry<Minimap.PinType>?>();
    var mineRocks = new List<MineRock5>();
    foreach (var prefab in ZNetScene.instance.m_prefabs)
    {
      if (prefab.GetComponent<Smelter>() is { } smelter)
      {
        foreach (var item in smelter.m_conversion.Select(x => x.m_from))
          smelterInputs.TryAdd(item, null);
      }
      else if (prefab.GetComponent<MineRock5>() is { } mineRock)
      {
        mineRocks.Add(mineRock);
      }
    }

    Dictionary<int, ConfigEntry<Minimap.PinType>>? entries = null;

    var acctableValues = new AcceptableEnum<Minimap.PinType>([Minimap.PinType.None, Minimap.PinType.Icon0, Minimap.PinType.Icon1, Minimap.PinType.Icon2, Minimap.PinType.Icon3, Minimap.PinType.Icon4]);

    foreach (var mineRock in mineRocks)
    {
      ConfigEntry<Minimap.PinType>? entry = null;
      foreach (var item in mineRock.m_dropItems.m_drops.Select(static x => x.m_item.GetComponent<ItemDrop>()))
      {
        if (!smelterInputs.TryGetValue(item, out entry))
          continue;

        if (entry is null)
        {
          var name = item.name;
          if (!char.IsUpper(name[0]))
            name = $"{char.ToUpperInvariant(name[0])}{name[1..]}";
          smelterInputs[item] = entry = cfg.Bind(Section, $"{name}PinType", Minimap.PinType.None, new ConfigDescription($"""
            The pin icon to use for {(global::Localization.instance.Localize(item.m_itemData.m_shared.m_name))}.
            Pins will only be added to the map table after the ore deposit was hit at least once with a pickaxe.
            """, acctableValues));
        }
        break;
      }

      if (entry is not null)
        (entries ??= []).Add(mineRock.gameObject.name.GetStableHashCode(), entry);
    }

    return entries;
  }
}
