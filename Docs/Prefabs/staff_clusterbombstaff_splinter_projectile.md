## staff_clusterbombstaff_splinter_projectile

### Component: Projectile (staff_clusterbombstaff_splinter_projectile)

|Field|Type|Default Value|
|-----|----|-------------|
|m_aoe|System.Single|0.6|
|m_dodgeable|System.Boolean|True|
|m_blockable|System.Boolean|True|
|m_attackForce|System.Single|100|
|m_backstabBonus|System.Single|4|
|m_statusEffect|System.String||
|m_healthReturn|System.Single|0|
|m_canHitWater|System.Boolean|False|
|m_ttl|System.Single|20|
|m_gravity|System.Single|15|
|m_drag|System.Single|0.33|
|m_rayRadius|System.Single|0.1|
|m_hitNoise|System.Single|10|
|m_doOwnerRaytest|System.Boolean|True|
|m_stayAfterHitStatic|System.Boolean|False|
|m_stayAfterHitDynamic|System.Boolean|False|
|m_stayTTL|System.Single|1|
|m_attachToRigidBody|System.Boolean|False|
|m_attachToClosestBone|System.Boolean|False|
|m_attachPenetration|System.Single|0|
|m_attachBoneNearify|System.Single|0.25|
|m_stopEmittersOnHit|System.Boolean|True|
|m_bounce|System.Boolean|True|
|m_bounceOnWater|System.Boolean|False|
|m_bouncePower|System.Single|1|
|m_bounceRoughness|System.Single|0.2|
|m_maxBounces|System.Int32|6|
|m_minBounceVel|System.Single|1|
|m_respawnItemOnHit|System.Boolean|False|
|m_spawnOnTtl|System.Boolean|False|
|m_spawnOnHitChance|System.Single|1|
|m_spawnCount|System.Int32|24|
|m_randomSpawnOnHitCount|System.Int32|1|
|m_randomSpawnSkipLava|System.Boolean|False|
|m_showBreakMessage|System.Boolean|False|
|m_staticHitOnly|System.Boolean|False|
|m_groundHitOnly|System.Boolean|False|
|m_spawnOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_copyProjectileRotation|System.Boolean|True|
|m_spawnRandomRotation|System.Boolean|False|
|m_spawnFacingRotation|System.Boolean|False|
|m_spawnProjectileNewVelocity|System.Boolean|True|
|m_spawnProjectileMinVel|System.Single|2|
|m_spawnProjectileMaxVel|System.Single|4|
|m_spawnProjectileRandomDir|System.Single|1|
|m_spawnProjectileHemisphereDir|System.Boolean|False|
|m_projectilesInheritHitData|System.Boolean|False|
|m_onlySpawnedProjectilesDealDamage|System.Boolean|False|
|m_divideDamageBetweenProjectiles|System.Boolean|True|
|m_rotateVisual|System.Single|1|
|m_rotateVisualY|System.Single|0|
|m_rotateVisualZ|System.Single|0|
|m_visual|UnityEngine.GameObject|splinter|
|m_canChangeVisuals|System.Boolean|False|
|m_startPoint|UnityEngine.Vector3|(0.00, 0.00, 0.00)|

### Component: ZSyncTransform (staff_clusterbombstaff_splinter_projectile)

|Field|Type|Default Value|
|-----|----|-------------|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

