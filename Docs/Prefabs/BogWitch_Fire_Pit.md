## BogWitch_Fire_Pit

### Component: Piece (BogWitch_Fire_Pit)

|Field|Type|Default Value|
|-----|----|-------------|
|m_targetNonPlayerBuilt|System.Boolean|False|
|m_name|System.String|$piece_firepit|
|m_description|System.String||
|m_enabled|System.Boolean|True|
|m_isUpgrade|System.Boolean|False|
|m_comfort|System.Int32|1|
|m_comfortObject|UnityEngine.GameObject|_enabled_high|
|m_groundPiece|System.Boolean|False|
|m_allowAltGroundPlacement|System.Boolean|False|
|m_groundOnly|System.Boolean|False|
|m_cultivatedGroundOnly|System.Boolean|False|
|m_waterPiece|System.Boolean|False|
|m_clipGround|System.Boolean|False|
|m_clipEverything|System.Boolean|False|
|m_noInWater|System.Boolean|True|
|m_notOnWood|System.Boolean|True|
|m_notOnTiltingSurface|System.Boolean|True|
|m_inCeilingOnly|System.Boolean|False|
|m_notOnFloor|System.Boolean|False|
|m_noClipping|System.Boolean|True|
|m_onlyInTeleportArea|System.Boolean|False|
|m_allowedInDungeons|System.Boolean|True|
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

### Component: Fireplace (BogWitch_Fire_Pit)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_fire|
|m_startFuel|System.Single|10|
|m_maxFuel|System.Single|10|
|m_secPerFuel|System.Single|5000|
|m_infiniteFuel|System.Boolean|True|
|m_disableCoverCheck|System.Boolean|False|
|m_checkTerrainOffset|System.Single|0.2|
|m_coverCheckOffset|System.Single|0.5|
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
|m_igniteInterval|System.Single|0|
|m_igniteChance|System.Single|0|
|m_igniteSpread|System.Int32|4|
|m_igniteCapsuleRadius|System.Single|0|
|m_igniteCapsuleStart|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_igniteCapsuleEnd|UnityEngine.Vector3|(0.00, 0.00, 0.00)|

### Component: EffectArea (PlayerBase)

|Field|Type|Default Value|
|-----|----|-------------|
|m_statusEffect|System.String||
|m_playerOnly|System.Boolean|False|

