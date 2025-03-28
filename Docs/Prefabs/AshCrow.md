## AshCrow

### Component: RandomFlyingBird (AshCrow)

|Field|Type|Default Value|
|-----|----|-------------|
|m_flyRange|System.Single|20|
|m_minAlt|System.Single|15|
|m_maxAlt|System.Single|20|
|m_speed|System.Single|15|
|m_turnRate|System.Single|60|
|m_wpDuration|System.Single|4|
|m_flapDuration|System.Single|2|
|m_sailDuration|System.Single|1.5|
|m_landChance|System.Single|0.2|
|m_landDuration|System.Single|10|
|m_avoidDangerDistance|System.Single|8|
|m_noRandomFlightAtNight|System.Boolean|True|
|m_randomNoiseIntervalMin|System.Single|5|
|m_randomNoiseIntervalMax|System.Single|10|
|m_noNoiseAtNight|System.Boolean|True|
|m_randomIdles|System.Int32|0|
|m_randomIdleTimeMin|System.Single|1|
|m_randomIdleTimeMax|System.Single|4|
|m_singleModel|System.Boolean|False|
|m_flyingModel|UnityEngine.GameObject|crow_anim|
|m_landedModel|UnityEngine.GameObject|crow_sitting|

### Component: ZSyncTransform (AshCrow)

|Field|Type|Default Value|
|-----|----|-------------|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: ZSyncAnimation (AshCrow)

|Field|Type|Default Value|
|-----|----|-------------|
|m_smoothCharacterSpeeds|System.Boolean|False|

### Component: Destructible (AshCrow)

|Field|Type|Default Value|
|-----|----|-------------|
|m_health|System.Single|1|
|m_minDamageTreshold|System.Single|0|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|False|
|m_ttl|System.Single|0|
|m_autoCreateFragments|System.Boolean|False|

### Component: DropOnDestroyed (AshCrow)

|Field|Type|Default Value|
|-----|----|-------------|
|m_spawnYOffset|System.Single|0.5|
|m_spawnYStep|System.Single|0.3|

