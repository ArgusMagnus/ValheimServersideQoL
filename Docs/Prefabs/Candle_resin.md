## Candle_resin

### Component: Piece (Candle_resin)

|Field|Type|Default Value|
|---|---|---|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_candle|
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

### Component: WearNTear (Candle_resin)

|Field|Type|Default Value|
|---|---|---|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|False|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|5|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|False|

### Component: Fireplace (Candle_resin)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$piece_candle|
|m_startFuel|System.Single|3|
|m_maxFuel|System.Single|3|
|m_secPerFuel|System.Single|5000|
|m_infiniteFuel|System.Boolean|False|
|m_disableCoverCheck|System.Boolean|True|
|m_checkTerrainOffset|System.Single|0.57|
|m_coverCheckOffset|System.Single|0.15|
|m_holdRepeatInterval|System.Single|0.2|
|m_halfThreshold|System.Single|0.5|
|m_canTurnOff|System.Boolean|True|
|m_canRefill|System.Boolean|False|
|m_lowWetOverHalf|System.Boolean|False|
|m_enabledObjectLow|UnityEngine.GameObject|low|
|m_enabledObjectHigh|UnityEngine.GameObject|high|
|m_fullObject|UnityEngine.GameObject|full|
|m_halfObject|UnityEngine.GameObject|half|
|m_emptyObject|UnityEngine.GameObject|off|
|m_playerBaseObject|UnityEngine.GameObject|PlayerBase|
|m_fuelItem|ItemDrop|Resin|
|m_fireworksMaxRandomAngle|System.Single|5|
|m_igniteInterval|System.Single|25|
|m_igniteChance|System.Single|0.0025|
|m_igniteSpread|System.Int32|1|
|m_igniteCapsuleRadius|System.Single|0.1|
|m_igniteCapsuleStart|UnityEngine.Vector3|(0.00, 0.65, 0.00)|
|m_igniteCapsuleEnd|UnityEngine.Vector3|(0.00, 1.00, 0.00)|
|m_firePrefab|UnityEngine.GameObject|Fire|

### Component: LightLod (Point Light)

|Field|Type|Default Value|
|---|---|---|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|15|
|m_shadowLod|System.Boolean|True|
|m_shadowDistance|System.Single|20|

