## Player_tombstone

### Component: TombStone (Player_tombstone)

|Field|Type|Default Value|
|-----|----|-------------|
|m_text|System.String|$piece_tombstone|
|m_floater|UnityEngine.GameObject|floater|
|m_spawnUpVel|System.Single|10|

### Component: Container (Player_tombstone)

|Field|Type|Default Value|
|-----|----|-------------|
|m_name|System.String|Grave|
|m_width|System.Int32|8|
|m_height|System.Int32|4|
|m_checkGuardStone|System.Boolean|False|
|m_autoDestroyEmpty|System.Boolean|False|

### Component: ZSyncTransform (Player_tombstone)

|Field|Type|Default Value|
|-----|----|-------------|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: Floating (Player_tombstone)

|Field|Type|Default Value|
|-----|----|-------------|
|m_waterLevelOffset|System.Single|0.8|
|m_forceDistance|System.Single|1|
|m_force|System.Single|0.5|
|m_balanceForceFraction|System.Single|0.02|
|m_damping|System.Single|0.05|

### Component: FloatingTerrain (Player_tombstone)

|Field|Type|Default Value|
|-----|----|-------------|
|m_padding|System.Single|0|
|m_waveMinOffset|System.Single|0.25|
|m_waveFreq|System.Single|1|
|m_waveAmp|System.Single|0.15|
|m_maxCorrectionSpeed|System.Single|0.025|
|m_copyLayer|System.Boolean|True|

### Component: CanvasScaler (Canvas)

### Component: Billboard (Canvas)

|Field|Type|Default Value|
|-----|----|-------------|
|m_vertical|System.Boolean|False|
|m_invert|System.Boolean|True|

### Component: TextMeshProUGUI (Text)

