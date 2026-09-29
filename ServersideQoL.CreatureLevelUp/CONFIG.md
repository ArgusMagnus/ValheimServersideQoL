<details open><summary><b>CreatureLevelUp</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|True|True/False|Enables/disables the entire mod|
|ShowHigherLevelStars|True|True/False|True to show stars for higher level creatures (&gt; 2 stars)|
|SizeIncreasePerStar|0.15||The relative size increase a starred creature will have|
|MaxLevelIncrease|0||Amount the max level of creatures is incremented throughout the world. <br>The level up chance increases with the max level. <br>Example: if this value is set to 2, a creature will spawn with 4 stars with the same probability as it would spawn with 2 stars without this setting.|
|MaxLevelIncreasePerDefeatedBoss|1||Amount the max level of creatures is incremented per defeated boss. <br>The respective boss's biome and previous biomes are affected and the level up chance increases with the max level. <br>Example: If this value is set to 1 and Eikthyr and the Elder is defeated, the max creature level in the Black Forest will be raised by 1 and in the Meadows by 2.|
|MaxLevelCap|0||If &gt; 0, caps max level of creatures at this value. <br>Example: If this value is set to 5 (4 stars), creatures in lower biomes won't get a higher level than 5 <br>even when MaxLevelIncreasePerDefeatedBoss is &gt; 0 and more than 4 bosses have been defeated.|
|TreatOceanAs|BlackForest|Meadows, Swamp, Mountain, BlackForest, Plains, AshLands, DeepNorth, Mistlands|Biome to treat the ocean as for the purpose of leveling up creatures|
|LevelUpBosses|False|True/False|True to also level up bosses|
|RespawnOneTimeSpawnsCondition|AfterBossDefeated|One of Never, Always, AfterBossDefeated|Condition for one-time spawns to respawn|
|RespawnOneTimeSpawnsAfterMinutes|240||Time after one-time spawns are respawned in minutes|
