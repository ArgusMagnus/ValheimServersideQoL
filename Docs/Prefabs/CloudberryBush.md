## CloudberryBush

### Component: Destructible (CloudberryBush)

|Field|Type|Default Value|
|---|---|---|
|m_health|System.Single|30|
|m_minDamageTreshold|System.Single|0|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|False|
|m_ttl|System.Single|0|
|m_autoCreateFragments|System.Boolean|False|

### Component: Pickable (CloudberryBush)

|Field|Type|Default Value|
|---|---|---|
|m_hideWhenPicked|UnityEngine.GameObject|Berrys|
|m_itemPrefab|UnityEngine.GameObject|Cloudberry|
|m_amount|System.Int32|1|
|m_minAmountScaled|System.Int32|1|
|m_dontScale|System.Boolean|False|
|m_overrideName|System.String||
|m_respawnTimeMinutes|System.Single|300|
|m_respawnTimeInitMin|System.Single|0|
|m_respawnTimeInitMax|System.Single|0|
|m_spawnOffset|System.Single|1|
|m_pickEffectAtSpawnPoint|System.Boolean|False|
|m_useInteractAnimation|System.Boolean|True|
|m_tarPreventsPicking|System.Boolean|False|
|m_aggravateRange|System.Single|0|
|m_defaultPicked|System.Boolean|False|
|m_defaultEnabled|System.Boolean|True|
|m_harvestable|System.Boolean|False|
|m_maxLevelBonusChance|System.Single|0.25|
|m_bonusYieldAmount|System.Int32|1|

### Component: StaticPhysics (CloudberryBush)

|Field|Type|Default Value|
|---|---|---|
|m_pushUp|System.Boolean|True|
|m_fall|System.Boolean|True|
|m_checkSolids|System.Boolean|False|
|m_fallCheckRadius|System.Single|0|

