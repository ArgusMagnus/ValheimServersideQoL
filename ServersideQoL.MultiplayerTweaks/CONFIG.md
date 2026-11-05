<details open><summary><b>MultiplayerTweaks</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|true|True/False|Enables/disables the entire mod|
|ForcePlayerMapPin|false|True/False|True to force player map pins to be visible for all players|
|AssignInteractablesToClosestPlayer|false|True/False|True to assign ownership of some interactable objects (such as smelters or cooking stations) to the closest player. <br>This should help avoiding the loss of ore, etc. due to networking issues.|
|AssignMobsToClosestPlayer|false|True/False|True to assign ownership of hostile mobs to the closest player. <br>This should help reduce issues with dodging/parrying due to networking issues.|
|AssignShipsToCaptain|false|True/False|True to assign ownership of ships to the player controlling the ship. <br>This should help reduce issues with ship control due to networking issues.|
|MinOwnershipDurationSeconds|2|From 2 to 60|The minimum time (seconds) after ownership was reassigned on an object before it can be reassigned again|
|MaxTimeSinceLastPingSeconds|1.5|From 1 to 5|The maximum time (seconds) since the last ping from a player was received for that player to be considered as potential new owner. <br>This is not the max ping. Even on a perfect connection this value can be up to one second, because vanilla clients only send pings once every second.|
</details>
<details open><summary><b>PlayerSave</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|SaveBeforeSleep|false|True/False|True to instruct clients to save before sleeping|
