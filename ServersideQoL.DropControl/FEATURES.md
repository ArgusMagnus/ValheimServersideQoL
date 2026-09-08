Configure mob drop tables.

On startup, a template configuration file will be generated.

|Field|Description|
|-----|-----------|
|Prefab|The name of the item to drop. This is the name of the prefab in the game files, not the display name. Technically non-item prefabs (like creatures) should also work, but that's not tested. See https://valheim-modding.github.io/Jotunn/data/objects/item-list.html|
|AmountMin|The minimum amount to drop. The actual amount is chosen randomly between `AmountMin` and `AmountMax`.|
|AmountMax|The maximum amount to drop.|
|Chance|The probability of dropping the item. 0 = 0%, 1 = 100%|
|OnePerPlayer|If true, exactly one item per player is dropped, other fields which influence the dropped amount are ignored.|
|DoubleAmountAndChancePerLevel|If true, `AmountMin`,`AmountMax` and `Chance` are doubled for each additional level (star) of the killed creature.|
|IgnoreWorldResourceRate|If true, world modifiers that change the amount of resources dropped have no effect for this drop.|
|MinLevel|If set, the minimum level the killed creature needs to have for this drop.|
|MaxLevel|If set, the maximum level the killed creature needs to have for this drop.| 
|RequiredGlobalKey|If set, the global key that needs to be set for this drop.| 
|ForbiddenGlobalKey|If set, the global key must not be set for this drop.| 
|QualityIncreaseChance|The probability for the dropped item to gain an additional quality level (upgrade level of gear/level of fish/level of eggs). 0 = 0%, 1 = 100%|
|DoubleQualityIncreaseChancePerLevel|If true, `QualityIncreaseChance` is doubled for each additional level (star) of the killed creature.|
|MaxQuality|The maximum quality the drop can be.|
|MultiplyMaxQualityByLevel|If true, `MaxQuality` is multiplied by the level of the killed creature. `QualityIncreaseChance` is adjusted so that the chance to get a max quality item stays the same across creature levels.|

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
    MinLevel: 
    MaxLevel: 
    RequiredGlobalKey: 
    ForbiddenGlobalKey: 
    QualityIncreaseChance: 0
    DoubleQualityIncreaseChancePerLevel: true
    MaxQuality: 1
    MultiplyMaxQualityByLevel: false
  - Prefab: TrophyGoblinBruteBrosBrute
    AmountMin: 1
    AmountMax: 1
    Chance: 1
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    MinLevel: 
    MaxLevel: 
    RequiredGlobalKey: 
    ForbiddenGlobalKey: 
    QualityIncreaseChance: 0
    DoubleQualityIncreaseChancePerLevel: true
    MaxQuality: 1
    MultiplyMaxQualityByLevel: false
  - Prefab: ChickenEgg
    AmountMin: 1
    AmountMax: 1
    Chance: 0.25
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    MinLevel: 
    MaxLevel: 
    RequiredGlobalKey: 
    ForbiddenGlobalKey: 
    QualityIncreaseChance: 0.25
    DoubleQualityIncreaseChancePerLevel: false
    MaxQuality: 1
    MultiplyMaxQualityByLevel: true
- Name: GoblinBruteBros_nochest
  DisplayName: Zil & Thungr
  Enabled: true
  Drops:
  - Prefab: GoblinShaman_Hildir_nochest
    AmountMin: 1
    AmountMax: 1
    Chance: 1
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    MinLevel: 
    MaxLevel: 
    RequiredGlobalKey: 
    ForbiddenGlobalKey: 
    QualityIncreaseChance: 0
    DoubleQualityIncreaseChancePerLevel: true
    MaxQuality: 1
    MultiplyMaxQualityByLevel: false
  - Prefab: TrophyGoblinBruteBrosBrute
    AmountMin: 1
    AmountMax: 1
    Chance: 1
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    MinLevel: 
    MaxLevel: 
    RequiredGlobalKey: 
    ForbiddenGlobalKey: 
    QualityIncreaseChance: 0
    DoubleQualityIncreaseChancePerLevel: true
    MaxQuality: 1
    MultiplyMaxQualityByLevel: false
  - Prefab: ChickenEgg
    AmountMin: 1
    AmountMax: 1
    Chance: 0.25
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    MinLevel: 
    MaxLevel: 
    RequiredGlobalKey: 
    ForbiddenGlobalKey: 
    QualityIncreaseChance: 0.25
    DoubleQualityIncreaseChancePerLevel: false
    MaxQuality: 1
    MultiplyMaxQualityByLevel: true
- Name: GoblinShaman_Hildir
  DisplayName: Zil
  Enabled: true
  Drops:
  - Prefab: chest_hildir3
    AmountMin: 1
    AmountMax: 1
    Chance: 1
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    MinLevel: 
    MaxLevel: 
    RequiredGlobalKey: 
    ForbiddenGlobalKey: 
    QualityIncreaseChance: 0
    DoubleQualityIncreaseChancePerLevel: true
    MaxQuality: 1
    MultiplyMaxQualityByLevel: false
  - Prefab: TrophyGoblinBruteBrosShaman
    AmountMin: 1
    AmountMax: 1
    Chance: 1
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    MinLevel: 
    MaxLevel: 
    RequiredGlobalKey: 
    ForbiddenGlobalKey: 
    QualityIncreaseChance: 0
    DoubleQualityIncreaseChancePerLevel: true
    MaxQuality: 1
    MultiplyMaxQualityByLevel: false
  - Prefab: ChickenEgg
    AmountMin: 1
    AmountMax: 1
    Chance: 0.25
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    MinLevel: 
    MaxLevel: 
    RequiredGlobalKey: 
    ForbiddenGlobalKey: 
    QualityIncreaseChance: 0.25
    DoubleQualityIncreaseChancePerLevel: false
    MaxQuality: 1
    MultiplyMaxQualityByLevel: true
- Name: GoblinShaman_Hildir_nochest
  DisplayName: Zil
  Enabled: true
  Drops:
  - Prefab: TrophyGoblinBruteBrosShaman
    AmountMin: 1
    AmountMax: 1
    Chance: 1
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    MinLevel: 
    MaxLevel: 
    RequiredGlobalKey: 
    ForbiddenGlobalKey: 
    QualityIncreaseChance: 0
    DoubleQualityIncreaseChancePerLevel: true
    MaxQuality: 1
    MultiplyMaxQualityByLevel: false
  - Prefab: ChickenEgg
    AmountMin: 1
    AmountMax: 1
    Chance: 0.25
    OnePerPlayer: false
    DoubleAmountAndChancePerLevel: false
    IgnoreWorldResourceRate: true
    MinLevel: 
    MaxLevel: 
    RequiredGlobalKey: 
    ForbiddenGlobalKey: 
    QualityIncreaseChance: 0.25
    DoubleQualityIncreaseChancePerLevel: false
    MaxQuality: 1
    MultiplyMaxQualityByLevel: true
```

</details>
