|Category|Key|Default Value|Acceptable Values|Description|
|---|---|---|---|---|
|General|Enabled|True|True/False|Enables/disables the entire mode|
|General|DiagnosticLogs|False|True/False|Enables/disables diagnostic logs|
|General|StartDelay|0||Time (in seconds) before the mod starts processing the world|
|General|Frequency|5||How many times per second the mod processes the world|
|General|MaxProcessingTime|20||Max processing time (in ms) per update|
|General|ZonesAroundPlayers|1||Zones to process around each player|
|General|MinPlayerDistance|4||Min distance all players must have to a ZDO for it to be modified|
|General|IgnoreGameVersionCheck|True|True/False|True to ignore the game version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
|General|IgnoreNetworkVersionCheck|False|True/False|True to ignore the network version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
|General|IgnoreItemDataVersionCheck|False|True/False|True to ignore the item data version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
|General|IgnoreWorldVersionCheck|False|True/False|True to ignore the world version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
|Build Pieces|DisableRainDamage|False|True/False|True to prevent rain from damaging build pieces|
|Build Pieces|DisableSupportRequirements|None|Combination of None, PlayerBuilt, World|Ignore support requirements on build pieces|
|Carts|ContentMassMultiplier|NaN||Multiplier for a carts content weight. E.g. set to 0 to ignore a cart's content weight|
|Containers|AutoSort|False|True/False|True to auto sort container inventories|
|Containers|AutoPickup|False|True/False|True to automatically put dropped items into containers if they already contain said item|
|Containers|AutoPickupRange|64||Required proximity of a container to a dropped item to be considered as auto pickup target|
|Containers|AutoPickupMinPlayerDistance|8||Min distance all player must have to a dropped item for it to be picked up|
|Containers|InventorySize_Cart|6x3||Inventory size for 'Cart'|
|Containers|InventorySize_incinerator|7x3||Inventory size for 'Obliterator'|
|Containers|InventorySize_Karve|2x2||Inventory size for 'Karve'|
|Containers|InventorySize_piece_chest|6x4||Inventory size for 'Reinforced Chest'|
|Containers|InventorySize_piece_chest_barrel|6x2||Inventory size for 'Barrel'|
|Containers|InventorySize_piece_chest_blackmetal|8x4||Inventory size for 'Black Metal Chest'|
|Containers|InventorySize_piece_chest_private|3x2||Inventory size for 'Personal Chest'|
|Containers|InventorySize_piece_chest_wood|5x2||Inventory size for 'Chest'|
|Containers|InventorySize_piece_gift1|1x1||Inventory size for 'Yuleklapp'|
|Containers|InventorySize_piece_gift2|2x1||Inventory size for 'Yuleklapp'|
|Containers|InventorySize_piece_gift3|3x1||Inventory size for 'Yuleklapp'|
|Containers|InventorySize_piece_pot1|1x2||Inventory size for 'Medium Green Pot'|
|Containers|InventorySize_piece_pot2|1x3||Inventory size for 'Large Green Pot'|
|Containers|InventorySize_piece_pot3|1x1||Inventory size for 'Small Green Pot'|
|Containers|InventorySize_VikingShip|6x3||Inventory size for 'Longship'|
|Containers|InventorySize_VikingShip_Ashlands|8x4||Inventory size for 'Drakkar'|
|Doors|AutoCloseMinPlayerDistance|NaN||Min distance all players must have to the door before it is closed. NaN to disable this feature|
|Fireplaces|MakeToggleable|False|True/False|True to make all fireplaces (including torches, braziers, etc.) toggleable|
|Fireplaces|InfiniteFuel|False|True/False|True to make all fireplaces have infinite fuel|
|Global Keys|NoPortalsPreventsContruction|True|True/False|True to change the effect of the 'NoPortals' global key, to prevent the construction of new portals but leave existing portals functional|
|Global Keys|PlayerDamage|100||Sets the value for the 'PlayerDamage' global key|
|Global Keys|EnemyDamage|100||Sets the value for the 'EnemyDamage' global key|
|Global Keys|WorldLevel|0|From 0 to 10|Sets the value for the 'WorldLevel' global key|
|Global Keys|EventRate|100||Sets the value for the 'EventRate' global key|
|Global Keys|ResourceRate|100||Sets the value for the 'ResourceRate' global key|
|Global Keys|StaminaRate|100||Sets the value for the 'StaminaRate' global key|
|Global Keys|MoveStaminaRate|100||Sets the value for the 'MoveStaminaRate' global key|
|Global Keys|StaminaRegenRate|100||Sets the value for the 'StaminaRegenRate' global key|
|Global Keys|SkillGainRate|100||Sets the value for the 'SkillGainRate' global key|
|Global Keys|SkillReductionRate|100||Sets the value for the 'SkillReductionRate' global key|
|Global Keys|EnemySpeedSize|100||Sets the value for the 'EnemySpeedSize' global key|
|Global Keys|EnemyLevelUpRate|100||Sets the value for the 'EnemyLevelUpRate' global key|
|Global Keys|PlayerEvents|False|True/False|True to set the 'PlayerEvents' global key|
|Global Keys|Fire|False|True/False|True to set the 'Fire' global key|
|Global Keys|DeathKeepEquip|False|True/False|True to set the 'DeathKeepEquip' global key|
|Global Keys|DeathDeleteItems|False|True/False|True to set the 'DeathDeleteItems' global key|
|Global Keys|DeathDeleteUnequipped|False|True/False|True to set the 'DeathDeleteUnequipped' global key|
|Global Keys|DeathSkillsReset|False|True/False|True to set the 'DeathSkillsReset' global key|
|Global Keys|NoBuildCost|False|True/False|True to set the 'NoBuildCost' global key|
|Global Keys|NoCraftCost|False|True/False|True to set the 'NoCraftCost' global key|
|Global Keys|AllPiecesUnlocked|False|True/False|True to set the 'AllPiecesUnlocked' global key|
|Global Keys|NoWorkbench|False|True/False|True to set the 'NoWorkbench' global key|
|Global Keys|AllRecipesUnlocked|False|True/False|True to set the 'AllRecipesUnlocked' global key|
|Global Keys|WorldLevelLockedTools|False|True/False|True to set the 'WorldLevelLockedTools' global key|
|Global Keys|PassiveMobs|False|True/False|True to set the 'PassiveMobs' global key|
|Global Keys|NoMap|False|True/False|True to set the 'NoMap' global key|
|Global Keys|NoPortals|False|True/False|True to set the 'NoPortals' global key|
|Global Keys|NoBossPortals|False|True/False|True to set the 'NoBossPortals' global key|
|Global Keys|DungeonBuild|False|True/False|True to set the 'DungeonBuild' global key|
|Global Keys|TeleportAll|False|True/False|True to set the 'TeleportAll' global key|
|Global Keys|Preset||, Easy, Hard, Hardcore, Casual, Hammer, Immersive, Default|World preset|
|Global Keys|Combat||, VeryEasy, Easy, Default, Hard, VeryHard|World modifier 'Combat'|
|Global Keys|DeathPenalty||, Casual, VeryEasy, Easy, Default, Hard, Hardcore|World modifier 'DeathPenalty'|
|Global Keys|Resources||, MuchLess, Less, Default, More, MuchMore, Most|World modifier 'Resources'|
|Global Keys|Raids||, None, MuchLess, Less, Default, More, MuchMore|World modifier 'Raids'|
|Global Keys|Portals||, Casual, Default, Hard, VeryHard|World modifier 'Portals'|
|Map Tables|AutoUpdatePortals|False|True/False|True to update map tables with portal pins|
|Map Tables|AutoUpdatePortalsExclude|||Portals with a tag that matches this filter are not added to map tables|
|Map Tables|AutoUpdatePortalsInclude|*||Only portals with a tag that matches this filter are added to map tables|
|Map Tables|AutoUpdateShips|False|True/False|True to update map tables with ship pins|
|Players|InfiniteBuildingStamina|False|True/False|True to give players infinite stamina when building. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|Players|InfiniteFarmingStamina|False|True/False|True to give players infinite stamina when farming. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|Signs|TimeSigns|False|True/False|True to update sign texts which contain time emojis (any of 🕛🕧🕐🕜🕑🕝🕒🕞🕓🕟🕔🕠🕕🕡🕖🕢🕗🕣🕘🕤🕙🕥🕚🕦) with the in-game time|
|Smelters|FeedFromContainers|False|True/False|True to automatically feed smelters from nearby containers|
|Smelters|FeedFromContainersRange|4||Required proxmity of a container to a smelter to be used as feeding source|
|Smelters|FeedFromContainersLeaveAtLeastFuel|1||Minimum amout of fuel to leave in a container|
|Smelters|FeedFromContainersLeaveAtLeastOre|1||Minimum amout of ore to leave in a container|
|Tames|MakeCommandable|False|True/False|True to make all tames commandable (like wolves)|
|Tames|SendTamingPogressMessages|False|True/False|True to send taming progress messages to nearby players|
|Tames|AlwaysFed|False|True/False|True to make tames always fed (not hungry)|
|Tames|TeleportFollow|False|True/False|True to teleport following tames to the players location if the player gets too far away from them|
|Turrets|DontTargetPlayers|False|True/False|True to stop ballistas from targeting players|
|Turrets|DontTargetTames|False|True/False|True to stop ballistas from targeting tames|
|Turrets|LoadFromContainers|False|True/False|True to automatically load ballistas from containers|
|Turrets|LoadFromContainersRange|4||Required proxmity of a container to a ballista to be used as ammo source|
|Windmills|IgnoreWind|False|True/False|True to make windmills ignore wind (Cover still decreases operating efficiency though)|
