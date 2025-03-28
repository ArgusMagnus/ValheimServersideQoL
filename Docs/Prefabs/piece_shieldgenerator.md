## piece_shieldgenerator

### Component: ShieldGenerator (piece_shieldgenerator)

|Field|Type|Default Value|
|-----|----|-------------|
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
|m_startStopSpeed|System.Single|25|
|m_offWhenNoFuel|System.Boolean|True|
|m_enableAttack|System.Boolean|False|
|m_attackChargeTime|System.Single|900|
|m_damagePlayers|System.Boolean|True|
|m_attackObject|UnityEngine.GameObject|shieldgenerator_attack|
|m_shieldLowLoopFuelStart|System.Single|0.2|

### Component: Piece (piece_shieldgenerator)

|Field|Type|Default Value|
|-----|----|-------------|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_shieldgenerator|
|m_description|System.String|$piece_shieldgenerator_description|
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
|m_blockRadius|System.Single|15|
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

### Component: WearNTear (piece_shieldgenerator)

|Field|Type|Default Value|
|-----|----|-------------|
|m_new|UnityEngine.GameObject|New|
|m_worn|UnityEngine.GameObject|New|
|m_broken|UnityEngine.GameObject|New|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|True|
|m_burnable|System.Boolean|False|
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
|-----|----|-------------|
|m_statusEffect|System.String||
|m_playerOnly|System.Boolean|False|

### Component: GuidePoint (GuidePoint)

|Field|Type|Default Value|
|-----|----|-------------|
|m_ravenPrefab|UnityEngine.GameObject|Ravens|

### Component: EffectArea (PlayerBase)

|Field|Type|Default Value|
|-----|----|-------------|
|m_statusEffect|System.String||
|m_playerOnly|System.Boolean|False|

### Component: Switch (add_ore)

|Field|Type|Default Value|
|-----|----|-------------|
|m_hoverText|System.String||
|m_name|System.String||
|m_holdRepeatInterval|System.Single|0.2|

### Component: LightLod (Point light)

|Field|Type|Default Value|
|-----|----|-------------|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|40|
|m_shadowLod|System.Boolean|True|
|m_shadowDistance|System.Single|20|

### Component: LightLod (Point light (1))

|Field|Type|Default Value|
|-----|----|-------------|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|40|
|m_shadowLod|System.Boolean|True|
|m_shadowDistance|System.Single|20|

### Component: ZSFX (sfx_shieldgenerator_powered_loop)

|Field|Type|Default Value|
|-----|----|-------------|
|m_playOnAwake|System.Boolean|True|
|m_closedCaptionToken|System.String||
|m_secondaryCaptionToken|System.String||
|m_minimumCaptionVolume|System.Single|0.3|
|m_maxConcurrentSources|System.Int32|1|
|m_ignoreConcurrencyDistance|System.Boolean|False|
|m_maxPitch|System.Single|1.1|
|m_minPitch|System.Single|0.9|
|m_maxVol|System.Single|1.2|
|m_minVol|System.Single|1.2|
|m_fadeInDuration|System.Single|1|
|m_fadeOutDuration|System.Single|1|
|m_fadeOutDelay|System.Single|0|
|m_fadeOutOnAwake|System.Boolean|False|
|m_randomPan|System.Boolean|False|
|m_minPan|System.Single|-1|
|m_maxPan|System.Single|1|
|m_maxDelay|System.Single|0|
|m_minDelay|System.Single|0|
|m_distanceReverb|System.Boolean|True|
|m_useCustomReverbDistance|System.Boolean|False|
|m_customReverbDistance|System.Single|4|
|m_hash|System.Int32|-2048667594|

### Component: TimedDestruction (sfx_shieldgenerator_powered_loop)

|Field|Type|Default Value|
|-----|----|-------------|
|m_timeout|System.Single|1.5|
|m_triggerOnAwake|System.Boolean|False|
|m_forceTakeOwnershipAndDestroy|System.Boolean|False|

