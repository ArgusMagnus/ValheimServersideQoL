<details open><summary><b>TameAssist</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|true|True/False|Enables/disables the entire mod|
|MakeCommandable|false|True/False|True to make all tames commandable (like wolves)|
|TamingProgressMessageType|InWorld|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of taming progress messages to show|
|GrowingProgressMessageType|InWorld|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of growing progress messages to show|
|FedDurationMultiplier|1||Multiply the time tames stay fed after they have eaten by this factor. Infinity to keep them fed indefinitely|
|TamingTimeMultiplier|1||Multiply the time it takes to tame a tameable creature by this factor. <br>E.g. a value of 0.5 means that the taming time is halved.|
|PotionTamingBoostMultiplier|1||Multiply the taming boost from the animal whispers potion by this factor. <br>E.g. a value of 2 means that the effect of the potion is doubled and the resulting taming time is reduced by a factor of 4 per player.|
|TeleportFollowers|true|True/False|True to teleport following tames and summons to the players location if the player gets too far away from them|
|TeleportFollowersMinDistance|64||Minimum distance from the player at which followers will be teleported to the player's location|
|TakeIntoDungeons|true|True/False|True to take followers into (and out of) dungeons with the player|
|FeedFromContainers|false|True/False|True to automatically feed tames from nearby containers|
|FeedFromContainersRange|0||Required proximity of a container to a tame to be used as feeding source. <br>Can be overridden per chest by putting '&lt;FeedFromContainersRangeSignPrefix&gt;&lt;Range&gt;' on a chest sign, e.g. '🐗64'. <br>  For example, '🐗64' increase the range of that chest to 64m. <br>  Only works with automatic chest signs added by the ServersideQoL.ContainerSigns mod.|
|FeedFromContainersRangeSignPrefix|🐗||Requires the ServersideQoL.ContainerSigns mod. <br>The prefix used to identify the container specifc feed range value in chest sign text.|
|FeedFromContainersMaxRange|64||Requires the ServersideQoL.ContainerSigns mod. <br>Max feeding range players can set per chest (by putting '&lt;FeedFromContainersRangeSignPrefix&gt;&lt;Range&gt;' on a chest sign)|
|FeedFromContainersLeaveAtLeast|1||Minimum amount of any given food item type to leave in a container|
|TameFedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a time was fed from a container|
