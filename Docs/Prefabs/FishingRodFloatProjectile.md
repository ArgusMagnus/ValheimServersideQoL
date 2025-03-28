## FishingRodFloatProjectile

### Component: Projectile (FishingRodFloatProjectile)

|Field|Type|Default Value|
|-----|----|-------------|
|m_aoe|System.Single|0|
|m_dodgeable|System.Boolean|False|
|m_blockable|System.Boolean|False|
|m_attackForce|System.Single|30|
|m_backstabBonus|System.Single|4|
|m_statusEffect|System.String||
|m_healthReturn|System.Single|0|
|m_canHitWater|System.Boolean|True|
|m_ttl|System.Single|20|
|m_gravity|System.Single|10|
|m_drag|System.Single|0|
|m_rayRadius|System.Single|0.2|
|m_hitNoise|System.Single|0|
|m_doOwnerRaytest|System.Boolean|False|
|m_stayAfterHitStatic|System.Boolean|False|
|m_stayAfterHitDynamic|System.Boolean|False|
|m_stayTTL|System.Single|1|
|m_attachToRigidBody|System.Boolean|False|
|m_attachToClosestBone|System.Boolean|False|
|m_attachPenetration|System.Single|0|
|m_attachBoneNearify|System.Single|0.25|
|m_stopEmittersOnHit|System.Boolean|True|
|m_bounce|System.Boolean|False|
|m_bounceOnWater|System.Boolean|False|
|m_bouncePower|System.Single|0.85|
|m_bounceRoughness|System.Single|0.3|
|m_maxBounces|System.Int32|99|
|m_minBounceVel|System.Single|0.25|
|m_respawnItemOnHit|System.Boolean|False|
|m_spawnOnTtl|System.Boolean|False|
|m_spawnOnHit|UnityEngine.GameObject|FishingRodFloat|
|m_spawnOnHitChance|System.Single|1|
|m_spawnCount|System.Int32|1|
|m_randomSpawnOnHitCount|System.Int32|1|
|m_randomSpawnSkipLava|System.Boolean|False|
|m_showBreakMessage|System.Boolean|False|
|m_staticHitOnly|System.Boolean|False|
|m_groundHitOnly|System.Boolean|False|
|m_spawnOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_copyProjectileRotation|System.Boolean|True|
|m_spawnRandomRotation|System.Boolean|False|
|m_spawnFacingRotation|System.Boolean|False|
|m_spawnProjectileNewVelocity|System.Boolean|False|
|m_spawnProjectileMinVel|System.Single|1|
|m_spawnProjectileMaxVel|System.Single|5|
|m_spawnProjectileRandomDir|System.Single|0|
|m_spawnProjectileHemisphereDir|System.Boolean|False|
|m_projectilesInheritHitData|System.Boolean|False|
|m_onlySpawnedProjectilesDealDamage|System.Boolean|False|
|m_divideDamageBetweenProjectiles|System.Boolean|False|
|m_rotateVisual|System.Single|-200|
|m_rotateVisualY|System.Single|0|
|m_rotateVisualZ|System.Single|0|
|m_visual|UnityEngine.GameObject|default|
|m_canChangeVisuals|System.Boolean|False|
|m_startPoint|UnityEngine.Vector3|(0.00, 0.00, 0.00)|

### Component: ZSyncTransform (FishingRodFloatProjectile)

|Field|Type|Default Value|
|-----|----|-------------|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: LineConnect (FishingRodFloatProjectile)

|Field|Type|Default Value|
|-----|----|-------------|
|m_centerOfCharacter|System.Boolean|False|
|m_childObject|System.String|_RodTop|
|m_hideIfNoConnection|System.Boolean|True|
|m_noConnectionWorldOffset|UnityEngine.Vector3|(0.00, -1.00, 0.00)|
|m_dynamicSlack|System.Boolean|False|
|m_slack|System.Single|0.5|
|m_dynamicThickness|System.Boolean|False|
|m_minDistance|System.Single|6|
|m_maxDistance|System.Single|30|
|m_minThickness|System.Single|0.2|
|m_maxThickness|System.Single|0.8|
|m_thicknessPower|System.Single|0.2|
|m_netViewPrefix|System.String||

