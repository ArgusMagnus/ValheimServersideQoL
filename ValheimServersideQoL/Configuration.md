<details><summary>General</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|True|True/False|Enables/disables the entire mode|
|ConfigPerWorld|False|True/False|Use one config file per world. The file is saved next to the world file|
|InWorldConfigRoom|False|True/False|True to generate an in-world room which admins can enter to configure this mod by editing signs. A portal is placed at the start location|
|FarMessageRange|64||Max distance a player can have to a modified object to receive messages of type TopLeftFar or CenterFar|
|DiagnosticLogs|False|True/False|Enables/disables diagnostic logs|
|Frequency|5|From 0 to Infinity|How many times per second the mod processes the world|
|MaxProcessingTime|20||Max processing time (in ms) per update|
|ZonesAroundPlayers|1||Zones to process around each player|
|MinPlayerDistance|4||Min distance all players must have to a ZDO for it to be modified|
|IgnoreGameVersionCheck|True|True/False|True to ignore the game version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
|IgnoreNetworkVersionCheck|False|True/False|True to ignore the network version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
|IgnoreItemDataVersionCheck|False|True/False|True to ignore the item data version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
|IgnoreWorldVersionCheck|False|True/False|True to ignore the world version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
</details>
<details><summary>Build Pieces</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|DisableRainDamage|False|True/False|True to prevent rain from damaging build pieces|
|DisableSupportRequirements|None|None or combination of PlayerBuilt, World|Ignore support requirements on build pieces|
|MakeIndestructible|False|True/False|True to make player-built pieces indestructible|
</details>
<details><summary>Carts</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|ContentMassMultiplier|1|From 0 to Infinity|Multiplier for a carts content weight. E.g. set to 0 to ignore a cart's content weight|
</details>
<details><summary>Containers</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|AutoSort|False|True/False|True to auto sort container inventories|
|SortedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a container was sorted|
|AutoPickup|False|True/False|True to automatically put dropped items into containers if they already contain said item|
|AutoPickupRange|64||Required proximity of a container to a dropped item to be considered as auto pickup target. Can be overriden per chest by putting '🧲<Range>' on a chest sign|
|AutoPickupMaxRange|64||Max auto pickup range players can set per chest (by putting '🧲<Range>' on a chest sign)|
|AutoPickupMinPlayerDistance|4||Min distance all player must have to a dropped item for it to be picked up|
|AutoPickupExcludeFodder|True|True/False|True to exclude food items for tames when tames are within search range|
|AutoPickupRequestOwnership|True|True/False|True to make the server request (and receive) ownership of dropped items from the clients before they are picked up. This will reduce the risk of data conflicts (e.g. item duplication) but will drastically decrease performance|
|PickedUpMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a dropped item is added to a container|
|ChestSignsDefaultText|•||Default text for chest signs|
|ChestSignsContentListMaxCount|3||Max number of entries to show in the content list on chest signs.|
|ChestSignsContentListPlaceholder|•||Bullet to use for content lists on chest signs|
|ChestSignsContentListSeparator|<br>||Separator to use for content lists on chest signs|
|ChestSignsContentListNameRest|Other||Text to show for the entry summarizing the rest of the items|
|ChestSignsContentListEntryFormat|{0} {1}|.NET Format strings for two arguments (String, Int32): https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-string-format#get-started-with-the-stringformat-method|Format string for entries in the content list, the first argument is the name of the item, the second is the total number of per item. The item names can be configured further by editing ChestSignItemNames.yml|
|WoodChestSigns|None|None or combination of Left, Right, Front, Back, TopLongitudinal, TopLateral|Options to automatically put signs on wood chests|
|ReinforcedChestSigns|None|None or combination of Left, Right, Front, Back, TopLongitudinal, TopLateral|Options to automatically put signs on reinforced chests|
|BlackmetalChestSigns|None|None or combination of Left, Right, Front, Back, TopLongitudinal, TopLateral|Options to automatically put signs on blackmetal chests|
|ObliteratorSigns|None|None or combination of Front|Options to automatically put signs on obliterators|
|ObliteratorItemTeleporter|Disabled|Disabled, Enabled, EnabledAllItems|Options to enable obliterators to teleport items instead of obliterating them when the lever is pulled. Requires 'ObliteratorSigns' and two obliterators with matching tags. The tag is set by putting '🔗<Tag>' on the sign|
|ObliteratorItemTeleporterMessageType|InWorld|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show for obliterator item teleporters|
|InventorySize_Cart|6x3||Inventory size for 'Cart'|
|InventorySize_incinerator|7x3||Inventory size for 'Obliterator'|
|InventorySize_Karve|2x2||Inventory size for 'Karve'|
|InventorySize_piece_chest|6x4||Inventory size for 'Reinforced Chest'|
|InventorySize_piece_chest_barrel|6x2||Inventory size for 'Barrel'|
|InventorySize_piece_chest_blackmetal|8x4||Inventory size for 'Black Metal Chest'|
|InventorySize_piece_chest_private|3x2||Inventory size for 'Personal Chest'|
|InventorySize_piece_chest_wood|5x2||Inventory size for 'Chest'|
|InventorySize_piece_gift1|1x1||Inventory size for 'Yuleklapp'|
|InventorySize_piece_gift2|2x1||Inventory size for 'Yuleklapp'|
|InventorySize_piece_gift3|3x1||Inventory size for 'Yuleklapp'|
|InventorySize_piece_pot1|1x2||Inventory size for 'Medium Green Pot'|
|InventorySize_piece_pot2|1x3||Inventory size for 'Large Green Pot'|
|InventorySize_piece_pot3|1x1||Inventory size for 'Small Green Pot'|
|InventorySize_VikingShip|6x3||Inventory size for 'Longship'|
|InventorySize_VikingShip_Ashlands|8x4||Inventory size for 'Drakkar'|
</details>
<details><summary>Crafting Stations</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|ArtisanstationBuildRange|40||Build range of Artisan Table|
|ArtisanstationExtraBuildRangePerLevel|0||Additional build range per level of Artisan Table|
|ArtisanstationMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Artisan Table to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Artisan Table to be able to place the extension.|
|BlackforgeBuildRange|20||Build range of Black Forge|
|BlackforgeExtraBuildRangePerLevel|0||Additional build range per level of Black Forge|
|BlackforgeMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Black Forge to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Black Forge to be able to place the extension.|
|CauldronMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Cauldron to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Cauldron to be able to place the extension.|
|ForgeBuildRange|20||Build range of Forge|
|ForgeExtraBuildRangePerLevel|3||Additional build range per level of Forge|
|ForgeMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Forge to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Forge to be able to place the extension.|
|MagetableBuildRange|20||Build range of Galdr Table|
|MagetableExtraBuildRangePerLevel|0||Additional build range per level of Galdr Table|
|MagetableMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Galdr Table to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Galdr Table to be able to place the extension.|
|StonecutterBuildRange|20||Build range of Stonecutter|
|WorkbenchBuildRange|20||Build range of Workbench|
|WorkbenchExtraBuildRangePerLevel|4||Additional build range per level of Workbench|
|WorkbenchMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Workbench to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Workbench to be able to place the extension.|
</details>
<details><summary>Creatures</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|ShowHigherLevelStars|False|True/False|True to show stars for higher level creatures (> 2 stars)|
|ShowHigherLevelAura|Never|Never or combination of Wild, Tamed|Show an aura for higher level creatures (> 2 stars)|
|MaxLevelIncrease|0||Amount the max level of creatures is incremented throughout the world|
|MaxLevelIncreasePerDefeatedBoss|0||Amount the max level of creatures is incremented per defeated boss. The respective boss's biome and previous biomes are affected.|
|TreatOceanAs|BlackForest|None or combination of Meadows, Swamp, Mountain, BlackForest, Plains, AshLands, DeepNorth, Mistlands|Biome to treat the ocean as for the purpose of leveling up creatures|
|LevelUpBosses|False|True/False|True to also level up bosses|
|RespawnOneTimeSpawnsAfter|0||Time after one-time spawns are respawned in minutes|
</details>
<details><summary>Doors</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|AutoCloseMinPlayerDistance|NaN||Min distance all players must have to the door before it is closed. NaN to disable this feature|
</details>
<details><summary>Fireplaces</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|MakeToggleable|False|True/False|True to make all fireplaces (including torches, braziers, etc.) toggleable|
|InfiniteFuel|False|True/False|True to make all fireplaces have infinite fuel|
|IgnoreRain|Never|Never, Always, InsideShield|Options to make all fireplaces ignore rain|
</details>
<details><summary>Hostile Summons</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|AllowReplacementSummon|False|True/False|True to allow the summoning of new hostile summons (such as summoned trolls) to replace older ones when the limit exceeded|
|MakeFriendly|False|True/False|True to make all hostile summons (such as summoned trolls) friendly|
|FollowSummoner|False|True/False|True to make summoned creatures follow the summoner|
</details>
<details><summary>Map Tables</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|AutoUpdatePortals|False|True/False|True to update map tables with portal pins|
|AutoUpdatePortalsExclude|||Portals with a tag that matches this filter are not added to map tables|
|AutoUpdatePortalsInclude|*||Only portals with a tag that matches this filter are added to map tables|
|AutoUpdateShips|False|True/False|True to update map tables with ship pins|
|UpdatedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a map table is updated|
</details>
<details><summary>Non-teleportable Items</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enable|False|True/False|True to enable the non-teleportable items feature|
|PortalRange|4||When a player enters this range around a portal, non-teleportable items (for which you set boss keys below) might temporarily be taken from their inventory|
|MessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a non-teleportable item is taken from/returned to a player's inventory|
|BlackMetal|defeated_goblinking|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Black Metal' to be teleported when defeated|
|BlackMetalScrap|defeated_goblinking|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Black Metal Scrap' to be teleported when defeated|
|Bronze|defeated_gdking|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Bronze' to be teleported when defeated|
|BronzeScrap|defeated_gdking|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Scrap Bronze' to be teleported when defeated|
|CharredCogwheel|defeated_fader|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Charred Cogwheel' to be teleported when defeated|
|chest_hildir1||defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Hildir's Brass Chest' to be teleported when defeated|
|chest_hildir2||defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Hildir's Silver Chest' to be teleported when defeated|
|chest_hildir3||defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Hildir's Bronze Chest' to be teleported when defeated|
|Copper|defeated_gdking|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Copper' to be teleported when defeated|
|CopperOre|defeated_gdking|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Copper Ore' to be teleported when defeated|
|CopperScrap|defeated_gdking|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Copper Scrap' to be teleported when defeated|
|DragonEgg|defeated_dragon|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Dragon Egg' to be teleported when defeated|
|DvergrNeedle|defeated_queen|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Dvergr Extractor' to be teleported when defeated|
|Flametal|defeated_fader|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Ancient Metal' to be teleported when defeated|
|FlametalNew|defeated_fader|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Flametal' to be teleported when defeated|
|FlametalOre|defeated_fader|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Glowing Metal Ore' to be teleported when defeated|
|FlametalOreNew|defeated_fader|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Flametal Ore' to be teleported when defeated|
|Iron|defeated_bonemass|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Iron' to be teleported when defeated|
|IronOre|defeated_bonemass|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Iron Ore' to be teleported when defeated|
|Ironpit|defeated_bonemass|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Iron Pit' to be teleported when defeated|
|IronScrap|defeated_bonemass|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Scrap Iron' to be teleported when defeated|
|MechanicalSpring|defeated_queen|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Mechanical Spring' to be teleported when defeated|
|Silver|defeated_dragon|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Silver' to be teleported when defeated|
|SilverOre|defeated_dragon|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Silver Ore' to be teleported when defeated|
|Tin|defeated_gdking|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Tin' to be teleported when defeated|
|TinOre|defeated_gdking|defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader|Key of the boss that will allow 'Tin Ore' to be teleported when defeated|
</details>
<details><summary>Plants</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|GrowTimeMultiplier|1|From 0 to Infinity|Multiply plant grow time by this factor. 0 to make them grow almost instantly.|
|SpaceRequirementMultiplier|1|From 0 to Infinity|Multiply plant space requirement by this factor. 0 to disable space requirements.|
|DontDestroyIfCantGrow|False|True/False|True to keep plants which can't grow alive|
</details>
<details><summary>Players</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|InfiniteBuildingStamina|False|True/False|True to give players infinite stamina when building. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|InfiniteFarmingStamina|False|True/False|True to give players infinite stamina when farming. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|InfiniteMiningStamina|False|True/False|True to give players infinite stamina when mining. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|InfiniteWoodCuttingStamina|False|True/False|True to give players infinite stamina when cutting wood. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|InfiniteEncumberedStamina|False|True/False|True to give players infinite stamina when encumbered. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|StackInventoryIntoContainersEmote|-1|-1, -2, Wave, Sit, Challenge, Cheer, NoNoNo, ThumbsUp, Point, BlowKiss, Bow, Cower, Cry, Despair, Flex, ComeHere, Headbang, Kneel, Laugh, Roar, Shrug, Dance, Relax, Toast, Rest, Count|Emote to stack inventory into containers. -1 to disable this feature, -2 to use any emote as trigger|
|StackInventoryIntoContainersReturnDelay|1|From 1 to 10|Time in seconds after which items which could not be stacked into containers are returned to the player. Increasing this value can help with bad connections|
|CanSacrificeMegingjord|False|True/False|If true, players can permanently unlock increased carrying weight by sacrificing a megingjord in an obliterator|
|CanSacrificeCryptKey|False|True/False|If true, players can permanently unlock the ability to open sunken crypt doors by sacrificing a crypt key in an obliterator|
|CanSacrificeWishbone|False|True/False|If true, players can permanently unlock the ability to sense hidden objects by sacrificing a wishbone in an obliterator|
|CanSacrificeTornSpirit|False|True/False|If true, players can permanently unlock a wisp companion by sacrificing a torn spirit in an obliterator. WARNING: Wisp companion cannot be unsummoned and will stay as long as this setting is enabled.|
</details>
<details><summary>Portal Hub</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enable|False|True/False|True to automatically generate a portal hub|
|Exclude|||Portals with a tag that matches this filter are not added to the portal hub|
|Include|*||Only portals with a tag that matches this filter are added to the portal hub|
|AutoNameNewPortals|False|True/False|True to automatically name new portals. Has no effect if 'Enable' is false|
|AutoNameNewPortalsFormat|{0} {1:D2}|.NET Format strings for two arguments (String, Int32): https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-string-format#get-started-with-the-stringformat-method|Format string for autonaming portals, the first argument is the biome name, the second is an automatically incremented integer|
</details>
<details><summary>Signs</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|DefaultColor|||Default color for signs. Can be a color name or hex code (e.g. #FF0000 for red)|
|TimeSigns|False|True/False|True to update sign texts which contain time emojis (any of 🕛🕧🕐🕜🕑🕝🕒🕞🕓🕟🕔🕠🕕🕡🕖🕢🕗🕣🕘🕤🕙🕥🕚🕦) with the in-game time|
</details>
<details><summary>Sleeping</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|MinPlayersInBed|0||Minimum number of players in bed to show the sleep prompt to the other players. 0 to require all players to be in bed (default behavior)|
|RequiredPlayerPercentage|100|From 0 to 100|Percentage of players that must be in bed or sitting to skip the night|
|SleepPromptMessageType|Center|TopLeft, Center|Type of message to show for the sleep prompt|
</details>
<details><summary>Smelters</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|FeedFromContainers|False|True/False|True to automatically feed smelters from nearby containers|
|FeedFromContainersRange|4||Required proxmity of a container to a smelter to be used as feeding source. Can be overriden per chest by putting '↔️<Range>' on a chest sign|
|FeedFromContainersMaxRange|64||Max feeding range players can set per chest (by putting '↔️<Range>' on a chest sign)|
|FeedFromContainersLeaveAtLeastFuel|1||Minimum amout of fuel to leave in a container|
|FeedFromContainersLeaveAtLeastOre|1||Minimum amout of ore to leave in a container|
|OreOrFuelAddedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when ore or fuel is added to a smelter|
|CapacityMultiplier|1||Multiply a smelter's ore/fuel capacity by this factor|
|TimePerProductMultiplier|1||Multiply the time it takes to produce one product by this factor (will not go below 1 second per product).|
</details>
<details><summary>Summons</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|UnsummonDistanceMultiplier|1|From 0 to Infinity|Multiply unsummon distance by this factor. 0 to disable distance-based unsummoning|
|UnsummonLogoutTimeMultiplier|1|From 0 to Infinity|Multiply the time after which summons are unsummoned when the player logs out. 0 to disable logout-based unsummoning|
</details>
<details><summary>Tames</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|MakeCommandable|False|True/False|True to make all tames commandable (like wolves)|
|TamingProgressMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of taming progress messages to show|
|GrowingProgressMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of growing progress messages to show|
|AlwaysFed|False|True/False|True to make tames always fed (not hungry)|
|TeleportFollow|False|True/False|True to teleport following tames to the players location if the player gets too far away from them|
|TakeIntoDungeons|False|True/False|True to take following tames into (and out of) dungeons with you|
</details>
<details><summary>Traders</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|AlwaysUnlockBogWitchScytheHandle|False|True/False|Remove the progression requirements for buying Scythe Handle from |
|AlwaysUnlockBogWitchMushroomBzerker|False|True/False|Remove the progression requirements for buying Toadstool from |
|AlwaysUnlockBogWitchFragrantBundle|False|True/False|Remove the progression requirements for buying Fragrant Bundle from |
|AlwaysUnlockBogWitchSpiceForests|False|True/False|Remove the progression requirements for buying Woodland Herb Blend from |
|AlwaysUnlockBogWitchSpiceOceans|False|True/False|Remove the progression requirements for buying Seafarer's Herbs from |
|AlwaysUnlockBogWitchSpiceMountains|False|True/False|Remove the progression requirements for buying Mountain Peak Pepper Powder from |
|AlwaysUnlockBogWitchSpicePlains|False|True/False|Remove the progression requirements for buying Grasslands Herbalist Harvest from |
|AlwaysUnlockBogWitchSpiceMistlands|False|True/False|Remove the progression requirements for buying Herbs of the Hidden Hills from |
|AlwaysUnlockBogWitchSpiceAshlands|False|True/False|Remove the progression requirements for buying Fiery Spice Powder from |
|AlwaysUnlockHaldorYmirRemains|False|True/False|Remove the progression requirements for buying Ymir Flesh from Haldor|
|AlwaysUnlockHaldorThunderstone|False|True/False|Remove the progression requirements for buying Thunder Stone from Haldor|
|AlwaysUnlockHaldorChickenEgg|False|True/False|Remove the progression requirements for buying Egg from Haldor|
|AlwaysUnlockHildirArmorDress2|False|True/False|Remove the progression requirements for buying Brown Dress with Shawl from Hildir|
|AlwaysUnlockHildirArmorDress3|False|True/False|Remove the progression requirements for buying Brown Dress with Beads from Hildir|
|AlwaysUnlockHildirArmorDress5|False|True/False|Remove the progression requirements for buying Blue Dress with Shawl from Hildir|
|AlwaysUnlockHildirArmorDress6|False|True/False|Remove the progression requirements for buying Blue Dress with Beads from Hildir|
|AlwaysUnlockHildirArmorDress8|False|True/False|Remove the progression requirements for buying Yellow Dress with Shawl from Hildir|
|AlwaysUnlockHildirArmorDress9|False|True/False|Remove the progression requirements for buying Yellow Dress with Beads from Hildir|
|AlwaysUnlockHildirArmorTunic2|False|True/False|Remove the progression requirements for buying Blue Tunic with Cape from Hildir|
|AlwaysUnlockHildirArmorTunic3|False|True/False|Remove the progression requirements for buying Blue Tunic with Beads from Hildir|
|AlwaysUnlockHildirArmorTunic5|False|True/False|Remove the progression requirements for buying Red Tunic with Cape from Hildir|
|AlwaysUnlockHildirArmorTunic6|False|True/False|Remove the progression requirements for buying Red Tunic with Beads from Hildir|
|AlwaysUnlockHildirArmorTunic8|False|True/False|Remove the progression requirements for buying Yellow Tunic with Cape from Hildir|
|AlwaysUnlockHildirArmorTunic9|False|True/False|Remove the progression requirements for buying Yellow Tunic with Beads from Hildir|
|AlwaysUnlockHildirArmorDress1|False|True/False|Remove the progression requirements for buying Plain Brown Dress from Hildir|
|AlwaysUnlockHildirArmorDress4|False|True/False|Remove the progression requirements for buying Plain Blue Dress from Hildir|
|AlwaysUnlockHildirArmorDress7|False|True/False|Remove the progression requirements for buying Plain Yellow Dress from Hildir|
|AlwaysUnlockHildirArmorTunic1|False|True/False|Remove the progression requirements for buying Plain Blue Tunic from Hildir|
|AlwaysUnlockHildirArmorTunic4|False|True/False|Remove the progression requirements for buying Plain Red Tunic from Hildir|
|AlwaysUnlockHildirArmorTunic7|False|True/False|Remove the progression requirements for buying Plain Yellow Tunic from Hildir|
|AlwaysUnlockHildirArmorHarvester1|False|True/False|Remove the progression requirements for buying Harvest Tunic from Hildir|
|AlwaysUnlockHildirArmorHarvester2|False|True/False|Remove the progression requirements for buying Harvest Dress from Hildir|
|AlwaysUnlockHildirHelmetHat1|False|True/False|Remove the progression requirements for buying Blue Tied Headscarf from Hildir|
|AlwaysUnlockHildirHelmetHat2|False|True/False|Remove the progression requirements for buying Green Twisted Headscarf from Hildir|
|AlwaysUnlockHildirHelmetHat3|False|True/False|Remove the progression requirements for buying Brown Fur Cap from Hildir|
|AlwaysUnlockHildirHelmetHat4|False|True/False|Remove the progression requirements for buying Extravagant Green Cap from Hildir|
|AlwaysUnlockHildirHelmetHat6|False|True/False|Remove the progression requirements for buying Yellow Tied Headscarf from Hildir|
|AlwaysUnlockHildirHelmetHat7|False|True/False|Remove the progression requirements for buying Red Twisted Headscarf from Hildir|
|AlwaysUnlockHildirHelmetHat8|False|True/False|Remove the progression requirements for buying Grey Fur Cap from Hildir|
|AlwaysUnlockHildirHelmetHat9|False|True/False|Remove the progression requirements for buying Extravagant Orange Cap from Hildir|
|AlwaysUnlockHildirHelmetStrawHat|False|True/False|Remove the progression requirements for buying Straw Hat from Hildir|
|AlwaysUnlockHildirFireworksRocket_White|False|True/False|Remove the progression requirements for buying Basic Fireworks from Hildir|
</details>
<details><summary>Traps</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|DisableTriggeredByPlayers|False|True/False|True to stop traps from being triggered by players|
|DisableFriendlyFire|False|True/False|True to stop traps from damaging players and tames|
|SelfDamageMultiplier|1|From 0 to Infinity|Multiply the damage the trap takes when it is triggered by this factor. 0 to make the trap take no damage|
|AutoRearm|False|True/False|True to automatically rearm traps when they are triggered|
</details>
<details><summary>Trophy Spawner</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enable|False|True/False|True to make dropped trophies attract mobs. Does not work for passive mobs (such as deer or rabbits).|
|ActivationDelay|3600||Time in seconds before trophies start attracting mobs|
|RespawnDelay|12||Respawn delay in seconds|
|MinSpawnDistance|181|From 0 to 181|Min distance from the trophy mobs can spawn|
|MaxSpawnDistance|181|From 0 to 181|Max distance from the trophy mobs can spawn|
|MaxLevel|3|From 1 to 9|Maximum level of spawned mobs|
|LevelUpChanceOverride|-1|From -1 to 100|Level up chance override for spawned mobs. If < 0, world default is used|
|SpawnLimit|20|From 1 to 10000|Maximum number of mobs of the trophy's type in the active area|
|SuppressDrops|True|True/False|True to suppress drops from mobs spawned by trophies. Does not work reliably (yet)|
|MessageType|InWorld|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a trophy is attracting mobs|
</details>
<details><summary>Turrets</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|DontTargetPlayers|False|True/False|True to stop ballistas from targeting players|
|DontTargetTames|False|True/False|True to stop ballistas from targeting tames|
|LoadFromContainers|False|True/False|True to automatically load ballistas from containers|
|LoadFromContainersRange|4||Required proxmity of a container to a ballista to be used as ammo source|
|AmmoAddedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when ammo is added to a turret|
|NoAmmoMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when there is no ammo to add to a turret|
</details>
<details><summary>Windmills</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|IgnoreWind|False|True/False|True to make windmills ignore wind (Cover still decreases operating efficiency though)|
</details>
<details><summary>World</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|AssignInteractableOwnershipToClosestPeer|False|True/False|True to assign ownership of some interactable objects (such as smelters or cooking stations) to the closest peer. This should help avoiding the loss of ore, etc. due to networking issues.|
|RemoveMistlandsMist|Never|Never, Always, AfterQueenKilled, InsideShield|Condition to remove the mist from the mistlands|
</details>
<details><summary>World Modifiers</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|SetPresetFromConfig|False|True/False|True to set the world preset according to the 'Preset' config entry|
|Preset|Default|Easy, Hard, Hardcore, Casual, Hammer, Immersive, Default|World preset. Enable 'SetPresetFromConfig' for this to have an effect|
|SetModifiersFromConfig|False|True/False|True to set world modifiers according to the following configuration entries|
|Combat|Default|VeryEasy, Easy, Default, Hard, VeryHard|World modifier 'Combat'. Enable 'SetModifiersFromConfig' for this to have an effect|
|DeathPenalty|Default|Casual, VeryEasy, Easy, Default, Hard, Hardcore|World modifier 'DeathPenalty'. Enable 'SetModifiersFromConfig' for this to have an effect|
|Resources|Default|MuchLess, Less, Default, More, MuchMore, Most|World modifier 'Resources'. Enable 'SetModifiersFromConfig' for this to have an effect|
|Raids|Default|None, MuchLess, Less, Default, More, MuchMore|World modifier 'Raids'. Enable 'SetModifiersFromConfig' for this to have an effect|
|Portals|Default|Casual, Default, Hard, VeryHard|World modifier 'Portals'. Enable 'SetModifiersFromConfig' for this to have an effect|
</details>
<details><summary>Global Keys</summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|SetGlobalKeysFromConfig|False|True/False|True to set global keys according to the following configuration entries|
|PlayerDamage|100||Sets the value for the 'PlayerDamage' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|EnemyDamage|100||Sets the value for the 'EnemyDamage' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|WorldLevel|0|From 0 to 10|Sets the value for the 'WorldLevel' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|EventRate|100||Sets the value for the 'EventRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|ResourceRate|100||Sets the value for the 'ResourceRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|StaminaRate|100||Sets the value for the 'StaminaRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|MoveStaminaRate|100||Sets the value for the 'MoveStaminaRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|StaminaRegenRate|100||Sets the value for the 'StaminaRegenRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|SkillGainRate|100||Sets the value for the 'SkillGainRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|SkillReductionRate|100||Sets the value for the 'SkillReductionRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|EnemySpeedSize|100||Sets the value for the 'EnemySpeedSize' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|EnemyLevelUpRate|100||Sets the value for the 'EnemyLevelUpRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|PlayerEvents|False|True/False|Sets the value for the 'PlayerEvents' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Fire|False|True/False|Sets the value for the 'Fire' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|DeathKeepEquip|False|True/False|Sets the value for the 'DeathKeepEquip' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|DeathDeleteItems|False|True/False|Sets the value for the 'DeathDeleteItems' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|DeathDeleteUnequipped|False|True/False|Sets the value for the 'DeathDeleteUnequipped' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|DeathSkillsReset|False|True/False|Sets the value for the 'DeathSkillsReset' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|NoBuildCost|False|True/False|Sets the value for the 'NoBuildCost' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|NoCraftCost|False|True/False|Sets the value for the 'NoCraftCost' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|AllPiecesUnlocked|False|True/False|Sets the value for the 'AllPiecesUnlocked' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|NoWorkbench|False|True/False|Sets the value for the 'NoWorkbench' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|AllRecipesUnlocked|False|True/False|Sets the value for the 'AllRecipesUnlocked' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|WorldLevelLockedTools|False|True/False|Sets the value for the 'WorldLevelLockedTools' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|PassiveMobs|False|True/False|Sets the value for the 'PassiveMobs' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|NoMap|False|True/False|Sets the value for the 'NoMap' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|NoPortals|False|True/False|Sets the value for the 'NoPortals' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|NoBossPortals|False|True/False|Sets the value for the 'NoBossPortals' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|DungeonBuild|False|True/False|Sets the value for the 'DungeonBuild' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|TeleportAll|False|True/False|Sets the value for the 'TeleportAll' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|NoPortalsPreventsContruction|True|True/False|True to change the effect of the 'NoPortals' global key, to prevent the construction of new portals but leave existing portals functional|
