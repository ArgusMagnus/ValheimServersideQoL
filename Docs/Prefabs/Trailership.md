## Trailership

### Component: Ship (Trailership)

|Field|Type|Default Value|
|-----|----|-------------|
|m_ashDamageMsgTime|System.Single|10|
|m_sailObject|UnityEngine.GameObject|Sail|
|m_mastObject|UnityEngine.GameObject|Mast|
|m_rudderObject|UnityEngine.GameObject|rudder (1)|
|m_waterLevelOffset|System.Single|1.7|
|m_forceDistance|System.Single|2|
|m_force|System.Single|0.5|
|m_damping|System.Single|0.1|
|m_dampingSideway|System.Single|0.05|
|m_dampingForward|System.Single|0.005|
|m_angularDamping|System.Single|0.1|
|m_disableLevel|System.Single|-0.5|
|m_sailForceOffset|System.Single|0|
|m_sailForceFactor|System.Single|0|
|m_rudderSpeed|System.Single|0.5|
|m_stearForceOffset|System.Single|-7|
|m_stearForce|System.Single|1.5|
|m_stearVelForceFactor|System.Single|0.5|
|m_backwardForce|System.Single|0.5|
|m_rudderRotationMax|System.Single|45|
|m_minWaterImpactForce|System.Single|5|
|m_minWaterImpactInterval|System.Single|2|
|m_waterImpactDamage|System.Single|10|
|m_upsideDownDmgInterval|System.Single|1|
|m_upsideDownDmg|System.Single|20|
|m_ashlandsReady|System.Boolean|False|

### Component: ZSyncTransform (Trailership)

|Field|Type|Default Value|
|-----|----|-------------|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|True|
|m_characterParentSync|System.Boolean|False|

### Component: Piece (Trailership)

|Field|Type|Default Value|
|-----|----|-------------|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|Longship|
|m_description|System.String||
|m_enabled|System.Boolean|True|
|m_isUpgrade|System.Boolean|False|
|m_comfort|System.Int32|0|
|m_groundPiece|System.Boolean|False|
|m_allowAltGroundPlacement|System.Boolean|False|
|m_groundOnly|System.Boolean|False|
|m_cultivatedGroundOnly|System.Boolean|False|
|m_waterPiece|System.Boolean|True|
|m_clipGround|System.Boolean|False|
|m_clipEverything|System.Boolean|False|
|m_noInWater|System.Boolean|False|
|m_notOnWood|System.Boolean|False|
|m_notOnTiltingSurface|System.Boolean|False|
|m_inCeilingOnly|System.Boolean|False|
|m_notOnFloor|System.Boolean|False|
|m_noClipping|System.Boolean|True|
|m_onlyInTeleportArea|System.Boolean|False|
|m_allowedInDungeons|System.Boolean|False|
|m_spaceRequirement|System.Single|0|
|m_repairPiece|System.Boolean|False|
|m_removePiece|System.Boolean|False|
|m_canRotate|System.Boolean|True|
|m_randomInitBuildRotation|System.Boolean|False|
|m_canBeRemoved|System.Boolean|False|
|m_allowRotatedOverlap|System.Boolean|False|
|m_vegetationGroundOnly|System.Boolean|False|
|m_blockRadius|System.Single|0|
|m_connectRadius|System.Single|0|
|m_mustBeAboveConnected|System.Boolean|False|
|m_noVines|System.Boolean|False|
|m_extraPlacementDistance|System.Int32|0|
|m_harvest|System.Boolean|False|
|m_harvestRadius|System.Single|0|
|m_harvestRadiusMaxLevel|System.Single|0|
|m_dlc|System.String||
|m_returnResourceHeightOffset|System.Single|1|
|m_primaryTarget|System.Boolean|True|
|m_randomTarget|System.Boolean|True|

### Component: WearNTear (Trailership)

|Field|Type|Default Value|
|-----|----|-------------|
|m_new|UnityEngine.GameObject|hull_new|
|m_worn|UnityEngine.GameObject|hull_worn|
|m_broken|UnityEngine.GameObject|hull_broken|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|False|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|True|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|False|
|m_health|System.Single|1000|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|False|

### Component: ImpactEffect (Trailership)

|Field|Type|Default Value|
|-----|----|-------------|
|m_hitDestroyChance|System.Single|0|
|m_minVelocity|System.Single|1.5|
|m_maxVelocity|System.Single|7|
|m_damageToSelf|System.Boolean|True|
|m_damagePlayers|System.Boolean|False|
|m_damageFish|System.Boolean|False|
|m_toolTier|System.Int32|0|
|m_interval|System.Single|0.5|

### Component: LineAttach (left_bottom)

|Field|Type|Default Value|
|-----|----|-------------|

### Component: LineAttach (right_bottom)

|Field|Type|Default Value|
|-----|----|-------------|

### Component: GlobalWind (sail_full)

|Field|Type|Default Value|
|-----|----|-------------|
|m_multiplier|System.Single|100|
|m_smoothUpdate|System.Boolean|False|
|m_alignToWindDirection|System.Boolean|False|
|m_particleVelocity|System.Boolean|True|
|m_particleForce|System.Boolean|False|
|m_particleEmission|System.Boolean|False|
|m_particleEmissionMin|System.Int32|0|
|m_particleEmissionMax|System.Int32|1|
|m_clothRandomAccelerationFactor|System.Single|0.5|
|m_checkPlayerShelter|System.Boolean|False|

### Component: LightFlicker (Point light)

|Field|Type|Default Value|
|-----|----|-------------|
|m_flickerIntensity|System.Single|0.2|
|m_flickerSpeed|System.Single|5|
|m_movement|System.Single|0.1|
|m_ttl|System.Single|0|
|m_fadeDuration|System.Single|0.2|
|m_fadeInDuration|System.Single|0|
|m_accessibilityBrightnessMultiplier|System.Single|1|

### Component: LineAttach (left)

|Field|Type|Default Value|
|-----|----|-------------|

### Component: LineAttach (right)

|Field|Type|Default Value|
|-----|----|-------------|

### Component: Ladder (ladder_left)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|Ladder|
|m_useDistance|System.Single|2|

### Component: Ladder (ladder_right)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|Ladder|
|m_useDistance|System.Single|2|

### Component: ShipControlls (rudder_button)

|Field|Type|Default Value|
|-----|----|-------------|
|m_hoverText|System.String|$piece_ship_rudder|
|m_maxUseRange|System.Single|10|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|

### Component: Container (piece_chest)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|Storage|
|m_width|System.Int32|6|
|m_height|System.Int32|3|
|m_checkGuardStone|System.Boolean|False|
|m_autoDestroyEmpty|System.Boolean|False|
|m_open|UnityEngine.GameObject|open|
|m_closed|UnityEngine.GameObject|closed|
|m_destroyedLootPrefab|UnityEngine.GameObject|CargoCrate|

### Component: ShipEffects (watereffects)

|Field|Type|Default Value|
|-----|----|-------------|
|m_offset|System.Single|0|
|m_minimumWakeVel|System.Single|3|
|m_speedWakeRoot|UnityEngine.GameObject|SpeedWake|
|m_wakeSoundRoot|UnityEngine.GameObject|WakeSounds|
|m_inWaterSoundRoot|UnityEngine.GameObject|InWaterSounds|
|m_audioFadeDuration|System.Single|2|
|m_sailFadeDuration|System.Single|1|
|m_splashEffects|UnityEngine.GameObject|splash_effects|

### Component: WaterTrigger (WaterTrigger)

|Field|Type|Default Value|
|-----|----|-------------|
|m_cooldownDelay|System.Single|2|

### Component: WaterTrigger (WaterTrigger (1))

|Field|Type|Default Value|
|-----|----|-------------|
|m_cooldownDelay|System.Single|2|

### Component: WaterTrigger (WaterTrigger (2))

|Field|Type|Default Value|
|-----|----|-------------|
|m_cooldownDelay|System.Single|2|

### Component: WaterTrigger (WaterTrigger (3))

|Field|Type|Default Value|
|-----|----|-------------|
|m_cooldownDelay|System.Single|2|

### Component: WaterTrigger (WaterTrigger (4))

|Field|Type|Default Value|
|-----|----|-------------|
|m_cooldownDelay|System.Single|2|

### Component: WaterTrigger (WaterTrigger (5))

|Field|Type|Default Value|
|-----|----|-------------|
|m_cooldownDelay|System.Single|2|

### Component: WaterTrigger (WaterTrigger (6))

|Field|Type|Default Value|
|-----|----|-------------|
|m_cooldownDelay|System.Single|2|

### Component: WaterTrigger (WaterTrigger (7))

|Field|Type|Default Value|
|-----|----|-------------|
|m_cooldownDelay|System.Single|2|

### Component: Chair (sit_box)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_stool|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: Chair (sit_box (1))

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_stool|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: Chair (sit_box (2))

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_stool|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: Chair (sit_box (3))

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_stool|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: Chair (sit_box (4))

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_stool|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: Chair (mast)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$ship_holdfast|
|m_useDistance|System.Single|2|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_mast|
|m_inShip|System.Boolean|False|

### Component: Chair (front)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$ship_holdfast|
|m_useDistance|System.Single|2|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_dragon|
|m_inShip|System.Boolean|False|

