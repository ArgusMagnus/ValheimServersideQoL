<details open><summary><b>BloodMagic</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|SummonsLevelUpChanceAtMinSkill|-1||The chance (in percent) at skill level 0 to summon a creature with an increased level. <br>The actual chance scales linearly between this value and SummonsLevelUpChanceAtMaxSkill with skill level. <br>Set both of these values to -1 to disable this feature.|
|SummonsLevelUpChanceAtMaxSkill|-1||The chance (in percent) at skill level 100 to summon a creature with an increased level. <br>The actual chance scales linearly between this value and SummonsLevelUpChanceAtMinSkill with skill level. <br>Set both of these values to -1 to disable this feature.|
|SummonsMaxLevel|3|From 2 to 9|The maximum level a summoned creature can reach.|
|MakeSummonsFriendlyChanceAtMinSkill|-1||The chance (in percent) at skill level 0 for hostile summons to be made friendly. <br>The actual chance scales linearly between this value and MakeSummonsFriendlyChanceAtMaxSkill with skill level. <br>Set both of these values to -1 to disable this feature. <br>This will not affect summons that are already friendly by default.|
|MakeSummonsFriendlyChanceAtMaxSkill|-1||The chance (in percent) at skill level 100 for hostile summons to be made friendly. <br>The actual chance scales linearly between this value and MakeSummonsFriendlyChanceAtMinSkill with skill level. <br>Set both of these values to -1 to disable this feature. <br>This will not affect summons that are already friendly by default.|
|MakeFriendlySummonsFollow|true|True/False|True to make friendly summoned creatures follow the summoner|
|MakeSummonsTolerateLavaChanceAtMinSkill|-1||The chance (in percent) at skill level 0 for a summoned creature to tolerate lava. <br>The actual chance scales linearly between this value and MakeSummonsTolerateLavaChanceAtMaxSkill with skill level. <br>Set both of these values to -1 to disable this feature. <br>This will not affect summons that already tolerate lava by default.|
|MakeSummonsTolerateLavaChanceAtMaxSkill|-1||The chance (in percent) at skill level 0 for a summoned creature to tolerate lava. <br>The actual chance scales linearly between this value and MakeSummonsTolerateLavaChanceAtMinSkill with skill level. <br>Set both of these values to -1 to disable this feature. <br>This will not affect summons that already tolerate lava by default.|
|SummonsHPRegenMultiplierAtMinSkill|1||The time it takes for a summoned creature to fully regenerate its health at skill level 0 is multiplied by this factor. <br>The actual chance scales linearly between this value and SummonsHPRegenMultiplierAtMaxSkill with skill level. <br>Set both of these values to 1 to disable this feature.|
|SummonsHPRegenMultiplierAtMaxSkill|1||The time it takes for a summoned creature to fully regenerate its health at skill level 100 is multiplied by this factor. <br>The actual chance scales linearly between this value and SummonsHPRegenMultiplierAtMinSkill with skill level. <br>Set both of these values to 1 to disable this feature.|
|SummonsSpeedMultiplierAtMinSkill|1||The movement speed of a summoned creature at skill level 0 is multiplied by this factor. <br>The actual chance scales linearly between this value and SummonsSpeedMultiplierAtMaxSkill with skill level. <br>Set both of these values to 1 to disable this feature.|
|SummonsSpeedMultiplierAtMaxSkill|1||The movement speed of a summoned creature at skill level 0 is multiplied by this factor. <br>The actual chance scales linearly between this value and SummonsSpeedMultiplierAtMinSkill with skill level. <br>Set both of these values to 1 to disable this feature.|
|AllowReplacementSummonMinSkill|NaN||Min skill level required to allow the summoning of new hostile summons (such as summoned trolls) to replace older ones when the limit exceeded|
</details>
<details open><summary><b>Crafting</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|AreaRepairRangeAtMinSkill|0||The range around a repaired build piece in which other build pieces will be repaired as well at crafting skill level 0. <br>The actual range scales linearly between this value and AreaRepairRangeAtMaxSkill with skill level. <br>Set both of these values to 0 to disable this feature.|
|AreaRepairRangeAtMaxSkill|4||The range around a repaired build piece in which other build pieces will be repaired as well at crafting skill level 100. <br>The actual range scales linearly between this value and AreaRepairRangeAtMinSkill with skill level. <br>Set both of these values to 0 to disable this feature.|
</details>
<details open><summary><b>Pickaxe</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|RockCollapseThresholdAtMinSkill|100||The percentage of destroyed parts required to collapse a rock or ore deposit at pickaxe skill level 0. <br>The actual required percentage scales linearly between this value and RockCollapseThresholdAtMaxSkill with skill level. <br>Set both of these values to -1 to disable this feature.|
|RockCollapseThresholdAtMaxSkill|1||The percentage of destroyed parts required to collapse a rock or ore deposit at pickaxe skill level 100. <br>The actual required percentage scales linearly between this value and RockCollapseThresholdAtMinSkill with skill level. <br>Set both of these values to -1 to disable this feature.|
</details>
<details open><summary><b>Skills</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|true|True/False|Enables/disables the entire mod|
