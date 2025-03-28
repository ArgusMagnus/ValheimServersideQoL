## YggaShoot3

### Component: TreeBase (YggaShoot3)

|Field|Type|Default Value|
|---|---|---|
|m_health|System.Single|100|
|m_minToolTier|System.Int32|4|
|m_trunk|UnityEngine.GameObject|Lod0|
|m_stubPrefab|UnityEngine.GameObject|ShootStump|
|m_logPrefab|UnityEngine.GameObject|yggashoot_log|
|m_spawnYOffset|System.Single|4|
|m_spawnYStep|System.Single|0.3|

### Component: LodFadeInOut (YggaShoot3)

### Component: StaticPhysics (YggaShoot3)

|Field|Type|Default Value|
|---|---|---|
|m_pushUp|System.Boolean|True|
|m_fall|System.Boolean|True|
|m_checkSolids|System.Boolean|True|
|m_fallCheckRadius|System.Single|0.2|

### Component: HoverText (YggaShoot3)

|Field|Type|Default Value|
|---|---|---|
|m_text|System.String|$prop_yggashoot|

### Component: GlobalWind (leaf_particles)

|Field|Type|Default Value|
|---|---|---|
|m_multiplier|System.Single|2|
|m_smoothUpdate|System.Boolean|False|
|m_alignToWindDirection|System.Boolean|False|
|m_particleVelocity|System.Boolean|False|
|m_particleForce|System.Boolean|True|
|m_particleEmission|System.Boolean|False|
|m_particleEmissionMin|System.Int32|0|
|m_particleEmissionMax|System.Int32|1|
|m_clothRandomAccelerationFactor|System.Single|0.5|
|m_checkPlayerShelter|System.Boolean|False|

