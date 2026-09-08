using BepInEx.Configuration;

namespace ServersideQoL.DropControl;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "DropControl";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");

  public YamlConfigEntry<DropsConfig> Drops { get; } = BindYaml<DropsConfig>(cfg);

  public sealed class DropsConfig
  {
    public List<DropConfig> Entries { get => field ??= GetDrops(); init; }

    static List<DropConfig> GetDrops()
    {
      var result = new List<DropConfig>();
      foreach (var prefab in ZNetScene.instance.m_prefabs)
      {
        if (prefab.GetComponent<CharacterDrop>() is not { } characterDrop)
          continue;

        var character = prefab.GetComponent<Character>();

        result.Add(new()
        {
          Name = characterDrop.gameObject.name,
          DisplayName = character is null ? null : Localization.instance.Localize(character.m_name).RemoveRichTextTags(),
          Drops = [..characterDrop.m_drops.Select(static x => new DropConfig.Drop
          {
            Prefab = x.m_prefab.name,
            AmountMin = x.m_amountMin,
            AmountMax = x.m_amountMax,
            Chance = x.m_chance,
            OnePerPlayer = x.m_onePerPlayer,
            DoubleAmountAndChancePerLevel = x.m_levelMultiplier,
            IgnoreWorldResourceRate = x.m_dontScale
          })]
        });
      }
      return result;
    }

    public sealed class DropConfig
    {
      public required string Name { get; init; }
      public required string? DisplayName { get; init; }
      public bool Enabled { get; init; }
      public required List<Drop> Drops { get; init; }

      public sealed class Drop
      {
        public required string Prefab { get; init; }
        public required int AmountMin { get; init; }
        public required int AmountMax { get; init; }
        public required float Chance { get; init; }
        public required bool OnePerPlayer { get; init; }
        public required bool DoubleAmountAndChancePerLevel { get; init; }
        public required bool IgnoreWorldResourceRate { get; init; }

        public int? MinLevel { get; init; }
        public int? MaxLevel { get; init; }
        public string? RequiredGlobalKey { get; init; }
        public string? ForbiddenGlobalKey { get; init; }

        public float QualityIncreaseChance { get; init; } = 0;
        public bool DoubleQualityIncreaseChancePerLevel { get; init; } = true;
        public int MaxQuality { get; init; } = 1;
        public bool MultiplyMaxQualityByLevel { get; init; } = false;
      }
    }
  }
}
