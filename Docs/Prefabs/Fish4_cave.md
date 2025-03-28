## Fish4_cave

### Component: ZSyncTransform (Fish4_cave)

|Field|Type|Default Value|
|---|---|---|
|m_syncPosition|System.Boolean|True|
|m_syncRotation|System.Boolean|True|
|m_syncScale|System.Boolean|False|
|m_syncBodyVelocity|System.Boolean|False|
|m_characterParentSync|System.Boolean|False|

### Component: Destructible (Fish4_cave)

|Field|Type|Default Value|
|---|---|---|
|m_health|System.Single|1|
|m_minDamageTreshold|System.Single|0|
|m_minToolTier|System.Int32|0|
|m_hitNoise|System.Single|0|
|m_destroyNoise|System.Single|0|
|m_triggerPrivateArea|System.Boolean|False|
|m_ttl|System.Single|0|
|m_autoCreateFragments|System.Boolean|False|

### Component: Fish (Fish4_cave)

|Field|Type|Default Value|
|---|---|---|
|m_name|System.String|$animal_fish4|
|m_swimRange|System.Single|5|
|m_minDepth|System.Single|0.5|
|m_maxDepth|System.Single|1|
|m_speed|System.Single|2|
|m_acceleration|System.Single|0.1|
|m_turnRate|System.Single|110|
|m_wpDurationMin|System.Single|5|
|m_wpDurationMax|System.Single|15|
|m_avoidSpeedScale|System.Single|2|
|m_avoidRange|System.Single|10|
|m_height|System.Single|0.5|
|m_hookForce|System.Single|20|
|m_staminaUse|System.Single|11|
|m_escapeStaminaUse|System.Single|28|
|m_escapeMin|System.Single|0.5|
|m_escapeMax|System.Single|3|
|m_escapeWaitMin|System.Single|1.5|
|m_escapeWaitMax|System.Single|5|
|m_escapeMaxPerLevel|System.Single|1.5|
|m_baseHookChance|System.Single|0.1|
|m_pickupItemStackSize|System.Int32|4|
|m_blockChangeDurationMin|System.Single|0.1|
|m_blockChangeDurationMax|System.Single|0.6|
|m_collisionFleeTimeout|System.Single|1.5|
|m_jumpSpeed|System.Single|5|
|m_jumpHeight|System.Single|10|
|m_jumpForwardStrength|System.Single|11|
|m_jumpHeightLand|System.Single|8|
|m_jumpChance|System.Single|0.25|
|m_jumpOnLandChance|System.Single|0.2|
|m_jumpOnLandDecay|System.Single|0.85|
|m_maxJumpDepthOffset|System.Single|0.5|
|m_jumpFrequencySeconds|System.Single|0.3|
|m_jumpOnLandRotation|System.Single|2|
|m_waveJumpMultiplier|System.Single|0.05|
|m_jumpMaxLevel|System.Single|2|
|m_waveFollowDirection|System.Single|7|

### Component: ItemDrop (Fish4_cave)

|Field|Type|Default Value|
|---|---|---|
|m_autoPickup|System.Boolean|True|
|m_autoDestroy|System.Boolean|False|

### Component: RandomSpeak (DeadSpeak_Base)

|Field|Type|Default Value|
|---|---|---|
|m_interval|System.Single|60|
|m_chance|System.Single|0.02|
|m_triggerDistance|System.Single|10|
|m_cullDistance|System.Single|20|
|m_ttl|System.Single|15|
|m_offset|UnityEngine.Vector3|(0.00, -0.20, 0.00)|
|m_useLargeDialog|System.Boolean|True|
|m_onlyOnce|System.Boolean|False|
|m_onlyOnItemStand|System.Boolean|True|
|m_topic|System.String||

