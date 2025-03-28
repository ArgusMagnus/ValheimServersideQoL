## piece_groundtorch

### Component: Piece (piece_groundtorch)

|Field|Type|Default Value|
|-----|----|-------------|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_groundtorch|
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
|m_noInWater|System.Boolean|False|
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
|m_primaryTarget|System.Boolean|True|
|m_randomTarget|System.Boolean|True|

### Component: WearNTear (piece_groundtorch)

|Field|Type|Default Value|
|-----|----|-------------|
|m_new|UnityEngine.GameObject|New|
|m_worn|UnityEngine.GameObject|New|
|m_broken|UnityEngine.GameObject|New|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|False|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|200|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

### Component: Fireplace (piece_groundtorch)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_groundtorch|
|m_startFuel|System.Single|2|
|m_maxFuel|System.Single|6|
|m_secPerFuel|System.Single|20000|
|m_infiniteFuel|System.Boolean|False|
|m_disableCoverCheck|System.Boolean|False|
|m_checkTerrainOffset|System.Single|0.57|
|m_coverCheckOffset|System.Single|0.76|
|m_holdRepeatInterval|System.Single|0.2|
|m_halfThreshold|System.Single|0.5|
|m_canTurnOff|System.Boolean|False|
|m_canRefill|System.Boolean|True|
|m_lowWetOverHalf|System.Boolean|True|
|m_enabledObject|UnityEngine.GameObject|_enabled|
|m_playerBaseObject|UnityEngine.GameObject|PlayerBase|
|m_fuelItem|ItemDrop|Resin|
|m_fireworksMaxRandomAngle|System.Single|5|
|m_igniteInterval|System.Single|10|
|m_igniteChance|System.Single|1|
|m_igniteSpread|System.Int32|1|
|m_igniteCapsuleRadius|System.Single|0.1|
|m_igniteCapsuleStart|UnityEngine.Vector3|(0.00, 0.65, 0.00)|
|m_igniteCapsuleEnd|UnityEngine.Vector3|(0.00, 1.00, 0.00)|
|m_firePrefab|UnityEngine.GameObject|Fire|

### Component: EffectArea (FireArea)

|Field|Type|Default Value|
|-----|----|-------------|
|m_statusEffect|System.String||
|m_playerOnly|System.Boolean|False|

### Component: LightFlicker (Point light)

|Field|Type|Default Value|
|-----|----|-------------|
|m_flickerIntensity|System.Single|0.1|
|m_flickerSpeed|System.Single|10|
|m_movement|System.Single|0.1|
|m_ttl|System.Single|0|
|m_fadeDuration|System.Single|0.2|
|m_fadeInDuration|System.Single|0|
|m_accessibilityBrightnessMultiplier|System.Single|1|

### Component: LightLod (Point light)

|Field|Type|Default Value|
|-----|----|-------------|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|80|
|m_shadowLod|System.Boolean|False|
|m_shadowDistance|System.Single|20|

### Component: ZSFX (sfx_fire_loop)

|Field|Type|Default Value|
|-----|----|-------------|
|m_playOnAwake|System.Boolean|True|
|m_closedCaptionToken|System.String||
|m_secondaryCaptionToken|System.String||
|m_minimumCaptionVolume|System.Single|0.3|
|m_maxConcurrentSources|System.Int32|2|
|m_ignoreConcurrencyDistance|System.Boolean|False|
|m_maxPitch|System.Single|1.1|
|m_minPitch|System.Single|0.9|
|m_maxVol|System.Single|1|
|m_minVol|System.Single|0.6|
|m_fadeInDuration|System.Single|0.4|
|m_fadeOutDuration|System.Single|0.4|
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
|m_hash|System.Int32|-851502317|

### Component: TimedDestruction (sfx_fire_loop)

|Field|Type|Default Value|
|-----|----|-------------|
|m_timeout|System.Single|5|
|m_triggerOnAwake|System.Boolean|False|
|m_forceTakeOwnershipAndDestroy|System.Boolean|False|

