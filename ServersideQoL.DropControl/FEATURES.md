Configure mob drop tables.

On startup, a template configuration file will be generated.
<details>
  <summary><b>Examples:</b></summary>

*$(ValheimInstallDir)/BepInEx/config/ArgusMagnus.{PluginName}/Drops.yml*:

```
Entries:
# Give Zil & Thungr a chance to drop a chicken egg with a chance for quality increase based on their level (number of stars)
- Name: GoblinBruteBros
  DisplayName: Zil & Thungr
  Enabled: true
  Drops:
  - Prefab: GoblinShaman_Hildir
    AmountMin: 1
    AmountMax: 1
    Chance: 1
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    QualityIncreaseChance: 0
    DoubleQualityIncreaseChancePerLevel: true
    MaxQuality: 1
    MultiplyMaxQualityByLevel: true
  - Prefab: TrophyGoblinBruteBrosBrute
    AmountMin: 1
    AmountMax: 1
    Chance: 1
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    QualityIncreaseChance: 0
    DoubleQualityIncreaseChancePerLevel: true
    MaxQuality: 1
    MultiplyMaxQualityByLevel: true
  - Prefab: ChickenEgg
    AmountMin: 1
    AmountMax: 1
    Chance: 0.25
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    QualityIncreaseChance: 0.1
    DoubleQualityIncreaseChancePerLevel: false
    MaxQuality: 1
    MultiplyMaxQualityByLevel: true
- Name: GoblinShaman_Hildir
  DisplayName: Zil
  Enabled: false
  Drops:
  - Prefab: chest_hildir3
    AmountMin: 1
    AmountMax: 1
    Chance: 1
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    QualityIncreaseChance: 0
    DoubleQualityIncreaseChancePerLevel: true
    MaxQuality: 1
    MultiplyMaxQualityByLevel: true
  - Prefab: TrophyGoblinBruteBrosShaman
    AmountMin: 1
    AmountMax: 1
    Chance: 1
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    QualityIncreaseChance: 0
    DoubleQualityIncreaseChancePerLevel: true
    MaxQuality: 1
    MultiplyMaxQualityByLevel: true
  - Prefab: ChickenEgg
    AmountMin: 1
    AmountMax: 1
    Chance: 0.25
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    QualityIncreaseChance: 0.1
    DoubleQualityIncreaseChancePerLevel: false
    MaxQuality: 1
    MultiplyMaxQualityByLevel: true
```

</details>
