## windmill

### Component: Piece (windmill)

|Field|Type|Default Value|
|-----|----|-------------|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_windmill|
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

### Component: Smelter (windmill)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|$piece_windmill|
|m_addOreTooltip|System.String|$piece_smelter_add|
|m_emptyOreTooltip|System.String|$piece_smelter_empty|
|m_enabledObject|UnityEngine.GameObject|_enabled|
|m_maxOre|System.Int32|50|
|m_maxFuel|System.Int32|0|
|m_fuelPerProduct|System.Int32|0|
|m_secPerProduct|System.Single|10|
|m_spawnStack|System.Boolean|True|
|m_requiresRoof|System.Boolean|False|
|m_addOreAnimationDuration|System.Single|0|

### Component: WearNTear (windmill)

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
|m_supports|System.Boolean|False|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|1000|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

### Component: Windmill (windmill)

|Field|Type|Default Value|
|-----|----|-------------|
|m_propellerAOE|UnityEngine.GameObject|BladeAOE|
|m_minAOEPropellerSpeed|System.Single|180|
|m_bomRotationSpeed|System.Single|100|
|m_propellerRotationSpeed|System.Single|-600|
|m_grindstoneRotationSpeed|System.Single|300|
|m_minWindSpeed|System.Single|0.1|
|m_minPitch|System.Single|0.3|
|m_maxPitch|System.Single|1.5|
|m_maxPitchVel|System.Single|1|
|m_maxVol|System.Single|1|
|m_maxVolVel|System.Single|0.5|
|m_audioChangeSpeed|System.Single|2|

### Component: LodFadeInOut (windmill)

### Component: EffectArea (PlayerBase)

|Field|Type|Default Value|
|-----|----|-------------|
|m_statusEffect|System.String||
|m_playerOnly|System.Boolean|False|

### Component: Switch (add_switch)

|Field|Type|Default Value|
|-----|----|-------------|
|m_hoverText|System.String||
|m_name|System.String|$piece_windmill|
|m_holdRepeatInterval|System.Single|0.2|

### Component: Switch (empty_switch)

|Field|Type|Default Value|
|-----|----|-------------|
|m_hoverText|System.String||
|m_name|System.String|$piece_windmill|
|m_holdRepeatInterval|System.Single|-1|

### Component: Aoe (BladeAOE)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String||
|m_useAttackSettings|System.Boolean|True|
|m_scaleDamageByDistance|System.Boolean|False|
|m_dodgeable|System.Boolean|False|
|m_blockable|System.Boolean|False|
|m_toolTier|System.Int32|0|
|m_attackForce|System.Single|100|
|m_backstabBonus|System.Single|1|
|m_statusEffect|System.String||
|m_statusEffectIfBoss|System.String||
|m_statusEffectIfPlayer|System.String||
|m_attackForceForward|System.Boolean|False|
|m_hitTerrainOnlyOnce|System.Boolean|False|
|m_groundLavaValue|System.Single|-1|
|m_hitNoise|System.Single|0|
|m_placeOnGround|System.Boolean|False|
|m_randomRotation|System.Boolean|False|
|m_maxTargetsFromCenter|System.Int32|0|
|m_multiSpawnMin|System.Int32|0|
|m_multiSpawnMax|System.Int32|0|
|m_multiSpawnDistanceMin|System.Single|0|
|m_multiSpawnDistanceMax|System.Single|0|
|m_multiSpawnScaleMin|System.Single|0|
|m_multiSpawnScaleMax|System.Single|0|
|m_multiSpawnSpringDelayMax|System.Single|0|
|m_chainStartChance|System.Single|0|
|m_chainStartChanceFalloff|System.Single|0.8|
|m_chainChancePerTarget|System.Single|0|
|m_chainStartDelay|System.Single|0|
|m_chainMinTargets|System.Int32|0|
|m_chainMaxTargets|System.Int32|0|
|m_damageSelf|System.Single|0|
|m_hitOwner|System.Boolean|False|
|m_hitParent|System.Boolean|True|
|m_hitSame|System.Boolean|False|
|m_hitFriendly|System.Boolean|True|
|m_hitEnemy|System.Boolean|True|
|m_hitCharacters|System.Boolean|True|
|m_hitProps|System.Boolean|False|
|m_hitTerrain|System.Boolean|False|
|m_ignorePVP|System.Boolean|False|
|m_launchCharacters|System.Boolean|False|
|m_launchForceUpFactor|System.Single|0.5|
|m_canRaiseSkill|System.Boolean|True|
|m_useTriggers|System.Boolean|True|
|m_triggerEnterOnly|System.Boolean|False|
|m_radius|System.Single|4|
|m_activationDelay|System.Single|0|
|m_ttl|System.Single|0|
|m_ttlMax|System.Single|0|
|m_hitAfterTtl|System.Boolean|False|
|m_hitInterval|System.Single|2|
|m_hitOnEnable|System.Boolean|False|
|m_attachToCaster|System.Boolean|False|

