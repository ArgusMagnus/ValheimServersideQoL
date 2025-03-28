## TarLiquid

### Component: LiquidVolume (TarLiquid)

|Field|Type|Default Value|
|-----|----|-------------|
|m_width|System.Int32|64|
|m_scale|System.Single|1|
|m_maxDepth|System.Single|20|
|m_physicsOffset|System.Single|-1|
|m_initialVolume|System.Single|500|
|m_initialArea|System.Int32|8|
|m_viscocity|System.Single|0.1|
|m_noiseHeight|System.Single|0.05|
|m_noiseFrequency|System.Single|4|
|m_noiseSpeed|System.Single|1|
|m_castShadow|System.Boolean|True|
|m_saveInterval|System.Single|4|
|m_randomEffectInterval|System.Single|0.1|

### Component: LiquidSurface (TriggerVolume)

### Component: Aoe (Collider_solid)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String||
|m_useAttackSettings|System.Boolean|True|
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
|m_hitParent|System.Boolean|True|
|m_hitSame|System.Boolean|False|
|m_hitFriendly|System.Boolean|True|
|m_hitEnemy|System.Boolean|True|
|m_hitCharacters|System.Boolean|True|
|m_hitProps|System.Boolean|True|
|m_hitTerrain|System.Boolean|False|
|m_ignorePVP|System.Boolean|False|
|m_launchCharacters|System.Boolean|False|
|m_launchForceUpFactor|System.Single|0.5|
|m_canRaiseSkill|System.Boolean|True|
|m_useTriggers|System.Boolean|True|
|m_triggerEnterOnly|System.Boolean|False|
|m_radius|System.Single|4|
|m_activationDelay|System.Single|0|
|m_ttl|System.Single|0|
|m_ttlMax|System.Single|0|
|m_hitAfterTtl|System.Boolean|False|
|m_hitInterval|System.Single|1|
|m_hitOnEnable|System.Boolean|False|
|m_attachToCaster|System.Boolean|False|

