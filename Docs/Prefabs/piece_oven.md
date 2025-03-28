## piece_oven

### Component: Piece (piece_oven)

|Field|Type|Default Value|
|-----|----|-------------|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_oven|
|m_description|System.String||
|m_enabled|System.Boolean|True|
|m_isUpgrade|System.Boolean|False|
|m_comfort|System.Int32|0|
|m_groundPiece|System.Boolean|False|
|m_allowAltGroundPlacement|System.Boolean|False|
|m_groundOnly|System.Boolean|False|
|m_cultivatedGroundOnly|System.Boolean|False|
|m_waterPiece|System.Boolean|False|
|m_clipGround|System.Boolean|True|
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

### Component: WearNTear (piece_oven)

|Field|Type|Default Value|
|-----|----|-------------|
|m_new|UnityEngine.GameObject|New|
|m_worn|UnityEngine.GameObject|Worn|
|m_broken|UnityEngine.GameObject|Broken|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|False|
|m_supports|System.Boolean|True|
|m_comOffset|UnityEngine.Vector3|(0.00, 1.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|500|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

### Component: CookingStation (piece_oven)

|Field|Type|Default Value|
|-----|----|-------------|
|m_addItemTooltip|System.String|$piece_oven_additem|
|m_spawnForce|System.Single|5|
|m_overCookedItem|ItemDrop|Coal|
|m_name|System.String|$piece_oven|
|m_requireFire|System.Boolean|False|
|m_fireCheckRadius|System.Single|0.25|
|m_useFuel|System.Boolean|True|
|m_fuelItem|ItemDrop|Wood|
|m_maxFuel|System.Int32|10|
|m_secPerFuel|System.Int32|2000|
|m_haveFuelObject|UnityEngine.GameObject|HaveFuel|
|m_haveFireObject|UnityEngine.GameObject|Working|

### Component: Switch (add_food)

|Field|Type|Default Value|
|-----|----|-------------|
|m_hoverText|System.String||
|m_name|System.String|$piece_oven|
|m_holdRepeatInterval|System.Single|0.2|

### Component: Switch (add_fuel)

|Field|Type|Default Value|
|-----|----|-------------|
|m_hoverText|System.String||
|m_name|System.String|$piece_oven|
|m_holdRepeatInterval|System.Single|0.2|

### Component: EffectArea (PlayerBase)

|Field|Type|Default Value|
|-----|----|-------------|
|m_statusEffect|System.String||
|m_playerOnly|System.Boolean|False|

### Component: ProximityState (HatchProxy)

|Field|Type|Default Value|
|-----|----|-------------|
|m_playerOnly|System.Boolean|True|

