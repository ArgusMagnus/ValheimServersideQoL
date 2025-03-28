## ArmorStand_Female

### Component: Piece (ArmorStand_Female)

|Field|Type|Default Value|
|---|---|---|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_armorstand|
|m_description|System.String|Horizontal|
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
|m_primaryTarget|System.Boolean|False|
|m_randomTarget|System.Boolean|False|

### Component: WearNTear (ArmorStand_Female)

|Field|Type|Default Value|
|---|---|---|
|m_new|UnityEngine.GameObject|Blocks & Switches|
|m_worn|UnityEngine.GameObject|Blocks & Switches|
|m_broken|UnityEngine.GameObject|Blocks & Switches|
|m_noRoofWear|System.Boolean|True|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|False|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|50|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

### Component: ArmorStand (ArmorStand_Female)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$piece_armorstand|
|m_poseCount|System.Int32|15|
|m_startPose|System.Int32|0|
|m_clothSimLodDistance|System.Single|10|

### Component: Switch (block body)

|Field|Type|Default Value|
|---|---|---|
|m_hoverText|System.String|Armor stand body|
|m_name|System.String||
|m_holdRepeatInterval|System.Single|-1|

### Component: Switch (block hand R)

|Field|Type|Default Value|
|---|---|---|
|m_hoverText|System.String|Armor stand right arm|
|m_name|System.String||
|m_holdRepeatInterval|System.Single|-1|

### Component: Switch (block hand L)

|Field|Type|Default Value|
|---|---|---|
|m_hoverText|System.String|Armor stand left arm|
|m_name|System.String||
|m_holdRepeatInterval|System.Single|-1|

### Component: Switch (block back R)

|Field|Type|Default Value|
|---|---|---|
|m_hoverText|System.String|Armor stand back shield|
|m_name|System.String||
|m_holdRepeatInterval|System.Single|-1|

### Component: Switch (block back L)

|Field|Type|Default Value|
|---|---|---|
|m_hoverText|System.String|Armor stand back weapon|
|m_name|System.String||
|m_holdRepeatInterval|System.Single|-1|

### Component: Switch (block pose)

|Field|Type|Default Value|
|---|---|---|
|m_hoverText|System.String||
|m_name|System.String||
|m_holdRepeatInterval|System.Single|-1|

### Component: VisEquipment (Player Pose)

|Field|Type|Default Value|
|---|---|---|
|m_isPlayer|System.Boolean|True|
|m_useAllTrails|System.Boolean|False|

