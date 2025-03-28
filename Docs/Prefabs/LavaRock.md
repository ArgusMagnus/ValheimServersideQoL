## LavaRock

### Component: SpawnAbility (LavaRock)

|Field|Type|Default Value|
|-----|----|-------------|
|m_maxSummonReached|System.String|$hud_maxsummonsreached|
|m_spawnOnAwake|System.Boolean|True|
|m_alertSpawnedCreature|System.Boolean|True|
|m_passiveAggressive|System.Boolean|False|
|m_spawnAtTarget|System.Boolean|True|
|m_minToSpawn|System.Int32|1|
|m_maxToSpawn|System.Int32|1|
|m_maxSpawned|System.Int32|0|
|m_spawnRadius|System.Single|0|
|m_circleSpawn|System.Boolean|False|
|m_snapToTerrain|System.Boolean|True|
|m_randomYRotation|System.Boolean|False|
|m_spawnGroundOffset|System.Single|0|
|m_getSolidHeightMargin|System.Int32|1000|
|m_initialSpawnDelay|System.Single|0|
|m_spawnDelay|System.Single|0|
|m_preSpawnDelay|System.Single|2|
|m_commandOnSpawn|System.Boolean|False|
|m_wakeUpAnimation|System.Boolean|False|
|m_copySkillToRandomFactor|System.Single|0|
|m_setMaxInstancesFromWeaponLevel|System.Boolean|False|
|m_maxTargetRange|System.Single|40|
|m_projectileVelocity|System.Single|10|
|m_projectileVelocityMax|System.Single|16|
|m_projectileAccuracy|System.Single|10|
|m_randomDirection|System.Boolean|True|
|m_randomAngleMin|System.Single|0|
|m_randomAngleMax|System.Single|0.32|

### Component: ZSyncTransform (LavaRock)

|Field|Type|Default Value|
|-----|----|-------------|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: TimedDestruction (LavaRock)

|Field|Type|Default Value|
|-----|----|-------------|
|m_timeout|System.Single|4|
|m_triggerOnAwake|System.Boolean|True|
|m_forceTakeOwnershipAndDestroy|System.Boolean|False|

