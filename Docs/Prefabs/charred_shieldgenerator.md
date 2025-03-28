## charred_shieldgenerator

### Component: ShieldGenerator (charred_shieldgenerator)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$piece_shieldgenerator|
|m_add|System.String|$piece_shieldgenerator_add|
|m_enabledObject|UnityEngine.GameObject|enabled|
|m_disabledObject|UnityEngine.GameObject|disabled|
|m_maxFuel|System.Int32|10|
|m_defaultFuel|System.Int32|0|
|m_fuelPerDamage|System.Single|0.002|
|m_shieldDome|UnityEngine.GameObject|ForceField|
|m_minShieldRadius|System.Single|30|
|m_maxShieldRadius|System.Single|30|
|m_decreaseInertia|System.Single|0.05|
|m_startStopSpeed|System.Single|0.5|
|m_offWhenNoFuel|System.Boolean|True|
|m_enableAttack|System.Boolean|False|
|m_attackChargeTime|System.Single|900|
|m_damagePlayers|System.Boolean|True|
|m_attackObject|UnityEngine.GameObject|shieldgenerator_attack|
|m_shieldLowLoopFuelStart|System.Single|0|

### Component: Piece (charred_shieldgenerator)

|Field|Type|Default Value|
|---|---|---|
|m_targetNonPlayerBuilt|System.Boolean|False|
|m_name|System.String|$piece_shieldgenerator|
|m_description|System.String||
|m_enabled|System.Boolean|True|
|m_isUpgrade|System.Boolean|False|
|m_comfort|System.Int32|0|
|m_groundPiece|System.Boolean|False|
|m_allowAltGroundPlacement|System.Boolean|False|
|m_groundOnly|System.Boolean|False|
|m_cultivatedGroundOnly|System.Boolean|False|
|m_waterPiece|System.Boolean|False|
|m_clipGround|System.Boolean|False|
|m_clipEverything|System.Boolean|False|
|m_noInWater|System.Boolean|True|
|m_notOnWood|System.Boolean|False|
|m_notOnTiltingSurface|System.Boolean|True|
|m_inCeilingOnly|System.Boolean|False|
|m_notOnFloor|System.Boolean|False|
|m_noClipping|System.Boolean|False|
|m_onlyInTeleportArea|System.Boolean|False|
|m_allowedInDungeons|System.Boolean|False|
|m_spaceRequirement|System.Single|0|
|m_repairPiece|System.Boolean|False|
|m_removePiece|System.Boolean|False|
|m_canRotate|System.Boolean|True|
|m_randomInitBuildRotation|System.Boolean|False|
|m_canBeRemoved|System.Boolean|True|
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
|m_primaryTarget|System.Boolean|False|
|m_randomTarget|System.Boolean|False|

### Component: WearNTear (charred_shieldgenerator)

|Field|Type|Default Value|
|---|---|---|
|m_new|UnityEngine.GameObject|New|
|m_worn|UnityEngine.GameObject|New|
|m_broken|UnityEngine.GameObject|New|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|True|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|True|
|m_comOffset|UnityEngine.Vector3|(0.00, 1.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|1500|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

### Component: EffectArea (PlayerBase)

|Field|Type|Default Value|
|---|---|---|
|m_statusEffect|System.String||
|m_playerOnly|System.Boolean|False|

### Component: Switch (add_ore)

|Field|Type|Default Value|
|---|---|---|
|m_hoverText|System.String||
|m_name|System.String||
|m_holdRepeatInterval|System.Single|0.2|

### Component: SmokeSpawner (SmokeSpawner)

|Field|Type|Default Value|
|---|---|---|
|m_smokePrefab|UnityEngine.GameObject|SmokeBall|
|m_interval|System.Single|0.5|
|m_testRadius|System.Single|0.75|
|m_spawnRadius|System.Single|0|
|m_stopFireOnStart|System.Boolean|False|

### Component: LightFlicker (Point light)

|Field|Type|Default Value|
|---|---|---|
|m_flickerIntensity|System.Single|0.1|
|m_flickerSpeed|System.Single|10|
|m_movement|System.Single|0.05|
|m_ttl|System.Single|0|
|m_fadeDuration|System.Single|0.2|
|m_fadeInDuration|System.Single|0|
|m_accessibilityBrightnessMultiplier|System.Single|1|

### Component: LightLod (Point light)

|Field|Type|Default Value|
|---|---|---|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|40|
|m_shadowLod|System.Boolean|True|
|m_shadowDistance|System.Single|20|

### Component: EffectArea (FireWarmth)

|Field|Type|Default Value|
|---|---|---|
|m_statusEffect|System.String||
|m_playerOnly|System.Boolean|False|

