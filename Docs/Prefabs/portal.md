## portal

### Component: Piece (portal)

|Field|Type|Default Value|
|-----|----|-------------|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_portal|
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

### Component: WearNTear (portal)

|Field|Type|Default Value|
|-----|----|-------------|
|m_new|UnityEngine.GameObject|New|
|m_worn|UnityEngine.GameObject|New|
|m_broken|UnityEngine.GameObject|New|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|True|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|1000|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

### Component: TeleportWorld (portal)

|Field|Type|Default Value|
|-----|----|-------------|
|m_activationRange|System.Single|5|
|m_exitDistance|System.Single|1|
|m_allowAllItems|System.Boolean|False|

### Component: EffectFade (_target_found)

|Field|Type|Default Value|
|-----|----|-------------|
|m_fadeDuration|System.Single|1|

### Component: TeleportWorldTrigger (TELEPORT)

### Component: Billboard (portcal_center)

|Field|Type|Default Value|
|-----|----|-------------|
|m_vertical|System.Boolean|True|
|m_invert|System.Boolean|False|

### Component: LightFlicker (Point light)

|Field|Type|Default Value|
|-----|----|-------------|
|m_flickerIntensity|System.Single|0.1|
|m_flickerSpeed|System.Single|10|
|m_movement|System.Single|0.1|
|m_ttl|System.Single|0|
|m_fadeDuration|System.Single|0.2|
|m_fadeInDuration|System.Single|0|
|m_accessibilityBrightnessMultiplier|System.Single|1|

### Component: EffectArea (PlayerBase)

|Field|Type|Default Value|
|-----|----|-------------|
|m_statusEffect|System.String||
|m_playerOnly|System.Boolean|False|

