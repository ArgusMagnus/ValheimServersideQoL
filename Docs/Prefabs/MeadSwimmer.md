## MeadSwimmer

### Component: ZSyncTransform (MeadSwimmer)

|Field|Type|Default Value|
|-----|----|-------------|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: ItemDrop (MeadSwimmer)

|Field|Type|Default Value|
|-----|----|-------------|
|m_autoPickup|System.Boolean|True|
|m_autoDestroy|System.Boolean|True|
|m_pieceDisabledObj|UnityEngine.GameObject|fx_ItemSparkles|

### Component: Piece (MeadSwimmer)

|Field|Type|Default Value|
|-----|----|-------------|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$item_mead_swimmer|
|m_description|System.String|$item_mead_swimmer_description|
|m_enabled|System.Boolean|True|
|m_isUpgrade|System.Boolean|False|
|m_comfort|System.Int32|1|
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
|m_returnResourceHeightOffset|System.Single|0.1|
|m_primaryTarget|System.Boolean|False|
|m_randomTarget|System.Boolean|True|

### Component: WearNTear (MeadSwimmer)

|Field|Type|Default Value|
|-----|----|-------------|
|m_new|UnityEngine.GameObject|default|
|m_worn|UnityEngine.GameObject|default|
|m_broken|UnityEngine.GameObject|default|
|m_wet|UnityEngine.GameObject|default|
|m_noRoofWear|System.Boolean|True|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|False|
|m_supports|System.Boolean|False|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|10|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|False|

### Component: Floating (MeadSwimmer)

|Field|Type|Default Value|
|-----|----|-------------|
|m_waterLevelOffset|System.Single|0.75|
|m_forceDistance|System.Single|1|
|m_force|System.Single|0.5|
|m_balanceForceFraction|System.Single|0.02|
|m_damping|System.Single|0.05|

