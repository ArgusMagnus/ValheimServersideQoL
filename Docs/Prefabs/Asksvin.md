## Asksvin

### Component: Humanoid (Asksvin)

|Field|Type|Default Value|
|---|---|---|
|m_equipStaminaDrain|System.Single|10|
|m_blockStaminaDrain|System.Single|25|
|m_name|System.String|$enemy_asksvin|
|m_group|System.String|asksvin|
|m_boss|System.Boolean|False|
|m_dontHideBossHud|System.Boolean|False|
|m_bossEvent|System.String||
|m_defeatSetGlobalKey|System.String||
|m_aiSkipTarget|System.Boolean|False|
|m_crouchSpeed|System.Single|2|
|m_walkSpeed|System.Single|3|
|m_speed|System.Single|7|
|m_turnSpeed|System.Single|95|
|m_runSpeed|System.Single|9|
|m_runTurnSpeed|System.Single|120|
|m_flySlowSpeed|System.Single|5|
|m_flyFastSpeed|System.Single|12|
|m_flyTurnSpeed|System.Single|12|
|m_acceleration|System.Single|0.5|
|m_jumpForce|System.Single|10|
|m_jumpForceForward|System.Single|0|
|m_jumpForceTiredFactor|System.Single|0.7|
|m_airControl|System.Single|0.1|
|m_canSwim|System.Boolean|True|
|m_swimDepth|System.Single|2.7|
|m_swimSpeed|System.Single|3|
|m_swimTurnSpeed|System.Single|50|
|m_swimAcceleration|System.Single|0.05|
|m_groundTiltSpeed|System.Single|50|
|m_flying|System.Boolean|False|
|m_jumpStaminaUsage|System.Single|10|
|m_disableWhileSleeping|System.Boolean|False|
|m_tolerateWater|System.Boolean|True|
|m_tolerateFire|System.Boolean|True|
|m_tolerateSmoke|System.Boolean|True|
|m_tolerateTar|System.Boolean|True|
|m_health|System.Single|800|
|m_staggerWhenBlocked|System.Boolean|True|
|m_staggerDamageFactor|System.Single|0.3|
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

### Component: MonsterAI (Asksvin)

|Field|Type|Default Value|
|---|---|---|
|m_alertRange|System.Single|10|
|m_fleeIfHurtWhenTargetCantBeReached|System.Boolean|True|
|m_fleeUnreachableSinceAttacking|System.Single|30|
|m_fleeUnreachableSinceHurt|System.Single|20|
|m_fleeIfNotAlerted|System.Boolean|False|
|m_fleeIfLowHealth|System.Single|0|
|m_fleeTimeSinceHurt|System.Single|20|
|m_fleeInLava|System.Boolean|False|
|m_fleePheromoneMin|System.Single|3|
|m_fleePheromoneMax|System.Single|8|
|m_circulateWhileCharging|System.Boolean|False|
|m_circulateWhileChargingFlying|System.Boolean|False|
|m_enableHuntPlayer|System.Boolean|False|
|m_attackPlayerObjects|System.Boolean|True|
|m_privateAreaTriggerTreshold|System.Int32|4|
|m_interceptTimeMax|System.Single|2|
|m_interceptTimeMin|System.Single|0|
|m_maxChaseDistance|System.Single|0|
|m_minAttackInterval|System.Single|2|
|m_circleTargetInterval|System.Single|30|
|m_circleTargetDuration|System.Single|3|
|m_circleTargetDistance|System.Single|5|
|m_sleeping|System.Boolean|False|
|m_wakeupRange|System.Single|5|
|m_noiseWakeup|System.Boolean|False|
|m_maxNoiseWakeupRange|System.Single|50|
|m_wakeUpDelayMin|System.Single|0|
|m_wakeUpDelayMax|System.Single|0|
|m_avoidLand|System.Boolean|False|
|m_consumeRange|System.Single|4|
|m_consumeSearchRange|System.Single|10|
|m_consumeSearchInterval|System.Single|10|
|m_viewRange|System.Single|35|
|m_viewAngle|System.Single|150|
|m_hearRange|System.Single|30|
|m_mistVision|System.Boolean|False|
|m_idleSoundInterval|System.Single|20|
|m_idleSoundChance|System.Single|0.5|
|m_moveMinAngle|System.Single|90|
|m_smoothMovement|System.Boolean|True|
|m_serpentMovement|System.Boolean|False|
|m_serpentTurnRadius|System.Single|20|
|m_jumpInterval|System.Single|0|
|m_randomCircleInterval|System.Single|2|
|m_randomMoveInterval|System.Single|40|
|m_randomMoveRange|System.Single|20|
|m_randomFly|System.Boolean|False|
|m_chanceToTakeoff|System.Single|1|
|m_chanceToLand|System.Single|1|
|m_groundDuration|System.Single|10|
|m_airDuration|System.Single|10|
|m_maxLandAltitude|System.Single|5|
|m_takeoffTime|System.Single|5|
|m_flyAltitudeMin|System.Single|3|
|m_flyAltitudeMax|System.Single|10|
|m_flyAbsMinAltitude|System.Single|32|
|m_avoidFire|System.Boolean|False|
|m_afraidOfFire|System.Boolean|False|
|m_avoidWater|System.Boolean|True|
|m_avoidLava|System.Boolean|True|
|m_skipLavaTargets|System.Boolean|False|
|m_avoidLavaFlee|System.Boolean|True|
|m_aggravatable|System.Boolean|False|
|m_passiveAggresive|System.Boolean|False|
|m_spawnMessage|System.String||
|m_deathMessage|System.String||
|m_alertedMessage|System.String||
|m_fleeRange|System.Single|25|
|m_fleeAngle|System.Single|45|
|m_fleeInterval|System.Single|2|

### Component: CharacterDrop (Asksvin)

|Field|Type|Default Value|
|---|---|---|
|m_spawnOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|

### Component: ZSyncTransform (Asksvin)

|Field|Type|Default Value|
|---|---|---|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|True|
|m_characterParentSync|System.Boolean|False|

### Component: ZSyncAnimation (Asksvin)

|Field|Type|Default Value|
|---|---|---|
|m_smoothCharacterSpeeds|System.Boolean|True|

### Component: FootStep (Asksvin)

|Field|Type|Default Value|
|---|---|---|
|m_footlessFootsteps|System.Boolean|False|
|m_footlessTriggerDistance|System.Single|1|
|m_footstepCullDistance|System.Single|20|

### Component: Tameable (Asksvin)

|Field|Type|Default Value|
|---|---|---|
|m_fedDuration|System.Single|600|
|m_tamingTime|System.Single|1800|
|m_startsTamed|System.Boolean|False|
|m_commandable|System.Boolean|False|
|m_unsummonDistance|System.Single|0|
|m_unsummonOnOwnerLogoutSeconds|System.Single|0|
|m_levelUpFactor|System.Single|1|
|m_saddleItem|ItemDrop|SaddleAsksvin|
|m_dropSaddleOnDeath|System.Boolean|True|
|m_dropSaddleOffset|UnityEngine.Vector3|(0.00, 1.00, 0.00)|
|m_dropItemVel|System.Single|5|
|m_tamingSpeedMultiplierRange|System.Single|60|
|m_tamingBoostMultiplier|System.Single|2|

### Component: Procreation (Asksvin)

|Field|Type|Default Value|
|---|---|---|
|m_updateInterval|System.Single|30|
|m_totalCheckRange|System.Single|10|
|m_maxCreatures|System.Int32|10|
|m_partnerCheckRange|System.Single|4|
|m_pregnancyChance|System.Single|0.33|
|m_pregnancyDuration|System.Single|60|
|m_requiredLovePoints|System.Int32|3|
|m_offspring|UnityEngine.GameObject|AsksvinEgg|
|m_minOffspringLevel|System.Int32|0|
|m_spawnOffset|System.Single|0|

### Component: CharacterAnimEvent (Asksvin)

|Field|Type|Default Value|
|---|---|---|
|m_footIK|System.Boolean|False|
|m_footDownMax|System.Single|0.4|
|m_footOffset|System.Single|0.1|
|m_footStepHeight|System.Single|1|
|m_stabalizeDistance|System.Single|0|
|m_useFeetValues|System.Boolean|False|
|m_headRotation|System.Boolean|True|
|m_lookWeight|System.Single|0.5|
|m_bodyLookWeight|System.Single|0.1|
|m_headLookWeight|System.Single|1|
|m_eyeLookWeight|System.Single|0|
|m_lookClamp|System.Single|0.5|
|m_femaleHack|System.Boolean|False|
|m_femaleOffset|System.Single|0.0004|
|m_maleOffset|System.Single|0.0007651657|

### Component: AnimationEffect (Asksvin)

|Field|Type|Default Value|
|---|---|---|

### Component: LevelEffects (Asksvin)

|Field|Type|Default Value|
|---|---|---|

