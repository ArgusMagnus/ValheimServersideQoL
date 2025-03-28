## piece_workbench

### Component: Piece (piece_workbench)

|Field|Type|Default Value|
|---|---|---|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_workbench|
|m_description|System.String|$piece_craftingstation|
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

### Component: CraftingStation (piece_workbench)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$piece_workbench|
|m_discoverRange|System.Single|4|
|m_rangeBuild|System.Single|20|
|m_extraRangePerLevel|System.Single|4|
|m_craftRequireRoof|System.Boolean|True|
|m_craftRequireFire|System.Boolean|False|
|m_showBasicRecipies|System.Boolean|True|
|m_useDistance|System.Single|2|
|m_useAnimation|System.Int32|1|
|m_areaMarker|UnityEngine.GameObject|AreaMarker|

### Component: WearNTear (piece_workbench)

|Field|Type|Default Value|
|---|---|---|
|m_new|UnityEngine.GameObject|New|
|m_worn|UnityEngine.GameObject|Worn|
|m_broken|UnityEngine.GameObject|Broken|
|m_noRoofWear|System.Boolean|True|
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

### Component: EffectArea (PlayerBase)

|Field|Type|Default Value|
|---|---|---|
|m_statusEffect|System.String||
|m_playerOnly|System.Boolean|False|

### Component: GuidePoint (GuidePoint)

|Field|Type|Default Value|
|---|---|---|
|m_ravenPrefab|UnityEngine.GameObject|Ravens|

### Component: CircleProjector (AreaMarker)

|Field|Type|Default Value|
|---|---|---|
|m_radius|System.Single|20|
|m_nrOfSegments|System.Int32|80|
|m_speed|System.Single|0.1|
|m_turns|System.Single|1|
|m_start|System.Single|0|
|m_sliceLines|System.Boolean|False|
|m_prefab|UnityEngine.GameObject|Circle_section|

