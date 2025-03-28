## eitrrefinery

### Component: Piece (eitrrefinery)

|Field|Type|Default Value|
|---|---|---|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_eitrrefinery|
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
|m_notOnWood|System.Boolean|True|
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

### Component: Smelter (eitrrefinery)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$piece_eitrrefinery|
|m_addOreTooltip|System.String|$piece_smelter_additem|
|m_emptyOreTooltip|System.String|$piece_smelter_empty|
|m_enabledObject|UnityEngine.GameObject|_enabled|
|m_fuelItem|ItemDrop|Sap|
|m_maxOre|System.Int32|20|
|m_maxFuel|System.Int32|20|
|m_fuelPerProduct|System.Int32|1|
|m_secPerProduct|System.Single|40|
|m_spawnStack|System.Boolean|False|
|m_requiresRoof|System.Boolean|False|
|m_addOreAnimationDuration|System.Single|2|

### Component: WearNTear (eitrrefinery)

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
|m_supports|System.Boolean|True|
|m_comOffset|UnityEngine.Vector3|(0.00, 2.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|1000|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

### Component: GuidePoint (GuidePoint)

|Field|Type|Default Value|
|---|---|---|
|m_ravenPrefab|UnityEngine.GameObject|Ravens|

### Component: Switch (add_ore)

|Field|Type|Default Value|
|---|---|---|
|m_hoverText|System.String||
|m_name|System.String|$piece_eitrrefinery|
|m_holdRepeatInterval|System.Single|0.2|

### Component: Switch (add_fuel)

|Field|Type|Default Value|
|---|---|---|
|m_hoverText|System.String||
|m_name|System.String|$piece_eitrrefinery|
|m_holdRepeatInterval|System.Single|0.2|

### Component: EffectArea (PlayerBase)

|Field|Type|Default Value|
|---|---|---|
|m_statusEffect|System.String||
|m_playerOnly|System.Boolean|False|

### Component: LightFlicker (Point light)

|Field|Type|Default Value|
|---|---|---|
|m_flickerIntensity|System.Single|0.1|
|m_flickerSpeed|System.Single|1|
|m_movement|System.Single|0|
|m_ttl|System.Single|0|
|m_fadeDuration|System.Single|0.2|
|m_fadeInDuration|System.Single|0|
|m_accessibilityBrightnessMultiplier|System.Single|1|

### Component: LightLod (Point light)

|Field|Type|Default Value|
|---|---|---|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|30|
|m_shadowLod|System.Boolean|True|
|m_shadowDistance|System.Single|20|

