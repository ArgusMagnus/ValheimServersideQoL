## piece_bathtub

### Component: Piece (piece_bathtub)

|Field|Type|Default Value|
|-----|----|-------------|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_bathtub|
|m_description|System.String||
|m_enabled|System.Boolean|True|
|m_isUpgrade|System.Boolean|False|
|m_comfort|System.Int32|2|
|m_comfortObject|UnityEngine.GameObject|_Enabled|
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
|m_primaryTarget|System.Boolean|False|
|m_randomTarget|System.Boolean|True|

### Component: WearNTear (piece_bathtub)

|Field|Type|Default Value|
|-----|----|-------------|
|m_new|UnityEngine.GameObject|New|
|m_worn|UnityEngine.GameObject|Worn|
|m_broken|UnityEngine.GameObject|Broken|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|True|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|400|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

### Component: Smelter (piece_bathtub)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_bathtub|
|m_addOreTooltip|System.String|$piece_smelter_additem|
|m_emptyOreTooltip|System.String|$piece_smelter_empty|
|m_enabledObject|UnityEngine.GameObject|_Enabled|
|m_fuelItem|ItemDrop|Wood|
|m_maxOre|System.Int32|0|
|m_maxFuel|System.Int32|10|
|m_fuelPerProduct|System.Int32|1|
|m_secPerProduct|System.Single|5000|
|m_spawnStack|System.Boolean|False|
|m_requiresRoof|System.Boolean|False|
|m_addOreAnimationDuration|System.Single|0|

### Component: WaterVolume (WaterVolume)

|Field|Type|Default Value|
|-----|----|-------------|
|m_forceDepth|System.Single|0.5|
|m_surfaceOffset|System.Single|-0.56|
|m_useGlobalWind|System.Boolean|False|

### Component: Switch (add_fuel)

|Field|Type|Default Value|
|-----|----|-------------|
|m_hoverText|System.String||
|m_name|System.String|$piece_bathtub|
|m_holdRepeatInterval|System.Single|0.2|

### Component: Chair (SitPoint)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_bench01|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: Chair (SitPoint (1))

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_bench01|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: Chair (SitPoint (2))

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_bench01|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: Chair (SitPoint (3))

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_bench01|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: Chair (SitPoint (4))

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_bench01|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: Chair (SitPoint (5))

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_bench01|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: Chair (SitPoint (6))

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_bench01|
|m_useDistance|System.Single|1.5|
|m_detachOffset|UnityEngine.Vector3|(0.00, 0.50, 0.00)|
|m_attachAnimation|System.String|attach_chair|
|m_inShip|System.Boolean|False|

### Component: GuidePoint (GuidePoint)

|Field|Type|Default Value|
|-----|----|-------------|
|m_ravenPrefab|UnityEngine.GameObject|Ravens|

