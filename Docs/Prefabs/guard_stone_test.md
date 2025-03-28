## guard_stone_test

### Component: Piece (guard_stone_test)

|Field|Type|Default Value|
|---|---|---|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|Guard stone|
|m_description|System.String||
|m_enabled|System.Boolean|True|
|m_isUpgrade|System.Boolean|False|
|m_comfort|System.Int32|0|
|m_groundPiece|System.Boolean|False|
|m_allowAltGroundPlacement|System.Boolean|False|
|m_groundOnly|System.Boolean|True|
|m_cultivatedGroundOnly|System.Boolean|False|
|m_waterPiece|System.Boolean|False|
|m_clipGround|System.Boolean|True|
|m_clipEverything|System.Boolean|False|
|m_noInWater|System.Boolean|False|
|m_notOnWood|System.Boolean|False|
|m_notOnTiltingSurface|System.Boolean|False|
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

### Component: WearNTear (guard_stone_test)

|Field|Type|Default Value|
|---|---|---|
|m_new|UnityEngine.GameObject|new|
|m_worn|UnityEngine.GameObject|new|
|m_broken|UnityEngine.GameObject|new|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|False|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|True|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|5000|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

### Component: PrivateArea (guard_stone_test)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|Guard stone|
|m_radius|System.Single|32|
|m_updateConnectionsInterval|System.Single|5|
|m_enabledByDefault|System.Boolean|False|
|m_enabledEffect|UnityEngine.GameObject|WayEffect|

### Component: CircleProjector (AreaMarker)

|Field|Type|Default Value|
|---|---|---|
|m_radius|System.Single|32|
|m_nrOfSegments|System.Int32|80|
|m_speed|System.Single|0.1|
|m_turns|System.Single|1|
|m_start|System.Single|0|
|m_sliceLines|System.Boolean|False|
|m_prefab|UnityEngine.GameObject|Circle_section|

