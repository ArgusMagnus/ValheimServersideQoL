## bonfire

### Component: Piece (bonfire)

|Field|Type|Default Value|
|---|---|---|
|m_targetNonPlayerBuilt|System.Boolean|False|
|m_name|System.String|$piece_bonfire|
|m_description|System.String||
|m_enabled|System.Boolean|True|
|m_isUpgrade|System.Boolean|False|
|m_comfort|System.Int32|1|
|m_groundPiece|System.Boolean|False|
|m_allowAltGroundPlacement|System.Boolean|False|
|m_groundOnly|System.Boolean|False|
|m_cultivatedGroundOnly|System.Boolean|False|
|m_waterPiece|System.Boolean|False|
|m_clipGround|System.Boolean|True|
|m_clipEverything|System.Boolean|False|
|m_noInWater|System.Boolean|True|
|m_notOnWood|System.Boolean|True|
|m_notOnTiltingSurface|System.Boolean|True|
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
|m_randomTarget|System.Boolean|True|

### Component: Fireplace (bonfire)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$piece_fire|
|m_startFuel|System.Single|0|
|m_maxFuel|System.Single|10|
|m_secPerFuel|System.Single|5000|
|m_infiniteFuel|System.Boolean|False|
|m_disableCoverCheck|System.Boolean|False|
|m_checkTerrainOffset|System.Single|0.5|
|m_coverCheckOffset|System.Single|1|
|m_holdRepeatInterval|System.Single|0.2|
|m_halfThreshold|System.Single|0.5|
|m_canTurnOff|System.Boolean|False|
|m_canRefill|System.Boolean|True|
|m_lowWetOverHalf|System.Boolean|True|
|m_enabledObject|UnityEngine.GameObject|_enabled|
|m_enabledObjectLow|UnityEngine.GameObject|_enabled_low|
|m_enabledObjectHigh|UnityEngine.GameObject|_enabled_high|
|m_playerBaseObject|UnityEngine.GameObject|PlayerBase|
|m_fuelItem|ItemDrop|Wood|
|m_fireworksMaxRandomAngle|System.Single|15|
|m_igniteInterval|System.Single|5|
|m_igniteChance|System.Single|0.75|
|m_igniteSpread|System.Int32|4|
|m_igniteCapsuleRadius|System.Single|1.69|
|m_igniteCapsuleStart|UnityEngine.Vector3|(0.00, 1.74, 0.00)|
|m_igniteCapsuleEnd|UnityEngine.Vector3|(0.00, 4.49, 0.00)|
|m_firePrefab|UnityEngine.GameObject|Fire|

### Component: CinderSpawner (bonfire)

|Field|Type|Default Value|
|---|---|---|
|m_cinderPrefab|UnityEngine.GameObject|Cinder|
|m_cinderInterval|System.Single|5|
|m_cinderChance|System.Single|0.5|
|m_cinderVel|System.Single|4|
|m_spawnOffset|System.Single|1|
|m_spawnOffsetPoint|UnityEngine.Vector3|(0.00, 1.50, 0.00)|
|m_spread|System.Int32|4|
|m_instancesPerSpawn|System.Int32|1|
|m_spawnOnAwake|System.Boolean|False|
|m_spawnOnProjectileHit|System.Boolean|False|

### Component: WearNTear (bonfire)

|Field|Type|Default Value|
|---|---|---|
|m_new|UnityEngine.GameObject|New|
|m_worn|UnityEngine.GameObject|New|
|m_broken|UnityEngine.GameObject|New|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|False|
|m_supports|System.Boolean|False|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|300|
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

