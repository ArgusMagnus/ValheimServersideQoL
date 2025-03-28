## EvilHeart_Swamp

### Component: Destructible (EvilHeart_Swamp)

|Field|Type|Default Value|
|-----|----|-------------|
|m_health|System.Single|100|
|m_minDamageTreshold|System.Single|0|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|False|
|m_ttl|System.Single|0|
|m_autoCreateFragments|System.Boolean|False|

### Component: SpawnArea (EvilHeart_Swamp)

|Field|Type|Default Value|
|-----|----|-------------|
|m_levelupChance|System.Single|15|
|m_spawnIntervalSec|System.Single|20|
|m_triggerDistance|System.Single|256|
|m_setPatrolSpawnPoint|System.Boolean|True|
|m_spawnRadius|System.Single|4|
|m_nearRadius|System.Single|10|
|m_farRadius|System.Single|1000|
|m_maxNear|System.Int32|3|
|m_maxTotal|System.Int32|30|
|m_onGroundOnly|System.Boolean|False|

### Component: DropOnDestroyed (EvilHeart_Swamp)

|Field|Type|Default Value|
|-----|----|-------------|
|m_spawnYOffset|System.Single|0.5|
|m_spawnYStep|System.Single|0.3|

### Component: LightFlicker (Point light)

|Field|Type|Default Value|
|-----|----|-------------|
|m_flickerIntensity|System.Single|0.1|
|m_flickerSpeed|System.Single|10|
|m_movement|System.Single|0.1|
|m_ttl|System.Single|0|
|m_fadeDuration|System.Single|1|
|m_fadeInDuration|System.Single|0|
|m_accessibilityBrightnessMultiplier|System.Single|1|

### Component: ZSFX (sfx)

|Field|Type|Default Value|
|-----|----|-------------|
|m_playOnAwake|System.Boolean|True|
|m_closedCaptionToken|System.String||
|m_secondaryCaptionToken|System.String||
|m_minimumCaptionVolume|System.Single|0.3|
|m_maxConcurrentSources|System.Int32|0|
|m_ignoreConcurrencyDistance|System.Boolean|False|
|m_maxPitch|System.Single|1|
|m_minPitch|System.Single|0.9|
|m_maxVol|System.Single|1|
|m_minVol|System.Single|1|
|m_fadeInDuration|System.Single|0|
|m_fadeOutDuration|System.Single|0|
|m_fadeOutDelay|System.Single|0|
|m_fadeOutOnAwake|System.Boolean|False|
|m_randomPan|System.Boolean|False|
|m_minPan|System.Single|-1|
|m_maxPan|System.Single|1|
|m_maxDelay|System.Single|0|
|m_minDelay|System.Single|0|
|m_distanceReverb|System.Boolean|True|
|m_useCustomReverbDistance|System.Boolean|False|
|m_customReverbDistance|System.Single|10|
|m_hash|System.Int32|-2039209986|

