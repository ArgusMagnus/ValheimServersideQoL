## FirTree_log_half

### Component: ZSyncTransform (FirTree_log_half)

|Field|Type|Default Value|
|---|---|---|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|True|
|m_characterParentSync|System.Boolean|False|

### Component: ImpactEffect (FirTree_log_half)

|Field|Type|Default Value|
|---|---|---|
|m_hitDestroyChance|System.Single|0|
|m_minVelocity|System.Single|1|
|m_maxVelocity|System.Single|5|
|m_damageToSelf|System.Boolean|False|
|m_damagePlayers|System.Boolean|True|
|m_damageFish|System.Boolean|True|
|m_toolTier|System.Int32|2|
|m_interval|System.Single|0.25|

### Component: Floating (FirTree_log_half)

|Field|Type|Default Value|
|---|---|---|
|m_waterLevelOffset|System.Single|0.3|
|m_forceDistance|System.Single|1|
|m_force|System.Single|0.5|
|m_balanceForceFraction|System.Single|0.05|
|m_damping|System.Single|0.03|

### Component: TreeLog (FirTree_log_half)

|Field|Type|Default Value|
|---|---|---|
|m_health|System.Single|40|
|m_minToolTier|System.Int32|0|
|m_useSubLogPointRotation|System.Boolean|False|
|m_spawnDistance|System.Single|2|
|m_hitNoise|System.Single|100|

### Component: HoverText (FirTree_log_half)

|Field|Type|Default Value|
|---|---|---|
|m_text|System.String|$prop_treelog|

