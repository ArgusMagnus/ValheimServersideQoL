## BatteringRam

### Component: DisableInPlacementGhost (BatteringRam)

|Field|Type|Default Value|
|---|---|---|

### Component: Vagon (BatteringRam)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$tool_batteringram|
|m_detachDistance|System.Single|1|
|m_attachOffset|UnityEngine.Vector3|(0.00, 0.80, 0.00)|
|m_lineAttachOffset|UnityEngine.Vector3|(0.00, 1.00, 0.00)|
|m_breakForce|System.Single|100000|
|m_spring|System.Single|5000|
|m_springDamping|System.Single|1000|
|m_baseMass|System.Single|50|
|m_itemWeightMassFactor|System.Single|0.1|
|m_playerExtraPullMass|System.Single|10|
|m_minPitch|System.Single|0.9|
|m_maxPitch|System.Single|1.1|
|m_maxPitchVel|System.Single|7|
|m_maxVol|System.Single|0.3|
|m_maxVolVel|System.Single|7|
|m_audioChangeSpeed|System.Single|2|

### Component: SiegeMachine (BatteringRam)

|Field|Type|Default Value|
|---|---|---|
|m_enabledWhenAttached|System.Boolean|True|
|m_chargeTime|System.Single|2|
|m_hitDelay|System.Single|0.25|
|m_chargeOffsetDistance|System.Single|1.6|
|m_aoe|UnityEngine.GameObject|AOEs|

### Component: Floating (BatteringRam)

|Field|Type|Default Value|
|---|---|---|
|m_waterLevelOffset|System.Single|0.5|
|m_forceDistance|System.Single|1|
|m_force|System.Single|1.3|
|m_balanceForceFraction|System.Single|0.02|
|m_damping|System.Single|0.05|

### Component: ZSyncTransform (BatteringRam)

|Field|Type|Default Value|
|---|---|---|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|True|
|m_characterParentSync|System.Boolean|False|

### Component: Piece (BatteringRam)

|Field|Type|Default Value|
|---|---|---|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$tool_batteringram|
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
|m_noInWater|System.Boolean|False|
|m_notOnWood|System.Boolean|False|
|m_notOnTiltingSurface|System.Boolean|False|
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
|m_randomTarget|System.Boolean|False|

### Component: WearNTear (BatteringRam)

|Field|Type|Default Value|
|---|---|---|
|m_new|UnityEngine.GameObject|new|
|m_worn|UnityEngine.GameObject|worn|
|m_broken|UnityEngine.GameObject|broken|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|False|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|True|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|False|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|False|
|m_health|System.Single|3000|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

### Component: ImpactEffect (BatteringRam)

|Field|Type|Default Value|
|---|---|---|
|m_hitDestroyChance|System.Single|0|
|m_minVelocity|System.Single|3|
|m_maxVelocity|System.Single|5|
|m_damageToSelf|System.Boolean|True|
|m_damagePlayers|System.Boolean|False|
|m_damageFish|System.Boolean|False|
|m_toolTier|System.Int32|0|
|m_interval|System.Single|0.5|

### Component: GuidePoint (GuidePoint)

|Field|Type|Default Value|
|---|---|---|
|m_ravenPrefab|UnityEngine.GameObject|Ravens|

### Component: Smelter (kiln engine)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$tool_batteringram|
|m_addOreTooltip|System.String|$piece_smelter_add Wood|
|m_emptyOreTooltip|System.String|$piece_smelter_empty|
|m_enabledObject|UnityEngine.GameObject|_enabled|
|m_maxOre|System.Int32|25|
|m_maxFuel|System.Int32|0|
|m_fuelPerProduct|System.Int32|0|
|m_secPerProduct|System.Single|20|
|m_spawnStack|System.Boolean|False|
|m_requiresRoof|System.Boolean|False|
|m_addOreAnimationDuration|System.Single|0|

### Component: Switch (add_ore)

|Field|Type|Default Value|
|---|---|---|
|m_hoverText|System.String||
|m_name|System.String||
|m_holdRepeatInterval|System.Single|0.2|

### Component: ZSFX (Audio Source (1))

|Field|Type|Default Value|
|---|---|---|
|m_playOnAwake|System.Boolean|True|
|m_closedCaptionToken|System.String||
|m_secondaryCaptionToken|System.String||
|m_minimumCaptionVolume|System.Single|0.3|
|m_maxConcurrentSources|System.Int32|0|
|m_ignoreConcurrencyDistance|System.Boolean|False|
|m_maxPitch|System.Single|1|
|m_minPitch|System.Single|1|
|m_maxVol|System.Single|1|
|m_minVol|System.Single|1|
|m_fadeInDuration|System.Single|0|
|m_fadeOutDuration|System.Single|0|
|m_fadeOutDelay|System.Single|0|
|m_fadeOutOnAwake|System.Boolean|False|
|m_randomPan|System.Boolean|False|
|m_minPan|System.Single|-1|
|m_maxPan|System.Single|1|
|m_maxDelay|System.Single|0|
|m_minDelay|System.Single|0|
|m_distanceReverb|System.Boolean|True|
|m_useCustomReverbDistance|System.Boolean|False|
|m_customReverbDistance|System.Single|10|
|m_hash|System.Int32|-1709386197|

### Component: ZSFX (Audio Source)

|Field|Type|Default Value|
|---|---|---|
|m_playOnAwake|System.Boolean|True|
|m_closedCaptionToken|System.String||
|m_secondaryCaptionToken|System.String||
|m_minimumCaptionVolume|System.Single|0.3|
|m_maxConcurrentSources|System.Int32|0|
|m_ignoreConcurrencyDistance|System.Boolean|False|
|m_maxPitch|System.Single|1|
|m_minPitch|System.Single|1|
|m_maxVol|System.Single|1|
|m_minVol|System.Single|1|
|m_fadeInDuration|System.Single|0|
|m_fadeOutDuration|System.Single|0|
|m_fadeOutDelay|System.Single|0|
|m_fadeOutOnAwake|System.Boolean|False|
|m_randomPan|System.Boolean|False|
|m_minPan|System.Single|-1|
|m_maxPan|System.Single|1|
|m_maxDelay|System.Single|0|
|m_minDelay|System.Single|0|
|m_distanceReverb|System.Boolean|True|
|m_useCustomReverbDistance|System.Boolean|False|
|m_customReverbDistance|System.Single|10|
|m_hash|System.Int32|-1967749462|

