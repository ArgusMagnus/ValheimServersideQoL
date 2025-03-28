## sapling_seedcarrot

### Component: Plant (sapling_seedcarrot)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_sapling_seedcarrot|
|m_growTime|System.Single|4000|
|m_growTimeMax|System.Single|5000|
|m_minScale|System.Single|0.9|
|m_maxScale|System.Single|1.1|
|m_growRadius|System.Single|0.5|
|m_growRadiusVines|System.Single|0|
|m_needCultivatedGround|System.Boolean|True|
|m_destroyIfCantGrow|System.Boolean|True|
|m_tolerateHeat|System.Boolean|False|
|m_tolerateCold|System.Boolean|False|
|m_attachDistance|System.Single|0|

### Component: Destructible (sapling_seedcarrot)

|Field|Type|Default Value|
|-----|----|-------------|
|m_health|System.Single|1|
|m_minDamageTreshold|System.Single|0|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|False|
|m_ttl|System.Single|0|
|m_autoCreateFragments|System.Boolean|True|

### Component: Piece (sapling_seedcarrot)

|Field|Type|Default Value|
|-----|----|-------------|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_sapling_seedcarrot|
|m_description|System.String|$piece_sapling_seedcarrot_description|
|m_enabled|System.Boolean|True|
|m_isUpgrade|System.Boolean|False|
|m_comfort|System.Int32|0|
|m_groundPiece|System.Boolean|False|
|m_allowAltGroundPlacement|System.Boolean|False|
|m_groundOnly|System.Boolean|False|
|m_cultivatedGroundOnly|System.Boolean|True|
|m_waterPiece|System.Boolean|False|
|m_clipGround|System.Boolean|True|
|m_clipEverything|System.Boolean|False|
|m_noInWater|System.Boolean|True|
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
|m_randomInitBuildRotation|System.Boolean|True|
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

