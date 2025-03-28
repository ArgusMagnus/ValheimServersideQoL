## CargoCrate

### Component: Container (CargoCrate)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$ship_cargo|
|m_width|System.Int32|2|
|m_height|System.Int32|2|
|m_checkGuardStone|System.Boolean|False|
|m_autoDestroyEmpty|System.Boolean|True|

### Component: ZSyncTransform (CargoCrate)

|Field|Type|Default Value|
|---|---|---|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|True|
|m_characterParentSync|System.Boolean|False|

### Component: Floating (CargoCrate)

|Field|Type|Default Value|
|---|---|---|
|m_waterLevelOffset|System.Single|0.5|
|m_forceDistance|System.Single|1|
|m_force|System.Single|0.5|
|m_balanceForceFraction|System.Single|0.02|
|m_damping|System.Single|0.05|

### Component: Destructible (CargoCrate)

|Field|Type|Default Value|
|---|---|---|
|m_health|System.Single|50|
|m_minDamageTreshold|System.Single|0|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|False|
|m_ttl|System.Single|0|
|m_autoCreateFragments|System.Boolean|False|

