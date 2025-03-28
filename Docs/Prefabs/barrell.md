## barrell

### Component: Destructible (barrell)

|Field|Type|Default Value|
|---|---|---|
|m_health|System.Single|10|
|m_minDamageTreshold|System.Single|0|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|False|
|m_ttl|System.Single|0|
|m_autoCreateFragments|System.Boolean|False|

### Component: ZSyncTransform (barrell)

|Field|Type|Default Value|
|---|---|---|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: Floating (barrell)

|Field|Type|Default Value|
|---|---|---|
|m_waterLevelOffset|System.Single|0.5|
|m_forceDistance|System.Single|1|
|m_force|System.Single|0.5|
|m_balanceForceFraction|System.Single|0.02|
|m_damping|System.Single|0.05|

### Component: DropOnDestroyed (barrell)

|Field|Type|Default Value|
|---|---|---|
|m_spawnYOffset|System.Single|0.5|
|m_spawnYStep|System.Single|0.3|

