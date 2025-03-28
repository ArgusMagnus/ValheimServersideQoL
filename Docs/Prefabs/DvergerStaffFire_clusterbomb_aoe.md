## DvergerStaffFire_clusterbomb_aoe

### Component: ZSyncTransform (DvergerStaffFire_clusterbomb_aoe)

|Field|Type|Default Value|
|-----|----|-------------|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: Aoe (DvergerStaffFire_clusterbomb_aoe)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String||
|m_useAttackSettings|System.Boolean|False|
|m_scaleDamageByDistance|System.Boolean|False|
|m_dodgeable|System.Boolean|False|
|m_blockable|System.Boolean|False|
|m_toolTier|System.Int32|0|
|m_attackForce|System.Single|0|
|m_backstabBonus|System.Single|4|
|m_statusEffect|System.String||
|m_statusEffectIfBoss|System.String||
|m_statusEffectIfPlayer|System.String||
|m_attackForceForward|System.Boolean|False|
|m_hitTerrainOnlyOnce|System.Boolean|False|
|m_groundLavaValue|System.Single|-1|
|m_hitNoise|System.Single|0|
|m_placeOnGround|System.Boolean|False|
|m_randomRotation|System.Boolean|False|
|m_maxTargetsFromCenter|System.Int32|0|
|m_multiSpawnMin|System.Int32|0|
|m_multiSpawnMax|System.Int32|0|
|m_multiSpawnDistanceMin|System.Single|0|
|m_multiSpawnDistanceMax|System.Single|0|
|m_multiSpawnScaleMin|System.Single|0|
|m_multiSpawnScaleMax|System.Single|0|
|m_multiSpawnSpringDelayMax|System.Single|0|
|m_chainStartChance|System.Single|0|
|m_chainStartChanceFalloff|System.Single|0.8|
|m_chainChancePerTarget|System.Single|0|
|m_chainStartDelay|System.Single|0|
|m_chainMinTargets|System.Int32|0|
|m_chainMaxTargets|System.Int32|0|
|m_damageSelf|System.Single|0|
|m_hitOwner|System.Boolean|False|
|m_hitParent|System.Boolean|False|
|m_hitSame|System.Boolean|False|
|m_hitFriendly|System.Boolean|False|
|m_hitEnemy|System.Boolean|True|
|m_hitCharacters|System.Boolean|True|
|m_hitProps|System.Boolean|False|
|m_hitTerrain|System.Boolean|False|
|m_ignorePVP|System.Boolean|False|
|m_launchCharacters|System.Boolean|False|
|m_launchForceUpFactor|System.Single|0.5|
|m_canRaiseSkill|System.Boolean|True|
|m_useTriggers|System.Boolean|False|
|m_triggerEnterOnly|System.Boolean|False|
|m_radius|System.Single|3|
|m_activationDelay|System.Single|0|
|m_ttl|System.Single|7.9|
|m_ttlMax|System.Single|0|
|m_hitAfterTtl|System.Boolean|False|
|m_hitInterval|System.Single|1|
|m_hitOnEnable|System.Boolean|False|
|m_attachToCaster|System.Boolean|False|

### Component: ZSFX (sfx_fire_loop)

|Field|Type|Default Value|
|-----|----|-------------|
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
|-----|----|-------------|
|m_timeout|System.Single|5|
|m_triggerOnAwake|System.Boolean|False|
|m_forceTakeOwnershipAndDestroy|System.Boolean|False|

### Component: SmokeSpawner (SmokeSpawner)

|Field|Type|Default Value|
|-----|----|-------------|
|m_smokePrefab|UnityEngine.GameObject|SmokeBall|
|m_interval|System.Single|0.5|
|m_testRadius|System.Single|0.75|
|m_spawnRadius|System.Single|0|
|m_stopFireOnStart|System.Boolean|False|

### Component: LightFlicker (Point light)

|Field|Type|Default Value|
|-----|----|-------------|
|m_flickerIntensity|System.Single|0.1|
|m_flickerSpeed|System.Single|10|
|m_movement|System.Single|0.1|
|m_ttl|System.Single|0|
|m_fadeDuration|System.Single|0.2|
|m_fadeInDuration|System.Single|0|
|m_accessibilityBrightnessMultiplier|System.Single|1|

### Component: LightLod (Point light)

|Field|Type|Default Value|
|-----|----|-------------|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|100|
|m_shadowLod|System.Boolean|True|
|m_shadowDistance|System.Single|20|

