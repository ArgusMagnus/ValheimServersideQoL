<details open><summary><b>AutoStore</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|true|True/False|Enables/disables the entire mod|
|AutoSort|true|True/False|True to auto sort container inventories|
|SortedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a container was sorted|
|AutoPickup|true|True/False|True to automatically put dropped items into containers if they already contain said item|
|AutoPickupRange|64||Required proximity of a container to a dropped item to be considered as auto pickup target. <br>Can be overridden per chest by putting '&lt;AutoPickupRangeSignPrefix&gt;&lt;Range&gt;' on a chest sign, e.g. '🧲16'. <br>  For example, '🧲0' will disable auto pickup/stacking from player inventory into that chest. <br>  Only works with automatic chest signs added by the ServersideQoL.ContainerSigns mod.|
|AutoPickupRangeSignPrefix|🧲||Requires the ServersideQoL.ContainerSigns mod. <br>The prefix used to identify the container specifc auto pickup range value in chest sign text.|
|AutoPickupMaxRange|64||Requires the ServersideQoL.ContainerSigns mod.  <br>Max auto pickup range players can set per chest (by putting '&lt;AutoPickupRangeSignPrefix&gt;&lt;Range&gt;' on a chest sign).|
|AutoPickupMinPlayerDistance|4||Min distance all players must have to a dropped item for it to be picked up|
|AutoPickupExcludeItemTypes||Ammo, AmmoNonEquipable, Bow, Chest, Consumable, Fish, Helmet, Legs, Material, Misc, OneHandedWeapon, Shield, Shoulder, Tool, Torch, Trinket, Trophy, TwoHandedWeapon, TwoHandedWeaponLeft, Utility|Item types which will be excluded from auto pickup|
|AutoPickupExcludeFodder|true|True/False|True to exclude food items for tames when tames are within search range|
|AutoPickupRequestOwnership|true|True/False|True to make the server request (and receive) ownership of dropped items from the clients before they are picked up. This will reduce the risk of data conflicts (e.g. item duplication) but will drastically decrease performance|
|PickedUpMessageType|InWorld|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a dropped item is added to a container|
|ShowContainerModifiedEffect|true|True/False|True to show an effect when a container is modified due to auto pickup or player inventory stacking|
|SuppressContainerModifiedEffectSound|false|True/False|True to suppress the sound of the container modified effect when ShowContainerModifiedEffect = true|
|StackInventoryIntoContainersEmote|Sit|-1, -2, Wave, Sit, Challenge, Cheer, NoNoNo, ThumbsUp, Point, BlowKiss, Bow, Cower, Cry, Despair, Flex, ComeHere, Headbang, Kneel, Laugh, Roar, Shrug, Dance, Relax, Toast, Rest, Vibe, LoveYou, Count|Emote to stack inventory into containers. <br>If a player uses this emote, their inventory will be automatically stacked into nearby containers. <br>The rules for which containers are used are the same as for auto pickup. <br>-1 to disable this feature, -2 to use any emote as trigger. <br>For example, on xbox you can use D-Pad down to execute the Sit emote. <br>If you use emotes exclusively for this feature, it is recommended to set the value to -2 as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.|
|StackInventoryIntoContainersExcludeItemTypes||Ammo, AmmoNonEquipable, Bow, Chest, Consumable, Fish, Helmet, Legs, Material, Misc, OneHandedWeapon, Shield, Shoulder, Tool, Torch, Trinket, Trophy, TwoHandedWeapon, TwoHandedWeaponLeft, Utility|Item types which will be excluded from stacking into containers via emote|
|StackInventoryIntoContainersReturnDelay|1|From 1 to 10|Time in seconds after which items which could not be stacked into containers are returned to the player. <br>Increasing this value can help with bad connections.|
