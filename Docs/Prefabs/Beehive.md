## Beehive

### Component: WearNTear (Beehive)

|Field|Type|Default Value|
|-----|----|-------------|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|True|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|50|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|20|
|m_destroyNoise|System.Single|20|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|False|

### Component: SpawnOnDamaged (Beehive)

|Field|Type|Default Value|
|-----|----|-------------|
|m_spawnOnDamage|UnityEngine.GameObject|bee_aoe|

### Component: DropOnDestroyed (Beehive)

|Field|Type|Default Value|
|-----|----|-------------|
|m_spawnYOffset|System.Single|0.5|
|m_spawnYStep|System.Single|0.3|

### Component: Aoe (Beehive)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String||
|m_useAttackSettings|System.Boolean|True|
|m_scaleDamageByDistance|System.Boolean|False|
|m_dodgeable|System.Boolean|True|
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
|m_hitOwner|System.Boolean|True|
|m_hitParent|System.Boolean|True|
|m_hitSame|System.Boolean|True|
|m_hitFriendly|System.Boolean|True|
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
|m_ttl|System.Single|0|
|m_ttlMax|System.Single|0|
|m_hitAfterTtl|System.Boolean|False|
|m_hitInterval|System.Single|4|
|m_hitOnEnable|System.Boolean|False|
|m_attachToCaster|System.Boolean|False|

### Component: ZSFX (SFX)

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
|m_maxVol|System.Single|0.8|
|m_minVol|System.Single|0.8|
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
|m_hash|System.Int32|-898097404|

