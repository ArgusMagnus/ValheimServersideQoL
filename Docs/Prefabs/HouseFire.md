## HouseFire

### Component: LightLod (HouseFire)

|Field|Type|Default Value|
|---|---|---|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|40|
|m_shadowLod|System.Boolean|False|
|m_shadowDistance|System.Single|20|

### Component: Fire (HouseFire)

|Field|Type|Default Value|
|---|---|---|
|m_dotInterval|System.Single|1|
|m_dotRadius|System.Single|1|
|m_fireDamage|System.Single|15|
|m_chopDamage|System.Single|9|
|m_spread|System.Int32|4|
|m_updateRate|System.Single|2|
|m_terrainHitDelay|System.Single|5|
|m_terrainMaxDist|System.Single|1.5|
|m_terrainCheckCultivated|System.Boolean|True|
|m_terrainCheckCleared|System.Boolean|False|
|m_terrainHitSpawn|UnityEngine.GameObject|burn|
|m_fuelBurnChance|System.Single|0.5|
|m_fuelBurnAmount|System.Single|0.5|
|m_smokeCheckHeight|System.Single|1.14|
|m_smokeCheckRadius|System.Single|0.75|
|m_smokeOxygenCheckHeight|System.Single|3.17|
|m_smokeOxygenCheckRadius|System.Single|2.88|
|m_smokeSuffocationPerHit|System.Single|0.2|
|m_oxygenSmokeTolerance|System.Int32|7|
|m_oxygenInteriorChecks|System.Int32|5|
|m_smokeDieChance|System.Single|1|
|m_maxSmoke|System.Single|4|

### Component: CinderSpawner (HouseFire)

|Field|Type|Default Value|
|---|---|---|
|m_cinderPrefab|UnityEngine.GameObject|Cinder|
|m_cinderInterval|System.Single|2|
|m_cinderChance|System.Single|0.3|
|m_cinderVel|System.Single|5|
|m_spawnOffset|System.Single|1|
|m_spawnOffsetPoint|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_spread|System.Int32|4|
|m_instancesPerSpawn|System.Int32|1|
|m_spawnOnAwake|System.Boolean|False|
|m_spawnOnProjectileHit|System.Boolean|False|

### Component: TimedDestruction (HouseFire)

|Field|Type|Default Value|
|---|---|---|
|m_timeout|System.Single|30|
|m_triggerOnAwake|System.Boolean|True|
|m_forceTakeOwnershipAndDestroy|System.Boolean|True|

### Component: ZSFX (sfx_fire_loop)

|Field|Type|Default Value|
|---|---|---|
|m_playOnAwake|System.Boolean|True|
|m_closedCaptionToken|System.String||
|m_secondaryCaptionToken|System.String||
|m_minimumCaptionVolume|System.Single|0.3|
|m_maxConcurrentSources|System.Int32|2|
|m_ignoreConcurrencyDistance|System.Boolean|False|
|m_maxPitch|System.Single|1.1|
|m_minPitch|System.Single|0.9|
|m_maxVol|System.Single|1|
|m_minVol|System.Single|0.6|
|m_fadeInDuration|System.Single|0.4|
|m_fadeOutDuration|System.Single|0.4|
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
|m_hash|System.Int32|-851502317|

### Component: TimedDestruction (sfx_fire_loop)

|Field|Type|Default Value|
|---|---|---|
|m_timeout|System.Single|5|
|m_triggerOnAwake|System.Boolean|False|
|m_forceTakeOwnershipAndDestroy|System.Boolean|False|

### Component: SmokeSpawner (SmokeSpawner)

|Field|Type|Default Value|
|---|---|---|
|m_smokePrefab|UnityEngine.GameObject|SmokeBallTurbulent|
|m_interval|System.Single|0.4|
|m_testRadius|System.Single|0.75|
|m_spawnRadius|System.Single|0|
|m_stopFireOnStart|System.Boolean|False|

