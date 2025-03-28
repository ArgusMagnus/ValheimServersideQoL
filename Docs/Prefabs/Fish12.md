## Fish12

### Component: ZSyncTransform (Fish12)

|Field|Type|Default Value|
|-----|----|-------------|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: Destructible (Fish12)

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

### Component: Fish (Fish12)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$animal_fish12|
|m_swimRange|System.Single|10|
|m_minDepth|System.Single|1.5|
|m_maxDepth|System.Single|3|
|m_speed|System.Single|3|
|m_acceleration|System.Single|0.1|
|m_turnRate|System.Single|100|
|m_wpDurationMin|System.Single|5|
|m_wpDurationMax|System.Single|15|
|m_avoidSpeedScale|System.Single|2|
|m_avoidRange|System.Single|10|
|m_height|System.Single|0.3|
|m_hookForce|System.Single|10|
|m_staminaUse|System.Single|14|
|m_escapeStaminaUse|System.Single|40|
|m_escapeMin|System.Single|1|
|m_escapeMax|System.Single|4|
|m_escapeWaitMin|System.Single|1.25|
|m_escapeWaitMax|System.Single|4|
|m_escapeMaxPerLevel|System.Single|1.5|
|m_baseHookChance|System.Single|0.1|
|m_pickupItemStackSize|System.Int32|1|
|m_blockChangeDurationMin|System.Single|0.1|
|m_blockChangeDurationMax|System.Single|0.6|
|m_collisionFleeTimeout|System.Single|1.5|
|m_jumpSpeed|System.Single|5|
|m_jumpHeight|System.Single|10|
|m_jumpForwardStrength|System.Single|11|
|m_jumpHeightLand|System.Single|8|
|m_jumpChance|System.Single|0.25|
|m_jumpOnLandChance|System.Single|0.2|
|m_jumpOnLandDecay|System.Single|0.85|
|m_maxJumpDepthOffset|System.Single|0.5|
|m_jumpFrequencySeconds|System.Single|0.3|
|m_jumpOnLandRotation|System.Single|2|
|m_waveJumpMultiplier|System.Single|0.05|
|m_jumpMaxLevel|System.Single|2|
|m_waveFollowDirection|System.Single|7|

### Component: ItemDrop (Fish12)

|Field|Type|Default Value|
|-----|----|-------------|
|m_autoPickup|System.Boolean|True|
|m_autoDestroy|System.Boolean|False|

### Component: Aoe (default spiky)

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
|m_radius|System.Single|0.5|
|m_activationDelay|System.Single|0|
|m_ttl|System.Single|0|
|m_ttlMax|System.Single|0|
|m_hitAfterTtl|System.Boolean|False|
|m_hitInterval|System.Single|2|
|m_hitOnEnable|System.Boolean|False|
|m_attachToCaster|System.Boolean|False|

### Component: RandomSpeak (DeadSpeak_Base)

|Field|Type|Default Value|
|-----|----|-------------|
|m_interval|System.Single|60|
|m_chance|System.Single|0.02|
|m_triggerDistance|System.Single|10|
|m_cullDistance|System.Single|20|
|m_ttl|System.Single|15|
|m_offset|UnityEngine.Vector3|(0.00, -0.20, 0.00)|
|m_useLargeDialog|System.Boolean|True|
|m_onlyOnce|System.Boolean|False|
|m_onlyOnItemStand|System.Boolean|True|
|m_topic|System.String||

