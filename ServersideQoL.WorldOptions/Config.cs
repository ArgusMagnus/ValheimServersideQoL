using BepInEx.Configuration;

namespace ServersideQoL.WorldOptions;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "WorldOptions";
  internal const string GlobalKeyNone = "--";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");

  public ConfigEntry<RemoveMistlandsMistOptions> RemoveMistlandsMist { get; } = BindEx(cfg, Section, RemoveMistlandsMistOptions.AfterQueenKilled, """
    Condition to remove the mist from the mistlands.
    Beware that there are a few cases of mist (namely mist around POIs like ancient bones/skulls)
    that cannot be removed by this mod and will remain regardless of this setting.
    """, AcceptableEnum<RemoveMistlandsMistOptions>.Default);

  public IReadOnlyDictionary<Trader, IReadOnlyList<ConfigEntry<string>>> TaderProgressRequirements { get; } = GetTaderProgressRequirements(cfg);

  static IReadOnlyDictionary<Trader, IReadOnlyList<ConfigEntry<string>>> GetTaderProgressRequirements(ConfigFile cfg)
  {
    List<Trader> traders = [];
    HashSet<string> keys = [];
    foreach (var prefab in ZNetScene.instance.m_prefabs)
    {
      if (prefab.GetComponent<Trader>() is { } trader)
      {
        traders.Add(trader);
        foreach (var key in trader.m_items.Select(static x => x.m_requiredGlobalKey).Where(static x => !string.IsNullOrEmpty(x)))
          keys.Add(key);
      }
      else if (prefab.GetComponent<Character>() is { m_defeatSetGlobalKey: { Length: > 0 } key })
        keys.Add(key);
    }

    var accetableValues = new AcceptableValueList<string>([GlobalKeyNone, .. keys.OrderBy(static x => x)]);

    Dictionary<Trader, IReadOnlyList<ConfigEntry<string>>>? result = null;
    foreach (var trader in traders)
    {
      List<ConfigEntry<string>>? entries = null;
      foreach (var group in trader.m_items
        .Where(static x => !string.IsNullOrEmpty(x.m_requiredGlobalKey))
        .GroupBy(static x => x.m_requiredGlobalKey)
        .OrderBy(static x => x.Key))
      {
        var cfgKey = $"Set{(entries?.Count ?? 0) + 1}";
        var defaultValue = group.Key;
        var itemNames = string.Join(", ", group.Select(static x => global::Localization.instance.Localize(x.m_prefab.m_itemData.m_shared.m_name)));
        (entries ??= []).Add(cfg.Bind($"{trader.name}ProgressionRequirements", cfgKey, defaultValue, new ConfigDescription(
          $"The required global key for buying {itemNames} from {(global::Localization.instance.Localize(trader.m_name))}",
          accetableValues)));
      }

      if (entries is not null)
        (result ??= []).Add(trader, entries);
    }

    return result ?? EmptyReadOnlyCollections<Trader, IReadOnlyList<ConfigEntry<string>>>.Dictionary;
  }

  public enum RemoveMistlandsMistOptions
  {
    Never,
    Always,
    AfterQueenKilled
  }
}
