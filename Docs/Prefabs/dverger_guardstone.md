## dverger_guardstone

### Component: Piece (dverger_guardstone)

|Field|Type|Default Value|
|-----|----|-------------|
|m_targetNonPlayerBuilt|System.Boolean|False|
|m_name|System.String|$piece_guardstone|
|m_description|System.String|$piece_guardstone_description|
|m_enabled|System.Boolean|True|
|m_isUpgrade|System.Boolean|False|
|m_comfort|System.Int32|0|
|m_groundPiece|System.Boolean|False|
|m_allowAltGroundPlacement|System.Boolean|False|
|m_groundOnly|System.Boolean|False|
|m_cultivatedGroundOnly|System.Boolean|False|
|m_waterPiece|System.Boolean|False|
|m_clipGround|System.Boolean|True|
|m_clipEverything|System.Boolean|True|
|m_noInWater|System.Boolean|False|
|m_notOnWood|System.Boolean|False|
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
|m_primaryTarget|System.Boolean|True|
|m_randomTarget|System.Boolean|True|

### Component: WearNTear (dverger_guardstone)

|Field|Type|Default Value|
|-----|----|-------------|
|m_new|UnityEngine.GameObject|new|
|m_worn|UnityEngine.GameObject|new|
|m_broken|UnityEngine.GameObject|new|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|False|
|m_ashDamageImmune|System.Boolean|True|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|False|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|2000|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

### Component: PrivateArea (dverger_guardstone)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_guardstone|
|m_radius|System.Single|32|
|m_updateConnectionsInterval|System.Single|5|
|m_enabledByDefault|System.Boolean|True|
|m_enabledEffect|UnityEngine.GameObject|WayEffect|
|m_connectEffect|UnityEngine.GameObject|vfx_guardstone_connection|
|m_inRangeEffect|UnityEngine.GameObject|InRangeIndicator|

### Component: LightLod (Point light)

|Field|Type|Default Value|
|-----|----|-------------|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|40|
|m_shadowLod|System.Boolean|False|
|m_shadowDistance|System.Single|20|

### Component: CircleProjector (AreaMarker)

|Field|Type|Default Value|
|-----|----|-------------|
|m_radius|System.Single|32|
|m_nrOfSegments|System.Int32|80|
|m_speed|System.Single|0.1|
|m_turns|System.Single|1|
|m_start|System.Single|0|
|m_sliceLines|System.Boolean|False|
|m_prefab|UnityEngine.GameObject|Circle_section|

