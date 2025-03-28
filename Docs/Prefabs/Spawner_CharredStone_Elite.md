## Spawner_CharredStone_Elite

### Component: Destructible (Spawner_CharredStone_Elite)

|Field|Type|Default Value|
|-----|----|-------------|
|m_health|System.Single|400|
|m_minDamageTreshold|System.Single|0|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|False|
|m_ttl|System.Single|0|
|m_autoCreateFragments|System.Boolean|False|

### Component: SpawnArea (Spawner_CharredStone_Elite)

|Field|Type|Default Value|
|-----|----|-------------|
|m_levelupChance|System.Single|5|
|m_spawnIntervalSec|System.Single|10|
|m_triggerDistance|System.Single|60|
|m_setPatrolSpawnPoint|System.Boolean|True|
|m_spawnRadius|System.Single|4|
|m_nearRadius|System.Single|20|
|m_farRadius|System.Single|1000|
|m_maxNear|System.Int32|3|
|m_maxTotal|System.Int32|100|
|m_onGroundOnly|System.Boolean|True|

### Component: HoverText (Spawner_CharredStone_Elite)

|Field|Type|Default Value|
|-----|----|-------------|
|m_text|System.String|$enemy_charredtwitcherspawner|

### Component: DropOnDestroyed (Spawner_CharredStone_Elite)

|Field|Type|Default Value|
|-----|----|-------------|
|m_spawnYOffset|System.Single|0.5|
|m_spawnYStep|System.Single|0.3|

### Component: ZSFX (sfx_charred_spawner_loop)

|Field|Type|Default Value|
|-----|----|-------------|
|m_playOnAwake|System.Boolean|True|
|m_closedCaptionToken|System.String||
|m_secondaryCaptionToken|System.String||
|m_minimumCaptionVolume|System.Single|0.3|
|m_maxConcurrentSources|System.Int32|0|
|m_ignoreConcurrencyDistance|System.Boolean|False|
|m_maxPitch|System.Single|1|
|m_minPitch|System.Single|1|
|m_maxVol|System.Single|2|
|m_minVol|System.Single|2|
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
|m_hash|System.Int32|1743477326|

### Component: LightLod (Light)

|Field|Type|Default Value|
|-----|----|-------------|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|40|
|m_shadowLod|System.Boolean|False|
|m_shadowDistance|System.Single|20|

