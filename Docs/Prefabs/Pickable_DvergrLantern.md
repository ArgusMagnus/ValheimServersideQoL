## Pickable_DvergrLantern

### Component: Pickable (Pickable_DvergrLantern)

|Field|Type|Default Value|
|---|---|---|
|m_itemPrefab|UnityEngine.GameObject|Lantern|
|m_amount|System.Int32|1|
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
|m_harvestable|System.Boolean|False|
|m_maxLevelBonusChance|System.Single|0.25|
|m_bonusYieldAmount|System.Int32|1|

### Component: StaticPhysics (Pickable_DvergrLantern)

|Field|Type|Default Value|
|---|---|---|
|m_pushUp|System.Boolean|True|
|m_fall|System.Boolean|True|
|m_checkSolids|System.Boolean|True|
|m_fallCheckRadius|System.Single|0|

### Component: LightFlicker (Point Light)

|Field|Type|Default Value|
|---|---|---|
|m_flickerIntensity|System.Single|0.1|
|m_flickerSpeed|System.Single|10|
|m_movement|System.Single|0.1|
|m_ttl|System.Single|0|
|m_fadeDuration|System.Single|0.2|
|m_fadeInDuration|System.Single|0|
|m_accessibilityBrightnessMultiplier|System.Single|1|

### Component: LightLod (Point Light)

|Field|Type|Default Value|
|---|---|---|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|30|
|m_shadowLod|System.Boolean|True|
|m_shadowDistance|System.Single|10|

