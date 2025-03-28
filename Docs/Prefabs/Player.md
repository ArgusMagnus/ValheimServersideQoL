## Player

### Component: PlayerController (Player)

|Field|Type|Default Value|
|---|---|---|
|m_minDodgeTime|System.Single|0.2|

### Component: Player (Player)

|Field|Type|Default Value|
|---|---|---|
|m_maxPlaceDistance|System.Single|8|
|m_maxInteractDistance|System.Single|3.5|
|m_scrollSens|System.Single|4|
|m_staminaRegen|System.Single|6|
|m_staminaRegenTimeMultiplier|System.Single|1|
|m_staminaRegenDelay|System.Single|1|
|m_runStaminaDrain|System.Single|8|
|m_sneakStaminaDrain|System.Single|5|
|m_swimStaminaDrainMinSkill|System.Single|6|
|m_swimStaminaDrainMaxSkill|System.Single|3|
|m_dodgeStaminaUsage|System.Single|15|
|m_weightStaminaFactor|System.Single|0.1|
|m_eiterRegen|System.Single|2|
|m_eitrRegenDelay|System.Single|1|
|m_autoPickupRange|System.Single|2|
|m_maxCarryWeight|System.Single|300|
|m_encumberedStaminaDrain|System.Single|5|
|m_hardDeathCooldown|System.Single|600|
|m_baseCameraShake|System.Single|3|
|m_placeDelay|System.Single|0.4|
|m_removeDelay|System.Single|0.25|
|m_placeMarker|UnityEngine.GameObject|PlaceMarker|
|m_tombstone|UnityEngine.GameObject|Player_tombstone|
|m_baseHP|System.Single|25|
|m_baseStamina|System.Single|50|
|m_guardianPowerCooldown|System.Single|0|
|m_scrollAmountThreshold|System.Single|0.1|
|m_autoRun|System.Boolean|False|
|m_equipStaminaDrain|System.Single|6|
|m_blockStaminaDrain|System.Single|10|
|m_unarmedWeapon|ItemDrop|PlayerUnarmed|
|m_name|System.String|Human|
|m_group|System.String||
|m_boss|System.Boolean|False|
|m_dontHideBossHud|System.Boolean|False|
|m_bossEvent|System.String||
|m_defeatSetGlobalKey|System.String||
|m_aiSkipTarget|System.Boolean|False|
|m_crouchSpeed|System.Single|2|
|m_walkSpeed|System.Single|1.6|
|m_speed|System.Single|4|
|m_turnSpeed|System.Single|300|
|m_runSpeed|System.Single|7|
|m_runTurnSpeed|System.Single|300|
|m_flySlowSpeed|System.Single|5|
|m_flyFastSpeed|System.Single|12|
|m_flyTurnSpeed|System.Single|12|
|m_acceleration|System.Single|0.8|
|m_jumpForce|System.Single|8|
|m_jumpForceForward|System.Single|2|
|m_jumpForceTiredFactor|System.Single|0.6|
|m_airControl|System.Single|0.1|
|m_canSwim|System.Boolean|True|
|m_swimDepth|System.Single|1.5|
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
|m_health|System.Single|100|
|m_staggerWhenBlocked|System.Boolean|True|
|m_staggerDamageFactor|System.Single|0.4|
|m_heatBuildupBase|System.Single|2.5|
|m_heatCooldownBase|System.Single|1|
|m_heatBuildupWater|System.Single|0.025|
|m_heatWaterTouchMultiplier|System.Single|0.1|
|m_lavaDamageTickInterval|System.Single|0.2|
|m_heatLevelFirstDamageThreshold|System.Single|0.7|
|m_lavaFirstDamage|System.Single|10|
|m_lavaFullDamage|System.Single|60|
|m_lavaAirDamageHeight|System.Single|2.2|
|m_dayHeatGainRunning|System.Single|0|
|m_dayHeatGainStill|System.Single|-0.1|
|m_dayHeatEquipmentStop|System.Single|0.5|
|m_lavaSlowMax|System.Single|0.8|
|m_lavaSlowHeight|System.Single|0.8|

### Component: ZSyncTransform (Player)

|Field|Type|Default Value|
|---|---|---|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|True|
|m_characterParentSync|System.Boolean|True|

### Component: ZSyncAnimation (Player)

|Field|Type|Default Value|
|---|---|---|
|m_smoothCharacterSpeeds|System.Boolean|False|

### Component: Talker (Player)

|Field|Type|Default Value|
|---|---|---|
|m_visperDistance|System.Single|5|
|m_normalDistance|System.Single|25|
|m_shoutDistance|System.Single|200|

### Component: VisEquipment (Player)

|Field|Type|Default Value|
|---|---|---|
|m_isPlayer|System.Boolean|True|
|m_useAllTrails|System.Boolean|False|

### Component: Skills (Player)

|Field|Type|Default Value|
|---|---|---|
|m_DeathLowerFactor|System.Single|0.05|
|m_useSkillCap|System.Boolean|False|
|m_totalSkillCap|System.Single|500|

### Component: FootStep (Player)

|Field|Type|Default Value|
|---|---|---|
|m_footlessFootsteps|System.Boolean|False|
|m_footlessTriggerDistance|System.Single|1|
|m_footstepCullDistance|System.Single|20|

### Component: CharacterAnimEvent (Visual)

|Field|Type|Default Value|
|---|---|---|
|m_footIK|System.Boolean|True|
|m_footDownMax|System.Single|0.2|
|m_footOffset|System.Single|0.12|
|m_footStepHeight|System.Single|0.4|
|m_stabalizeDistance|System.Single|0|
|m_useFeetValues|System.Boolean|False|
|m_headRotation|System.Boolean|True|
|m_lookWeight|System.Single|0.5|
|m_bodyLookWeight|System.Single|0.1|
|m_headLookWeight|System.Single|1|
|m_eyeLookWeight|System.Single|0|
|m_lookClamp|System.Single|0.5|
|m_femaleHack|System.Boolean|True|
|m_femaleOffset|System.Single|0.0004|
|m_maleOffset|System.Single|0.0007651657|

### Component: AnimationEffect (Visual)

|Field|Type|Default Value|
|---|---|---|

