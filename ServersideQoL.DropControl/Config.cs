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
      var ragdolls = new HashSet<Ragdoll>();
      foreach (var prefab in ZNetScene.instance.m_prefabs)
      {
        if (prefab.GetComponent<CharacterDrop>() is not { } characterDrop)
          continue;

        if (prefab.GetComponent<Character>() is { } character && character.m_deathEffects.m_effectPrefabs
            .Select(static x => x.m_prefab.GetComponent<Ragdoll>())
            .FirstOrDefault(static x => x is not null)
            is { } ragdoll)
        {
          if (!ragdolls.Add(ragdoll))
            continue;
        }

        result.Add(new()
        {
          Name = characterDrop.gameObject.name,
          Drops = [..characterDrop.m_drops.Select(static x => new DropConfig.Drop
          {
            Prefab = x.m_prefab.name,
            AmountMin = x.m_amountMin,
            AmountMax = x.m_amountMax,
            Chance = x.m_chance,
            OnePerPlayer = x.m_onePerPlayer,
            LevelMultiplier = x.m_levelMultiplier,
            DontScale = x.m_dontScale
          })]
        });
      }
      return result;
    }

    public sealed class DropConfig
    {
      public required string Name { get; init; }
      public required List<Drop> Drops { get; init; }

      public sealed class Drop
      {
        public required string Prefab { get; init; }
        public required int AmountMin { get; init; }
        public required int AmountMax { get; init; }
        public required float Chance { get; init; }
        public required bool OnePerPlayer { get; init; }
        public required bool LevelMultiplier { get; init; }
        public required bool DontScale { get; init; }
      }
    }
  }
}
