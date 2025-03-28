## dvergrprops_lantern_standing

### Component: DropOnDestroyed (dvergrprops_lantern_standing)

|Field|Type|Default Value|
|---|---|---|
|m_spawnYOffset|System.Single|0.5|
|m_spawnYStep|System.Single|0.3|

### Component: WearNTear (dvergrprops_lantern_standing)

|Field|Type|Default Value|
|---|---|---|
|m_new|UnityEngine.GameObject|lantern|
|m_worn|UnityEngine.GameObject|lantern|
|m_broken|UnityEngine.GameObject|lantern|
|m_noRoofWear|System.Boolean|False|
|m_noSupportWear|System.Boolean|True|
|m_ashDamageImmune|System.Boolean|False|
|m_ashDamageResist|System.Boolean|False|
|m_burnable|System.Boolean|True|
|m_supports|System.Boolean|False|
|m_comOffset|UnityEngine.Vector3|(0.00, 0.00, 0.00)|
|m_forceCorrectCOMCalculation|System.Boolean|False|
|m_staticPosition|System.Boolean|True|
|m_health|System.Single|10|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|True|
|m_autoCreateFragments|System.Boolean|False|

### Component: LightFlicker (Point Light)

|Field|Type|Default Value|
|---|---|---|
|m_flickerIntensity|System.Single|0.1|
|m_flickerSpeed|System.Single|10|
|m_movement|System.Single|0.1|
|m_ttl|System.Single|0|
|m_fadeDuration|System.Single|0.2|
|m_fadeInDuration|System.Single|0|
|m_accessibilityBrightnessMultiplier|System.Single|1|

### Component: LightLod (Point Light)

|Field|Type|Default Value|
|---|---|---|
|m_lightLod|System.Boolean|True|
|m_lightDistance|System.Single|30|
|m_shadowLod|System.Boolean|True|
|m_shadowDistance|System.Single|10|

