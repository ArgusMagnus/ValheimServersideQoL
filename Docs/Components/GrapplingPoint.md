# GrapplingPoint

The following section headers are in the format `Prefab.name: Component.name`.

## GrapplingPoint: GrapplingPoint

|Field|Type|Default Value|
|-----|----|-------------|
|m_pullForce|System.Single|20|
|m_distanceForceMultiplier|System.Single|2|
|m_closeBreakDist|System.Single|1|
|m_closePullDist|System.Single|1|
|m_dotBreakValue|System.Single|0|
|m_breakEarlyTime|System.Single|0.5|
|m_stopOnPoint|System.Boolean|True|
|m_stopOnlyXZ|System.Boolean|True|
|m_repellingDotStart|System.Single|0.45|
|m_repellingDotEnd|System.Single|0.98|
|m_repellingMinDistance|System.Single|2|
|m_repellingInForce|System.Single|5|
|m_maxLength|System.Single|60|
|m_jumpOnDone|System.Single|10|
|m_jumpPullMaxDist|System.Single|5|
|m_FOVTarget|System.Single|110|
|m_FOVInertia|System.Single|0.1|
|m_secondary|System.Boolean|False|
|m_ttlSE|System.Single|2|
|m_repellingAirControl|System.Single|0.25|
|m_spawnOnDone|UnityEngine.GameObject|GrapplingPointSecondary|
|m_equipCheck|ItemDrop|GrapplingHook|
|m_attachOffsetHand|UnityEngine.Vector3|(0.10, -0.07, 0.66)|
|m_attachOffsetProjectile|UnityEngine.Vector3|(0.00, 0.00, -0.64)|
|m_rotateCharacter|System.Boolean|True|

## GrapplingPointSecondary: GrapplingPointSecondary

|Field|Type|Default Value|
|-----|----|-------------|
|m_pullForce|System.Single|15|
|m_distanceForceMultiplier|System.Single|2|
|m_closeBreakDist|System.Single|1|
|m_closePullDist|System.Single|1|
|m_dotBreakValue|System.Single|0|
|m_breakEarlyTime|System.Single|0.5|
|m_stopOnPoint|System.Boolean|True|
|m_stopOnlyXZ|System.Boolean|True|
|m_repellingDotStart|System.Single|0.7|
|m_repellingDotEnd|System.Single|0.98|
|m_repellingMinDistance|System.Single|2|
|m_repellingInForce|System.Single|40|
|m_maxLength|System.Single|70|
|m_jumpOnDone|System.Single|0|
|m_jumpPullMaxDist|System.Single|5|
|m_FOVTarget|System.Single|0|
|m_FOVInertia|System.Single|0.1|
|m_secondary|System.Boolean|True|
|m_ttlSE|System.Single|3|
|m_repellingAirControl|System.Single|0.25|
|m_spawnOnDone|UnityEngine.GameObject|*null*|
|m_equipCheck|ItemDrop|GrapplingHook|
|m_attachOffsetHand|UnityEngine.Vector3|(0.10, -0.07, 0.66)|
|m_attachOffsetProjectile|UnityEngine.Vector3|(0.00, 0.00, -0.64)|
|m_rotateCharacter|System.Boolean|False|

