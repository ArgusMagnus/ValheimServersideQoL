## Pickable_SeedCarrot

### Component: Pickable (Pickable_SeedCarrot)

|Field|Type|Default Value|
|---|---|---|
|m_itemPrefab|UnityEngine.GameObject|CarrotSeeds|
|m_amount|System.Int32|3|
|m_minAmountScaled|System.Int32|1|
|m_dontScale|System.Boolean|False|
|m_overrideName|System.String||
|m_respawnTimeMinutes|System.Single|0|
|m_respawnTimeInitMin|System.Single|0|
|m_respawnTimeInitMax|System.Single|0|
|m_spawnOffset|System.Single|0.5|
|m_pickEffectAtSpawnPoint|System.Boolean|False|
|m_useInteractAnimation|System.Boolean|True|
|m_tarPreventsPicking|System.Boolean|False|
|m_aggravateRange|System.Single|0|
|m_defaultPicked|System.Boolean|False|
|m_defaultEnabled|System.Boolean|True|
|m_harvestable|System.Boolean|True|
|m_maxLevelBonusChance|System.Single|0.25|
|m_bonusYieldAmount|System.Int32|1|

### Component: Destructible (Pickable_SeedCarrot)

|Field|Type|Default Value|
|---|---|---|
|m_health|System.Single|1|
|m_minDamageTreshold|System.Single|0|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|False|
|m_ttl|System.Single|0|
|m_autoCreateFragments|System.Boolean|True|

### Component: StaticTarget (Pickable_SeedCarrot)

|Field|Type|Default Value|
|---|---|---|
|m_primaryTarget|System.Boolean|False|
|m_randomTarget|System.Boolean|True|

### Component: StaticPhysics (Pickable_SeedCarrot)

|Field|Type|Default Value|
|---|---|---|
|m_pushUp|System.Boolean|True|
|m_fall|System.Boolean|True|
|m_checkSolids|System.Boolean|False|
|m_fallCheckRadius|System.Single|0|

