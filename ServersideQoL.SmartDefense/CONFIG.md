<details open><summary><b>SmartDefense</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|true|True/False|Enables/disables the entire mod|
</details>
<details open><summary><b>Traps</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|DisableTriggeredByPlayers|true|True/False|True to stop traps from being triggered by players|
|DisableFriendlyFire|true|True/False|True to stop traps from damaging players and tames. Does not work reliably (yet).|
|SelfDamageMultiplier|1|From 0 to Infinity|Multiply the damage the trap takes when it is triggered by this factor. 0 to make the trap take no damage|
|AutoRearm|true|True/False|True to automatically rearm traps when they are triggered|
</details>
<details open><summary><b>Turrets</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|DontTargetPlayers|true|True/False|True to stop ballistas from targeting players|
|DontTargetTames|true|True/False|True to stop ballistas from targeting tames|
|LoadFromContainers|true|True/False|True to automatically load ballistas from containers|
|LoadFromContainersRange|4||Required proximity of a container to a ballista to be used as ammo source|
|LoadFromContainersMinPlayerDistance|4||Min distance all players must have to a ballista|
|AmmoAddedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when ammo is added to a turret|
|NoAmmoMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when there is no ammo to add to a turret|
