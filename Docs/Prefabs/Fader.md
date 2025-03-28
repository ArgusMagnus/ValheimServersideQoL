## Fader

### Component: Humanoid (Fader)

|Field|Type|Default Value|
|---|---|---|
|m_equipStaminaDrain|System.Single|10|
|m_blockStaminaDrain|System.Single|25|
|m_name|System.String|$enemy_fader|
|m_group|System.String||
|m_boss|System.Boolean|True|
|m_dontHideBossHud|System.Boolean|True|
|m_bossEvent|System.String|boss_fader|
|m_defeatSetGlobalKey|System.String|defeated_fader|
|m_aiSkipTarget|System.Boolean|False|
|m_crouchSpeed|System.Single|2|
|m_walkSpeed|System.Single|4|
|m_speed|System.Single|4|
|m_turnSpeed|System.Single|120|
|m_runSpeed|System.Single|8|
|m_runTurnSpeed|System.Single|120|
|m_flySlowSpeed|System.Single|12|
|m_flyFastSpeed|System.Single|12|
|m_flyTurnSpeed|System.Single|250|
|m_acceleration|System.Single|0.8|
|m_jumpForce|System.Single|10|
|m_jumpForceForward|System.Single|0|
|m_jumpForceTiredFactor|System.Single|0.7|
|m_airControl|System.Single|0.1|
|m_canSwim|System.Boolean|True|
|m_swimDepth|System.Single|4.44|
|m_swimSpeed|System.Single|4|
|m_swimTurnSpeed|System.Single|60|
|m_swimAcceleration|System.Single|0.05|
|m_groundTiltSpeed|System.Single|100|
|m_flying|System.Boolean|False|
|m_jumpStaminaUsage|System.Single|10|
|m_disableWhileSleeping|System.Boolean|False|
|m_tolerateWater|System.Boolean|True|
|m_tolerateFire|System.Boolean|True|
|m_tolerateSmoke|System.Boolean|False|
|m_tolerateTar|System.Boolean|False|
|m_health|System.Single|25000|
|m_staggerWhenBlocked|System.Boolean|False|
|m_staggerDamageFactor|System.Single|0|
|m_heatBuildupBase|System.Single|1.5|
|m_heatCooldownBase|System.Single|1|
|m_heatBuildupWater|System.Single|2|
|m_heatWaterTouchMultiplier|System.Single|0.2|
|m_lavaDamageTickInterval|System.Single|0.2|
|m_heatLevelFirstDamageThreshold|System.Single|0.7|
|m_lavaFirstDamage|System.Single|10|
|m_lavaFullDamage|System.Single|100|
|m_lavaAirDamageHeight|System.Single|3|
|m_dayHeatGainRunning|System.Single|0.2|
|m_dayHeatGainStill|System.Single|-0.05|
|m_dayHeatEquipmentStop|System.Single|0.5|
|m_lavaSlowMax|System.Single|0.5|
|m_lavaSlowHeight|System.Single|0.8|

### Component: ZSyncTransform (Fader)

|Field|Type|Default Value|
|---|---|---|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: ZSyncAnimation (Fader)

|Field|Type|Default Value|
|---|---|---|
|m_smoothCharacterSpeeds|System.Boolean|True|

### Component: MonsterAI (Fader)

|Field|Type|Default Value|
|---|---|---|
|m_alertRange|System.Single|100|
|m_fleeIfHurtWhenTargetCantBeReached|System.Boolean|True|
|m_fleeUnreachableSinceAttacking|System.Single|30|
|m_fleeUnreachableSinceHurt|System.Single|20|
|m_fleeIfNotAlerted|System.Boolean|False|
|m_fleeIfLowHealth|System.Single|0|
|m_fleeTimeSinceHurt|System.Single|20|
|m_fleeInLava|System.Boolean|True|
|m_fleePheromoneMin|System.Single|3|
|m_fleePheromoneMax|System.Single|8|
|m_circulateWhileCharging|System.Boolean|False|
|m_circulateWhileChargingFlying|System.Boolean|False|
|m_enableHuntPlayer|System.Boolean|False|
|m_attackPlayerObjects|System.Boolean|True|
|m_privateAreaTriggerTreshold|System.Int32|4|
|m_interceptTimeMax|System.Single|0|
|m_interceptTimeMin|System.Single|0|
|m_maxChaseDistance|System.Single|0|
|m_minAttackInterval|System.Single|0.5|
|m_circleTargetInterval|System.Single|0|
|m_circleTargetDuration|System.Single|5|
|m_circleTargetDistance|System.Single|6|
|m_sleeping|System.Boolean|False|
|m_wakeupRange|System.Single|5|
|m_noiseWakeup|System.Boolean|False|
|m_maxNoiseWakeupRange|System.Single|50|
|m_wakeUpDelayMin|System.Single|0|
|m_wakeUpDelayMax|System.Single|0|
|m_avoidLand|System.Boolean|False|
|m_consumeRange|System.Single|2|
|m_consumeSearchRange|System.Single|5|
|m_consumeSearchInterval|System.Single|10|
|m_viewRange|System.Single|30|
|m_viewAngle|System.Single|90|
|m_hearRange|System.Single|9999|
|m_mistVision|System.Boolean|True|
|m_idleSoundInterval|System.Single|7|
|m_idleSoundChance|System.Single|0.5|
|m_moveMinAngle|System.Single|30|
|m_smoothMovement|System.Boolean|True|
|m_serpentMovement|System.Boolean|False|
|m_serpentTurnRadius|System.Single|20|
|m_jumpInterval|System.Single|0|
|m_randomCircleInterval|System.Single|5|
|m_randomMoveInterval|System.Single|4|
|m_randomMoveRange|System.Single|10|
|m_randomFly|System.Boolean|False|
|m_chanceToTakeoff|System.Single|1|
|m_chanceToLand|System.Single|1|
|m_groundDuration|System.Single|10|
|m_airDuration|System.Single|10|
|m_maxLandAltitude|System.Single|4|
|m_takeoffTime|System.Single|1|
|m_flyAltitudeMin|System.Single|3|
|m_flyAltitudeMax|System.Single|6|
|m_flyAbsMinAltitude|System.Single|32|
|m_avoidFire|System.Boolean|False|
|m_afraidOfFire|System.Boolean|False|
|m_avoidWater|System.Boolean|True|
|m_avoidLava|System.Boolean|False|
|m_skipLavaTargets|System.Boolean|False|
|m_avoidLavaFlee|System.Boolean|True|
|m_aggravatable|System.Boolean|False|
|m_passiveAggresive|System.Boolean|False|
|m_spawnMessage|System.String||
|m_deathMessage|System.String|$enemy_boss_fader_deathmessage|
|m_alertedMessage|System.String|$enemy_boss_fader_alertmessage|
|m_fleeRange|System.Single|25|
|m_fleeAngle|System.Single|45|
|m_fleeInterval|System.Single|2|

### Component: CharacterDrop (Fader)

|Field|Type|Default Value|
|---|---|---|
|m_spawnOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|

### Component: FootStep (Fader)

|Field|Type|Default Value|
|---|---|---|
|m_footlessFootsteps|System.Boolean|False|
|m_footlessTriggerDistance|System.Single|1|
|m_footstepCullDistance|System.Single|50|

### Component: MovementDamage (Fader)

|Field|Type|Default Value|
|---|---|---|
|m_runDamageObject|UnityEngine.GameObject|RunHitDamager|
|m_speedTreshold|System.Single|1|

### Component: DropProjectileOverDistance (Fader)

|Field|Type|Default Value|
|---|---|---|
|m_projectilePrefab|UnityEngine.GameObject|Fader_DroppedFire_AOE|
|m_distancePerProjectile|System.Single|5|
|m_spawnHeight|System.Single|1|
|m_snapToGround|System.Boolean|True|
|m_timeToForceSpawn|System.Single|10|
|m_minVelocity|System.Single|0|
|m_maxVelocity|System.Single|0|

### Component: Aoe (RunHitDamager)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String||
|m_useAttackSettings|System.Boolean|False|
|m_scaleDamageByDistance|System.Boolean|False|
|m_dodgeable|System.Boolean|True|
|m_blockable|System.Boolean|True|
|m_toolTier|System.Int32|0|
|m_attackForce|System.Single|150|
|m_backstabBonus|System.Single|1|
|m_statusEffect|System.String||
|m_statusEffectIfBoss|System.String||
|m_statusEffectIfPlayer|System.String||
|m_attackForceForward|System.Boolean|True|
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
|m_hitEnemy|System.Boolean|False|
|m_hitCharacters|System.Boolean|False|
|m_hitProps|System.Boolean|True|
|m_hitTerrain|System.Boolean|False|
|m_ignorePVP|System.Boolean|False|
|m_launchCharacters|System.Boolean|False|
|m_launchForceUpFactor|System.Single|0.5|
|m_canRaiseSkill|System.Boolean|True|
|m_useTriggers|System.Boolean|True|
|m_triggerEnterOnly|System.Boolean|True|
|m_radius|System.Single|4|
|m_activationDelay|System.Single|0|
|m_ttl|System.Single|0|
|m_ttlMax|System.Single|0|
|m_hitAfterTtl|System.Boolean|False|
|m_hitInterval|System.Single|4|
|m_hitOnEnable|System.Boolean|False|
|m_attachToCaster|System.Boolean|False|

### Component: AnimationEffect (Visual)

|Field|Type|Default Value|
|---|---|---|

### Component: CharacterAnimEvent (Visual)

|Field|Type|Default Value|
|---|---|---|
|m_footIK|System.Boolean|False|
|m_footDownMax|System.Single|1|
|m_footOffset|System.Single|0.2|
|m_footStepHeight|System.Single|5|
|m_stabalizeDistance|System.Single|0|
|m_useFeetValues|System.Boolean|True|
|m_headRotation|System.Boolean|False|
|m_lookWeight|System.Single|0.5|
|m_bodyLookWeight|System.Single|0.1|
|m_headLookWeight|System.Single|1|
|m_eyeLookWeight|System.Single|0|
|m_lookClamp|System.Single|0.8|
|m_femaleHack|System.Boolean|False|
|m_femaleOffset|System.Single|0.0004|
|m_maleOffset|System.Single|0.0007651657|

