## LuredWisp

### Component: ZSyncTransform (LuredWisp)

|Field|Type|Default Value|
|---|---|---|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|False|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: LuredWisp (LuredWisp)

|Field|Type|Default Value|
|---|---|---|
|m_despawnInDaylight|System.Boolean|True|
|m_maxLureDistance|System.Single|25|
|m_acceleration|System.Single|6|
|m_noiseDistance|System.Single|1.5|
|m_noiseDistanceYScale|System.Single|0.6|
|m_noiseSpeed|System.Single|0.5|
|m_maxSpeed|System.Single|40|
|m_friction|System.Single|0.03|

### Component: Pickable (LuredWisp)

|Field|Type|Default Value|
|---|---|---|
|m_itemPrefab|UnityEngine.GameObject|Wisp|
|m_amount|System.Int32|1|
|m_minAmountScaled|System.Int32|1|
|m_dontScale|System.Boolean|False|
|m_overrideName|System.String||
|m_respawnTimeMinutes|System.Single|0|
|m_respawnTimeInitMin|System.Single|0|
|m_respawnTimeInitMax|System.Single|0|
|m_spawnOffset|System.Single|0|
|m_pickEffectAtSpawnPoint|System.Boolean|False|
|m_useInteractAnimation|System.Boolean|True|
|m_tarPreventsPicking|System.Boolean|False|
|m_aggravateRange|System.Single|0|
|m_defaultPicked|System.Boolean|False|
|m_defaultEnabled|System.Boolean|True|
|m_harvestable|System.Boolean|False|
|m_maxLevelBonusChance|System.Single|0.25|
|m_bonusYieldAmount|System.Int32|1|

### Component: LightLod (Point light)

|Field|Type|Default Value|
|---|---|---|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|15|
|m_shadowLod|System.Boolean|False|
|m_shadowDistance|System.Single|20|

