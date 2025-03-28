## VineGreen

### Component: Vine (VineGreen)

|Field|Type|Default Value|
|---|---|---|
|m_growCheckChance|System.Single|0.7|
|m_growCheckChancePerBranch|System.Single|-0.495|
|m_growChance|System.Single|1|
|m_growCheckTime|System.Int32|45|
|m_growTime|System.Single|100|
|m_growTimePerBranch|System.Single|100|
|m_closeEndChance|System.Single|0|
|m_closeEndChancePerBranch|System.Single|0.973|
|m_closeEndChancePerHeight|System.Single|0.305|
|m_maxCloseEndChance|System.Single|0.98|
|m_maxGrowUp|System.Single|1000|
|m_maxGrowDown|System.Single|-2|
|m_maxGrowWidth|System.Single|0.8|
|m_extraGrowWidthPerHeight|System.Single|0.25|
|m_maxGrowEdgeIgnoreChance|System.Single|0.291|
|m_initialGrowItterations|System.Int32|0|
|m_forceSeed|System.Int32|0|
|m_size|System.Single|1.5|
|m_growCollidersMinimum|System.Single|0.75|
|m_growSides|System.Boolean|True|
|m_growUp|System.Boolean|True|
|m_growDown|System.Boolean|False|
|m_minScale|System.Single|0.75|
|m_maxScale|System.Single|1.2|
|m_randomOffset|System.Single|0.6|
|m_maxBerriesWithinBlocker|System.Int32|0|
|m_vinePrefab|UnityEngine.GameObject|VineGreen|
|m_vineFull|UnityEngine.GameObject|VineFull|
|m_vineTop|UnityEngine.GameObject|VineTop|
|m_vineBottom|UnityEngine.GameObject|VineBottom|
|m_vineLeft|UnityEngine.GameObject|VineLeft|
|m_vineRight|UnityEngine.GameObject|VineRight|
|m_sensorBlock|UnityEngine.GameObject|BlockSensor|
|m_testItterations|System.Int32|100|

### Component: WearNTear (VineGreen)

|Field|Type|Default Value|
|---|---|---|
|m_new|UnityEngine.GameObject|VineGreen|
|m_worn|UnityEngine.GameObject|VineGreen|
|m_broken|UnityEngine.GameObject|VineGreen|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|True|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|30|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|False|

### Component: Pickable (VineGreen)

|Field|Type|Default Value|
|---|---|---|
|m_hideWhenPicked|UnityEngine.GameObject|Berries|
|m_itemPrefab|UnityEngine.GameObject|Vineberry|
|m_amount|System.Int32|3|
|m_minAmountScaled|System.Int32|1|
|m_dontScale|System.Boolean|False|
|m_overrideName|System.String||
|m_respawnTimeMinutes|System.Single|200|
|m_respawnTimeInitMin|System.Single|0|
|m_respawnTimeInitMax|System.Single|150|
|m_spawnOffset|System.Single|1|
|m_pickEffectAtSpawnPoint|System.Boolean|False|
|m_useInteractAnimation|System.Boolean|True|
|m_tarPreventsPicking|System.Boolean|False|
|m_aggravateRange|System.Single|0|
|m_defaultPicked|System.Boolean|True|
|m_defaultEnabled|System.Boolean|True|
|m_harvestable|System.Boolean|False|
|m_maxLevelBonusChance|System.Single|0.25|
|m_bonusYieldAmount|System.Int32|1|

