## piece_turret

### Component: Turret (piece_turret)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$piece_turret|
|m_turretBody|UnityEngine.GameObject|BodyRotation|
|m_turretBodyArmed|UnityEngine.GameObject|Body|
|m_turretBodyUnarmed|UnityEngine.GameObject|Body_Unarmed|
|m_turretNeck|UnityEngine.GameObject|NeckRotation|
|m_eye|UnityEngine.GameObject|Eye|
|m_turnRate|System.Single|45|
|m_horizontalAngle|System.Single|50|
|m_verticalAngle|System.Single|50|
|m_viewDistance|System.Single|30|
|m_noTargetScanRate|System.Single|7|
|m_lookAcceleration|System.Single|1.2|
|m_lookDeacceleration|System.Single|0.05|
|m_lookMinDegreesDelta|System.Single|0.005|
|m_attackCooldown|System.Single|2|
|m_attackWarmup|System.Single|1|
|m_hitNoise|System.Single|10|
|m_shootWhenAimDiff|System.Single|0.9999|
|m_predictionModifier|System.Single|2|
|m_updateTargetIntervalNear|System.Single|1|
|m_updateTargetIntervalFar|System.Single|4|
|m_maxAmmo|System.Int32|40|
|m_ammoType|System.String|$ammo_turretbolt|
|m_returnAmmoOnDestroy|System.Boolean|True|
|m_holdRepeatInterval|System.Single|0.2|
|m_targetPlayers|System.Boolean|True|
|m_targetTamed|System.Boolean|True|
|m_targetEnemies|System.Boolean|True|
|m_targetTamedConfig|System.Boolean|False|
|m_maxConfigTargets|System.Int32|1|
|m_markerHideTime|System.Single|0.5|

### Component: Piece (piece_turret)

|Field|Type|Default Value|
|---|---|---|
|m_targetNonPlayerBuilt|System.Boolean|True|
|m_name|System.String|$piece_turret|
|m_description|System.String|$piece_turret_description|
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
|m_noInWater|System.Boolean|False|
|m_notOnWood|System.Boolean|False|
|m_notOnTiltingSurface|System.Boolean|True|
|m_inCeilingOnly|System.Boolean|False|
|m_notOnFloor|System.Boolean|False|
|m_noClipping|System.Boolean|True|
|m_onlyInTeleportArea|System.Boolean|False|
|m_allowedInDungeons|System.Boolean|True|
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

### Component: WearNTear (piece_turret)

|Field|Type|Default Value|
|---|---|---|
|m_new|UnityEngine.GameObject|New|
|m_worn|UnityEngine.GameObject|New|
|m_broken|UnityEngine.GameObject|New|
|m_noRoofWear|System.Boolean|True|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|False|
|m_comOffset|UnityEngine.Vector3|(0.00, -0.50, -0.50)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|400|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|True|

