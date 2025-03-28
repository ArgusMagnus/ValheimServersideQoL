## Goblin_Gem

### Component: Character (Goblin_Gem)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$enemy_gemgoblin|
|m_group|System.String||
|m_boss|System.Boolean|False|
|m_dontHideBossHud|System.Boolean|False|
|m_bossEvent|System.String||
|m_defeatSetGlobalKey|System.String||
|m_aiSkipTarget|System.Boolean|False|
|m_crouchSpeed|System.Single|2|
|m_walkSpeed|System.Single|5|
|m_speed|System.Single|1.5|
|m_turnSpeed|System.Single|80|
|m_runSpeed|System.Single|12|
|m_runTurnSpeed|System.Single|200|
|m_flySlowSpeed|System.Single|5|
|m_flyFastSpeed|System.Single|12|
|m_flyTurnSpeed|System.Single|12|
|m_acceleration|System.Single|0.5|
|m_jumpForce|System.Single|10|
|m_jumpForceForward|System.Single|0|
|m_jumpForceTiredFactor|System.Single|0.7|
|m_airControl|System.Single|0.1|
|m_canSwim|System.Boolean|True|
|m_swimDepth|System.Single|1.2|
|m_swimSpeed|System.Single|2|
|m_swimTurnSpeed|System.Single|100|
|m_swimAcceleration|System.Single|0.05|
|m_groundTiltSpeed|System.Single|50|
|m_flying|System.Boolean|False|
|m_jumpStaminaUsage|System.Single|10|
|m_disableWhileSleeping|System.Boolean|False|
|m_tolerateWater|System.Boolean|True|
|m_tolerateFire|System.Boolean|False|
|m_tolerateSmoke|System.Boolean|False|
|m_tolerateTar|System.Boolean|False|
|m_health|System.Single|1000|
|m_staggerWhenBlocked|System.Boolean|True|
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

### Component: AnimalAI (Goblin_Gem)

|Field|Type|Default Value|
|---|---|---|
|m_timeToSafe|System.Single|15|
|m_viewRange|System.Single|25|
|m_viewAngle|System.Single|45|
|m_hearRange|System.Single|15|
|m_mistVision|System.Boolean|False|
|m_idleSoundInterval|System.Single|5|
|m_idleSoundChance|System.Single|0.9|
|m_moveMinAngle|System.Single|90|
|m_smoothMovement|System.Boolean|True|
|m_serpentMovement|System.Boolean|False|
|m_serpentTurnRadius|System.Single|20|
|m_jumpInterval|System.Single|0|
|m_randomCircleInterval|System.Single|2|
|m_randomMoveInterval|System.Single|60|
|m_randomMoveRange|System.Single|10|
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
|m_avoidFire|System.Boolean|True|
|m_afraidOfFire|System.Boolean|True|
|m_avoidWater|System.Boolean|True|
|m_avoidLava|System.Boolean|True|
|m_skipLavaTargets|System.Boolean|False|
|m_avoidLavaFlee|System.Boolean|True|
|m_aggravatable|System.Boolean|False|
|m_passiveAggresive|System.Boolean|True|
|m_spawnMessage|System.String||
|m_deathMessage|System.String||
|m_alertedMessage|System.String||
|m_fleeRange|System.Single|30|
|m_fleeAngle|System.Single|45|
|m_fleeInterval|System.Single|2|

### Component: ZSyncTransform (Goblin_Gem)

|Field|Type|Default Value|
|---|---|---|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: ZSyncAnimation (Goblin_Gem)

|Field|Type|Default Value|
|---|---|---|
|m_smoothCharacterSpeeds|System.Boolean|True|

### Component: CharacterDrop (Goblin_Gem)

|Field|Type|Default Value|
|---|---|---|
|m_spawnOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|

### Component: RandomAnimation (Goblin_Gem)

|Field|Type|Default Value|
|---|---|---|

### Component: VisEquipment (Goblin_Gem)

|Field|Type|Default Value|
|---|---|---|
|m_isPlayer|System.Boolean|False|
|m_useAllTrails|System.Boolean|False|

### Component: FootStep (Goblin_Gem)

|Field|Type|Default Value|
|---|---|---|
|m_footlessFootsteps|System.Boolean|False|
|m_footlessTriggerDistance|System.Single|1|
|m_footstepCullDistance|System.Single|20|

### Component: CharacterAnimEvent (Visual)

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

### Component: LevelEffects (Visual)

|Field|Type|Default Value|
|---|---|---|

