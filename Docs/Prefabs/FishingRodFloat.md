## FishingRodFloat

### Component: ZSyncTransform (FishingRodFloat)

|Field|Type|Default Value|
|-----|----|-------------|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: Floating (FishingRodFloat)

|Field|Type|Default Value|
|-----|----|-------------|
|m_waterLevelOffset|System.Single|0.3|
|m_forceDistance|System.Single|0.5|
|m_force|System.Single|0.5|
|m_balanceForceFraction|System.Single|0.1|
|m_damping|System.Single|0.1|
|m_surfaceEffects|UnityEngine.GameObject|SurfaceEffect|

### Component: FishingFloat (FishingRodFloat)

|Field|Type|Default Value|
|-----|----|-------------|
|m_maxDistance|System.Single|30|
|m_moveForce|System.Single|5|
|m_pullLineSpeed|System.Single|2|
|m_pullLineSpeedMaxSkill|System.Single|6|
|m_pullStaminaUse|System.Single|0|
|m_pullStaminaUseMaxSkillMultiplier|System.Single|0.2|
|m_hookedStaminaPerSec|System.Single|1|
|m_hookedStaminaPerSecMaxSkill|System.Single|0.2|
|m_breakDistance|System.Single|10|
|m_range|System.Single|50|
|m_nibbleForce|System.Single|3|
|m_maxLineSlack|System.Single|0.3|

### Component: LineConnect (RodLine)

|Field|Type|Default Value|
|-----|----|-------------|
|m_centerOfCharacter|System.Boolean|False|
|m_childObject|System.String|_RodTop|
|m_hideIfNoConnection|System.Boolean|True|
|m_noConnectionWorldOffset|UnityEngine.Vector3|(0.00, -1.00, 0.00)|
|m_dynamicSlack|System.Boolean|True|
|m_slack|System.Single|0|
|m_dynamicThickness|System.Boolean|False|
|m_minDistance|System.Single|6|
|m_maxDistance|System.Single|100|
|m_minThickness|System.Single|0.2|
|m_maxThickness|System.Single|0.8|
|m_thicknessPower|System.Single|0.2|
|m_netViewPrefix|System.String|rod|

### Component: LineConnect (HookLine)

|Field|Type|Default Value|
|-----|----|-------------|
|m_centerOfCharacter|System.Boolean|False|
|m_childObject|System.String||
|m_hideIfNoConnection|System.Boolean|False|
|m_noConnectionWorldOffset|UnityEngine.Vector3|(0.00, -0.50, 0.00)|
|m_dynamicSlack|System.Boolean|False|
|m_slack|System.Single|0|
|m_dynamicThickness|System.Boolean|False|
|m_minDistance|System.Single|6|
|m_maxDistance|System.Single|100|
|m_minThickness|System.Single|0.2|
|m_maxThickness|System.Single|0.8|
|m_thicknessPower|System.Single|0.2|
|m_netViewPrefix|System.String|hook|

