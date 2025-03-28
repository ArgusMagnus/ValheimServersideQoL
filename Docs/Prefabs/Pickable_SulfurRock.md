## Pickable_SulfurRock

### Component: Pickable (Pickable_SulfurRock)

|Field|Type|Default Value|
|---|---|---|
|m_itemPrefab|UnityEngine.GameObject|SulfurStone|
|m_amount|System.Int32|1|
|m_minAmountScaled|System.Int32|1|
|m_dontScale|System.Boolean|False|
|m_overrideName|System.String|$item_pickable_sulfurrock|
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
|m_harvestable|System.Boolean|False|
|m_maxLevelBonusChance|System.Single|0.25|
|m_bonusYieldAmount|System.Int32|1|

### Component: StaticPhysics (Pickable_SulfurRock)

|Field|Type|Default Value|
|---|---|---|
|m_pushUp|System.Boolean|True|
|m_fall|System.Boolean|True|
|m_checkSolids|System.Boolean|True|
|m_fallCheckRadius|System.Single|0|

### Component: Destructible (Pickable_SulfurRock)

|Field|Type|Default Value|
|---|---|---|
|m_health|System.Single|50|
|m_minDamageTreshold|System.Single|0|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|False|
|m_ttl|System.Single|0|
|m_spawnWhenDestroyed|UnityEngine.GameObject|SulfurStone|
|m_autoCreateFragments|System.Boolean|False|

### Component: DropOnDestroyed (Pickable_SulfurRock)

|Field|Type|Default Value|
|---|---|---|
|m_spawnYOffset|System.Single|0.5|
|m_spawnYStep|System.Single|0.3|

