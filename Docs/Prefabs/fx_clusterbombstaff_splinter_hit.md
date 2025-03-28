## fx_clusterbombstaff_splinter_hit

### Component: TimedDestruction (fx_clusterbombstaff_splinter_hit)

|Field|Type|Default Value|
|---|---|---|
|m_timeout|System.Single|8|
|m_triggerOnAwake|System.Boolean|True|
|m_forceTakeOwnershipAndDestroy|System.Boolean|False|

### Component: TimedDestruction (sfx_staff_grenade_impact_small)

|Field|Type|Default Value|
|---|---|---|
|m_timeout|System.Single|2|
|m_triggerOnAwake|System.Boolean|True|
|m_forceTakeOwnershipAndDestroy|System.Boolean|False|

### Component: ZSFX (sfx_staff_grenade_impact_small)

|Field|Type|Default Value|
|---|---|---|
|m_playOnAwake|System.Boolean|True|
|m_closedCaptionToken|System.String|$item_staffclusterbomb|
|m_secondaryCaptionToken|System.String|$caption_projexploding|
|m_minimumCaptionVolume|System.Single|0.3|
|m_maxConcurrentSources|System.Int32|0|
|m_ignoreConcurrencyDistance|System.Boolean|False|
|m_maxPitch|System.Single|1.3|
|m_minPitch|System.Single|0.7|
|m_maxVol|System.Single|2.7|
|m_minVol|System.Single|2.7|
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
|m_hash|System.Int32|-1533987619|

### Component: CamShaker (sfx_staff_grenade_impact_small)

|Field|Type|Default Value|
|---|---|---|
|m_strength|System.Single|0.25|
|m_range|System.Single|50|
|m_delay|System.Single|0|
|m_continous|System.Boolean|False|
|m_continousDuration|System.Single|0|
|m_localOnly|System.Boolean|False|

### Component: GlobalWind (smoke)

|Field|Type|Default Value|
|---|---|---|
|m_multiplier|System.Single|1|
|m_smoothUpdate|System.Boolean|False|
|m_alignToWindDirection|System.Boolean|False|
|m_particleVelocity|System.Boolean|True|
|m_particleForce|System.Boolean|False|
|m_particleEmission|System.Boolean|False|
|m_particleEmissionMin|System.Int32|0|
|m_particleEmissionMax|System.Int32|1|
|m_clothRandomAccelerationFactor|System.Single|0.5|
|m_checkPlayerShelter|System.Boolean|False|

### Component: LightFlicker (Point light)

|Field|Type|Default Value|
|---|---|---|
|m_flickerIntensity|System.Single|0|
|m_flickerSpeed|System.Single|0|
|m_movement|System.Single|0|
|m_ttl|System.Single|0.75|
|m_fadeDuration|System.Single|0.33|
|m_fadeInDuration|System.Single|0.05|
|m_accessibilityBrightnessMultiplier|System.Single|1|

