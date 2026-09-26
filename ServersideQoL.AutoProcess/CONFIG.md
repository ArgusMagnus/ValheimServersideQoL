<details open><summary><b>AutoProcess</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|True|True/False|Enables/disables the entire mod|
|FeedFromContainers|True|True/False|True to automatically feed smelters from nearby containers|
|FeedFermenters|False|True/False|True to automatically add fermentable items (e.g. mead bases) to empty fermenters from nearby containers. Requires FeedFromContainers|
|TapFermenters|False|True/False|True to automatically tap fermenters when the content is ready. The products (e.g. mead) are dropped like when tapping by hand, use AutoStore to put them into containers|
|FeedFromContainersRange|4||Required proximity of a container to a smelter to be used as feeding source. <br>Can be overridden per chest by putting '&lt;FeedFromContainersRangeSignPrefix&gt;&lt;Range&gt;' on a chest sign, e.g. '↔️64'. <br>  For example, '↔️64' increase the range of that chest to 64m. <br>  Only works with automatic chest signs added by the ServersideQoL.ContainerSigns mod.|
|FeedFromContainersRangeSignPrefix|↔️||Requires the ServersideQoL.ContainerSigns mod. <br>The prefix used to identify the container specifc feed range value in chest sign text.|
|FeedFromContainersMaxRange|64||Requires the ServersideQoL.ContainerSigns mod. <br>Max feeding range players can set per chest (by putting '&lt;FeedFromContainersRangeSignPrefix&gt;&lt;Range&gt;' on a chest sign)|
|FeedFromContainersMinPlayerDistance|4||Min distance all players must have to a processing station|
|FeedFromContainersLeaveAtLeastFuel|1||Minimum amount of fuel to leave in a container|
|FeedFromContainersLeaveAtLeastOre|1||Minimum amount of ore to leave in a container|
|FeedFromContainersLeaveAtLeastFermentable|0||Minimum amount of fermentable items (e.g. mead bases) to leave in a container|
|OreOrFuelAddedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when ore or fuel is added to a smelter|
|CapacityMultiplier|1||Multiply a smelter's ore/fuel capacity by this factor|
|TimePerProductMultiplier|1||Multiply the time it takes to produce one product by this factor (will not go below 1 second per product).|
