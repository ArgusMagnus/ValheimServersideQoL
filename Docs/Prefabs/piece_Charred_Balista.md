## piece_Charred_Balista

### Component: Turret (piece_Charred_Balista)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$piece_charredballista|
|m_turretBody|UnityEngine.GameObject|BodyRotation|
|m_turretBodyArmed|UnityEngine.GameObject|Body|
|m_turretBodyUnarmed|UnityEngine.GameObject|Body_Unarmed|
|m_turretNeck|UnityEngine.GameObject|NeckRotation|
|m_eye|UnityEngine.GameObject|Eye|
|m_turnRate|System.Single|60|
|m_horizontalAngle|System.Single|75|
|m_verticalAngle|System.Single|75|
|m_viewDistance|System.Single|40|
|m_noTargetScanRate|System.Single|7|
|m_lookAcceleration|System.Single|1.2|
|m_lookDeacceleration|System.Single|0.05|
|m_lookMinDegreesDelta|System.Single|0.005|
|m_defaultAmmo|ItemDrop|TurretBoltBone|
|m_attackCooldown|System.Single|2|
|m_attackWarmup|System.Single|1|
|m_hitNoise|System.Single|10|
|m_shootWhenAimDiff|System.Single|0.9999|
|m_predictionModifier|System.Single|2|
|m_updateTargetIntervalNear|System.Single|1|
|m_updateTargetIntervalFar|System.Single|4|
|m_maxAmmo|System.Int32|0|
|m_ammoType|System.String|$ammo_turretbolt|
|m_returnAmmoOnDestroy|System.Boolean|True|
|m_holdRepeatInterval|System.Single|0.2|
|m_targetPlayers|System.Boolean|True|
|m_targetTamed|System.Boolean|True|
|m_targetEnemies|System.Boolean|False|
|m_targetTamedConfig|System.Boolean|False|
|m_maxConfigTargets|System.Int32|1|
|m_markerHideTime|System.Single|0.5|

### Component: WearNTear (piece_Charred_Balista)

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

### Component: DropOnDestroyed (piece_Charred_Balista)

|Field|Type|Default Value|
|---|---|---|
|m_spawnYOffset|System.Single|0.5|
|m_spawnYStep|System.Single|0.3|

