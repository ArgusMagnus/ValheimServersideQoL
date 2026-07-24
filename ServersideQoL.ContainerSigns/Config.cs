using BepInEx.Configuration;
using YamlDotNet.Serialization;

namespace ServersideQoL.ContainerSigns;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "ContainerSigns";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");

  const string DefaultPlaceholderString = "•";
  public ConfigEntry<string> ChestSignsDefaultText { get; } = BindEx(cfg, Section, DefaultPlaceholderString, "Default text for chest signs");
  public ConfigEntry<string> ChestSignsContentListPlaceholder { get; } = BindEx(cfg, Section, DefaultPlaceholderString,
    "If this value is found in the text of a chest sign, it will be replaced by a list of contained items in that chest");
  public ConfigEntry<int> ChestSignsContentListMaxCount { get; } = BindEx(cfg, Section, 3,
    "Max number of entries to show in the content list on chest signs.");
  public ConfigEntry<string> ChestSignsContentListSeparator { get; } = BindEx(cfg, Section, "<br>",
    "Separator to use for content lists on chest signs");
  public ConfigEntry<string> ChestSignsContentListNameRest { get; } = BindEx(cfg, Section, "Other",
    "Text to show for the entry summarizing the rest of the items");
  public ConfigEntry<string> ChestSignsContentListEntryFormat { get; } = BindEx(cfg, Section, "{0} {1}",
    $"Format string for entries in the content list, the first argument is the name of the item, the second is the total number of per item.",
  new AcceptableFormatString(["Test", 0]));

  public bool AutoPickup => Shared.AutoPickup?.Value ?? false;
  public ConfigEntry<int> AutoPickupMaxRange { get; } = Shared.AutoPickupMaxRange = BindEx(cfg, Section, (int)ZoneSystem.c_ZoneSize,
    $"Max auto pickup range players can set per chest (by putting '{SignProcessor.MagnetEmoji}<Range>' on a chest sign).");
  public bool FeedFromContainers => Shared.FeedFromContainers?.Value ?? false;
  public ConfigEntry<int> FeedFromContainersMaxRange { get; } = Shared.FeedFromContainersMaxRange = BindEx(cfg, Section, (int)ZoneSystem.c_ZoneSize,
      $"Max feeding range players can set per chest (by putting '{SignProcessor.LeftRightArrowEmoji}<Range>' on a chest sign)");

  public ConfigEntry<SignOptions> WoodChestSigns { get; } = BindEx(cfg, Section, SignOptions.None,
    "Options to automatically put signs on wood chests", AcceptableEnum<SignOptions>.Default);
  public ConfigEntry<SignOptions> ReinforcedChestSigns { get; } = BindEx(cfg, Section, SignOptions.None,
    "Options to automatically put signs on reinforced chests", AcceptableEnum<SignOptions>.Default);
  public ConfigEntry<SignOptions> BlackmetalChestSigns { get; } = BindEx(cfg, Section, SignOptions.None,
    "Options to automatically put signs on blackmetal chests", AcceptableEnum<SignOptions>.Default);
  public ConfigEntry<SignOptions> BarrelSigns { get; } = BindEx(cfg, Section, SignOptions.None,
    "Options to automatically put signs on barrels", AcceptableEnum<SignOptions>.Default);
  public ConfigEntry<SignOptions> ObliteratorSigns { get; } = BindEx(cfg, Section, SignOptions.None,
    "Options to automatically put signs on obliterators", new AcceptableEnum<SignOptions>([SignOptions.Front]));

  internal SignOptions GetSignOptions(int prefab)
  {
    if (prefab == Prefabs.WoodChest)
      return WoodChestSigns.Value;
    if (prefab == Prefabs.ReinforcedChest)
      return ReinforcedChestSigns.Value;
    if (prefab == Prefabs.BlackmetalChest)
      return BlackmetalChestSigns.Value;
    if (prefab == Prefabs.Barrel)
      return BarrelSigns.Value;
    if (prefab == Prefabs.Incinerator)
      return ObliteratorSigns.Value;
    return default;
  }

  public YamlConfigEntry<ChestSignOffsetConfig> ChestSignOffsets { get; } = BindYaml<ChestSignOffsetConfig>(cfg);

  [Flags]
  public enum SignOptions
  {
    None,
    Left = (1 << 0),
    Right = (1 << 1),
    Front = (1 << 2),
    Back = (1 << 3),
    TopLongitudinal = (1 << 4),
    TopLateral = (1 << 5)
  }

  public sealed class ChestSignOffsetConfig
  {
    public sealed record ChestSignOffset(float Left, float Right, float Front, float Back, float Top) { ChestSignOffset() : this(float.NaN, float.NaN, float.NaN, float.NaN, float.NaN) { } }

    [YamlMember(Alias = nameof(ChestSignOffsets))]
    Dictionary<string, ChestSignOffset> ChestSignOffsetsYaml { get; init; } = new()
    {
      [PrefabNames.WoodChest] = new(0.8f, 0.8f, 0.4f, 0.4f, 0.8f),
      [PrefabNames.ReinforcedChest] = new(0.85f, 0.85f, 0.5f, 0.5f, 1.1f),
      [PrefabNames.BlackmetalChest] = new(0.95f, 0.95f, 0.7f, 0.7f, 0.95f),
      [PrefabNames.Barrel] = new(0.4f, 0.4f, 0.4f, 0.4f, 0.9f),
      [PrefabNames.Incinerator] = new(float.NaN, float.NaN, 0.1f, float.NaN, 3f)
    };

    [YamlIgnore]
    public IReadOnlyDictionary<int, ChestSignOffset> ChestSignOffsets => field ??= ChestSignOffsetsYaml.ToDictionary(static x => x.Key.GetStableHashCode(), static x => x.Value);
  }
}
