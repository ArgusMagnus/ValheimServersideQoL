|Category|Key|Default Value|Acceptable Values|Description|
|--------|---|-------------|-----------------|-----------|
|General|Enabled|True|True/False|Enables/disables the entire mode|
|General|ConfigPerWorld|False|True/False|Use one config file per world. The file is saved next to the world file|
|General|InWorldConfigRoom|False|True/False|True to generate an in-world room which admins can enter to configure this mod by editing signs. A portal is placed at the start location|
|General|FarMessageRange|64||Max distance a player can have to a modified object to receive messages of type TopLeftFar or CenterFar|
|General|DiagnosticLogs|False|True/False|Enables/disables diagnostic logs|
|General|Frequency|5|From 0 to Infinity|How many times per second the mod processes the world|
|General|MaxProcessingTime|20||Max processing time (in ms) per update|
|General|ZonesAroundPlayers|1||Zones to process around each player|
|General|MinPlayerDistance|4||Min distance all players must have to a ZDO for it to be modified|
|General|IgnoreGameVersionCheck|True|True/False|True to ignore the game version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
|General|IgnoreNetworkVersionCheck|False|True/False|True to ignore the network version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
|General|IgnoreItemDataVersionCheck|False|True/False|True to ignore the item data version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
|General|IgnoreWorldVersionCheck|False|True/False|True to ignore the world version check. Turning this off may lead to the mod being run in an untested version and may lead to data loss/world corruption|
|Build Pieces|DisableRainDamage|False|True/False|True to prevent rain from damaging build pieces|
|Build Pieces|DisableSupportRequirements|None|None or combination of PlayerBuilt, World|Ignore support requirements on build pieces|
|Build Pieces|MakeIndestructible|False|True/False|True to make player-built pieces indestructible|
|Carts|ContentMassMultiplier|1|From 0 to Infinity|Multiplier for a carts content weight. E.g. set to 0 to ignore a cart's content weight|
|Containers|AutoSort|False|True/False|True to auto sort container inventories|
|Containers|SortedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a container was sorted|
|Containers|AutoPickup|False|True/False|True to automatically put dropped items into containers if they already contain said item|
|Containers|AutoPickupRange|64||Required proximity of a container to a dropped item to be considered as auto pickup target. Can be overriden per chest by putting '🧲<Range>' on a chest sign|
|Containers|AutoPickupMaxRange|64||Max auto pickup range players can set per chest (by putting '🧲<Range>' on a chest sign)|
|Containers|AutoPickupMinPlayerDistance|4||Min distance all player must have to a dropped item for it to be picked up|
|Containers|AutoPickupExcludeFodder|True|True/False|True to exclude food items for tames when tames are within search range|
|Containers|AutoPickupRequestOwnership|True|True/False|True to make the server request (and receive) ownership of dropped items from the clients before they are picked up. This will reduce the risk of data conflicts (e.g. item duplication) but will drastically decrease performance|
|Containers|PickedUpMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a dropped item is added to a container|
|Containers|ChestSignsDefaultText|•||Default text for chest signs|
|Containers|ChestSignsContentListMaxCount|3||Max number of entries to show in the content list on chest signs.|
|Containers|ChestSignsContentListPlaceholder|•||Bullet to use for content lists on chest signs|
|Containers|ChestSignsContentListSeparator|<br>||Separator to use for content lists on chest signs|
|Containers|ChestSignsContentListNameRest|Other||Text to show for the entry summarizing the rest of the items|
|Containers|ChestSignsContentListEntryFormat|{0} {1}|.NET Format strings for two arguments (String, Int32): https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-string-format#get-started-with-the-stringformat-method|Format string for entries in the content list, the first argument is the name of the item, the second is the total number of per item. The item names can be configured further by editing ChestSignItemNames.yml|
|Containers|WoodChestSigns|None|None or combination of Left, Right, Front, Back, TopLongitudinal, TopLateral|Options to automatically put signs on wood chests. Exact positions can be configured in ChestSignOffsets.yml|
|Containers|ReinforcedChestSigns|None|None or combination of Left, Right, Front, Back, TopLongitudinal, TopLateral|Options to automatically put signs on reinforced chests. Exact positions can be configured in ChestSignOffsets.yml|
|Containers|BlackmetalChestSigns|None|None or combination of Left, Right, Front, Back, TopLongitudinal, TopLateral|Options to automatically put signs on blackmetal chests. Exact positions can be configured in ChestSignOffsets.yml|
|Containers|ObliteratorSigns|None|None or combination of Front|Options to automatically put signs on obliterators. Exact positions can be configured in ChestSignOffsets.yml|
|Containers|ObliteratorItemTeleporter|Disabled|Disabled, Enabled, EnabledAllItems|Options to enable obliterators to teleport items instead of obliterating them when the lever is pulled. Requires 'ObliteratorSigns' and two obliterators with matching tags. The tag is set by putting '🔗<Tag>' on the sign|
|Containers|ObliteratorItemTeleporterMessageType|InWorld|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show for obliterator item teleporters|
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
|Crafting Stations|ArtisanstationBuildRange|40||Build range of Artisan Table|
|Crafting Stations|ArtisanstationExtraBuildRangePerLevel|0||Additional build range per level of Artisan Table|
|Crafting Stations|ArtisanstationMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Artisan Table to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Artisan Table to be able to place the extension.|
|Crafting Stations|BlackforgeBuildRange|20||Build range of Black Forge|
|Crafting Stations|BlackforgeExtraBuildRangePerLevel|0||Additional build range per level of Black Forge|
|Crafting Stations|BlackforgeMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Black Forge to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Black Forge to be able to place the extension.|
|Crafting Stations|CauldronMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Cauldron to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Cauldron to be able to place the extension.|
|Crafting Stations|ForgeBuildRange|20||Build range of Forge|
|Crafting Stations|ForgeExtraBuildRangePerLevel|3||Additional build range per level of Forge|
|Crafting Stations|ForgeMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Forge to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Forge to be able to place the extension.|
|Crafting Stations|MagetableBuildRange|20||Build range of Galdr Table|
|Crafting Stations|MagetableExtraBuildRangePerLevel|0||Additional build range per level of Galdr Table|
|Crafting Stations|MagetableMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Galdr Table to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Galdr Table to be able to place the extension.|
|Crafting Stations|StonecutterBuildRange|20||Build range of Stonecutter|
|Crafting Stations|WorkbenchBuildRange|20||Build range of Workbench|
|Crafting Stations|WorkbenchExtraBuildRangePerLevel|4||Additional build range per level of Workbench|
|Crafting Stations|WorkbenchMaxExtensionDistance|NaN||Max distance an extension can have to the corresponding Workbench to increase its level. NaN to use the games default range. Increasing this range will only increase the range for already built extensions, you may need to temporarily place additional Workbench to be able to place the extension.|
|Creatures|ShowHigherLevelStars|False|True/False|True to show stars for higher level creatures (> 2 stars). The intended use is with other mods, which spawn higher level creatures|
|Creatures|ShowHigherLevelAura|Never|Never or combination of Wild, Tamed|Show an aura for higher level creatures (> 2 stars)|
|Doors|AutoCloseMinPlayerDistance|NaN||Min distance all players must have to the door before it is closed. NaN to disable this feature|
|Fireplaces|MakeToggleable|False|True/False|True to make all fireplaces (including torches, braziers, etc.) toggleable|
|Fireplaces|InfiniteFuel|False|True/False|True to make all fireplaces have infinite fuel|
|Fireplaces|IgnoreRain|Never|Never, Always, InsideShield|Options to make all fireplaces ignore rain|
|Map Tables|AutoUpdatePortals|False|True/False|True to update map tables with portal pins|
|Map Tables|AutoUpdatePortalsExclude|||Portals with a tag that matches this filter are not added to map tables|
|Map Tables|AutoUpdatePortalsInclude|*||Only portals with a tag that matches this filter are added to map tables|
|Map Tables|AutoUpdateShips|False|True/False|True to update map tables with ship pins|
|Map Tables|UpdatedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a map table is updated|
|Plants|GrowTimeMultiplier|1|From 0 to Infinity|Multiply plant grow time by this factor. 0 to make them grow almost instantly.|
|Plants|SpaceRequirementMultiplier|1|From 0 to Infinity|Multiply plant space requirement by this factor. 0 to disable space requirements.|
|Plants|DontDestroyIfCantGrow|False|True/False|True to keep plants which can't grow alive|
|Players|InfiniteBuildingStamina|False|True/False|True to give players infinite stamina when building. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|Players|InfiniteFarmingStamina|False|True/False|True to give players infinite stamina when farming. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|Players|InfiniteMiningStamina|False|True/False|True to give players infinite stamina when mining. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|Players|InfiniteWoodCuttingStamina|False|True/False|True to give players infinite stamina when cutting wood. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|Players|InfiniteEncumberedStamina|False|True/False|True to give players infinite stamina when encumbered. If you want infinite stamina in general, set the global key 'StaminaRate' to 0|
|Players|StackInventoryIntoContainersEmote|-1|-1, -2, Wave, Sit, Challenge, Cheer, NoNoNo, ThumbsUp, Point, BlowKiss, Bow, Cower, Cry, Despair, Flex, ComeHere, Headbang, Kneel, Laugh, Roar, Shrug, Dance, Relax, Toast, Rest, Count|Emote to stack inventory into containers. -1 to disable this feature, -2 to use any emote as trigger|
|Players|CanSacrificeMegingjord|False|True/False|If true, players can permanently unlock increased carrying weight by sacrificing a megingjord in an obliterator|
|Players|CanSacrificeCryptKey|False|True/False|If true, players can permanently unlock the ability to open sunken crypt doors by sacrificing a crypt key in an obliterator|
|Players|CanSacrificeWishbone|False|True/False|If true, players can permanently unlock the ability to sense hidden objects by sacrificing a wishbone in an obliterator|
|Players|CanSacrificeTornSpirit|False|True/False|If true, players can permanently unlock a wisp companion by sacrificing a torn spirit in an obliterator. WARNING: Wisp companion cannot be unsummoned and will stay as long as this setting is enabled.|
|Portal Hub|Enable|False|True/False|True to automatically generate a portal hub|
|Portal Hub|Exclude|||Portals with a tag that matches this filter are not added to the portal hub|
|Portal Hub|Include|*||Only portals with a tag that matches this filter are added to the portal hub|
|Portal Hub|AutoNameNewPortals|False|True/False|True to automatically name new portals. Has no effect if 'Enable' is false|
|Portal Hub|AutoNameNewPortalsFormat|{0} {1:D2}|.NET Format strings for two arguments (String, Int32): https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-string-format#get-started-with-the-stringformat-method|Format string for autonaming portals, the first argument is the biome name, the second is an automatically incremented integer|
|Signs|DefaultColor|||Default color for signs. Can be a color name or hex code (e.g. #FF0000 for red)|
|Signs|TimeSigns|False|True/False|True to update sign texts which contain time emojis (any of 🕛🕧🕐🕜🕑🕝🕒🕞🕓🕟🕔🕠🕕🕡🕖🕢🕗🕣🕘🕤🕙🕥🕚🕦) with the in-game time|
|Smelters|FeedFromContainers|False|True/False|True to automatically feed smelters from nearby containers|
|Smelters|FeedFromContainersRange|4||Required proxmity of a container to a smelter to be used as feeding source. Can be overriden per chest by putting '↔️<Range>' on a chest sign|
|Smelters|FeedFromContainersMaxRange|64||Max feeding range players can set per chest (by putting '↔️<Range>' on a chest sign)|
|Smelters|FeedFromContainersLeaveAtLeastFuel|1||Minimum amout of fuel to leave in a container|
|Smelters|FeedFromContainersLeaveAtLeastOre|1||Minimum amout of ore to leave in a container|
|Smelters|OreOrFuelAddedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when ore or fuel is added to a smelter|
|Smelters|CapacityMultiplier|1||Multiply a smelter's ore/fuel capacity by this factor|
|Summons|TakeIntoDungeons|False|True/False|True to take your summons into (and out of) dungeons with you. This only affects summons that are friendly by default ('MakeFriendly' has on effect on this setting)|
|Summons|UnsummonDistanceMultiplier|1|From 0 to Infinity|Multiply unsummon distance by this factor. 0 to disable distance-based unsummoning|
|Summons|UnsummonLogoutTimeMultiplier|1|From 0 to Infinity|Multiply the time after which summons are unsummoned when the player logs out. 0 to disable logout-based unsummoning|
|Summons|MakeFriendly|False|True/False|True to make all summoned creatures (such as summoned trolls) friendly|
|Tames|MakeCommandable|False|True/False|True to make all tames commandable (like wolves)|
|Tames|TamingProgressMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of taming progress messages to show|
|Tames|GrowingProgressMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of growing progress messages to show|
|Tames|AlwaysFed|False|True/False|True to make tames always fed (not hungry)|
|Tames|TeleportFollow|False|True/False|True to teleport following tames to the players location if the player gets too far away from them|
|Traders|AlwaysUnlockBogWitchScytheHandle|False|True/False|Remove the progression requirements for buying Scythe Handle from |
|Traders|AlwaysUnlockBogWitchMushroomBzerker|False|True/False|Remove the progression requirements for buying Toadstool from |
|Traders|AlwaysUnlockBogWitchFragrantBundle|False|True/False|Remove the progression requirements for buying Fragrant Bundle from |
|Traders|AlwaysUnlockBogWitchSpiceForests|False|True/False|Remove the progression requirements for buying Woodland Herb Blend from |
|Traders|AlwaysUnlockBogWitchSpiceOceans|False|True/False|Remove the progression requirements for buying Seafarer's Herbs from |
|Traders|AlwaysUnlockBogWitchSpiceMountains|False|True/False|Remove the progression requirements for buying Mountain Peak Pepper Powder from |
|Traders|AlwaysUnlockBogWitchSpicePlains|False|True/False|Remove the progression requirements for buying Grasslands Herbalist Harvest from |
|Traders|AlwaysUnlockBogWitchSpiceMistlands|False|True/False|Remove the progression requirements for buying Herbs of the Hidden Hills from |
|Traders|AlwaysUnlockBogWitchSpiceAshlands|False|True/False|Remove the progression requirements for buying Fiery Spice Powder from |
|Traders|AlwaysUnlockHaldorYmirRemains|False|True/False|Remove the progression requirements for buying Ymir Flesh from Haldor|
|Traders|AlwaysUnlockHaldorThunderstone|False|True/False|Remove the progression requirements for buying Thunder Stone from Haldor|
|Traders|AlwaysUnlockHaldorChickenEgg|False|True/False|Remove the progression requirements for buying Egg from Haldor|
|Traders|AlwaysUnlockHildirArmorDress2|False|True/False|Remove the progression requirements for buying Brown Dress with Shawl from Hildir|
|Traders|AlwaysUnlockHildirArmorDress3|False|True/False|Remove the progression requirements for buying Brown Dress with Beads from Hildir|
|Traders|AlwaysUnlockHildirArmorDress5|False|True/False|Remove the progression requirements for buying Blue Dress with Shawl from Hildir|
|Traders|AlwaysUnlockHildirArmorDress6|False|True/False|Remove the progression requirements for buying Blue Dress with Beads from Hildir|
|Traders|AlwaysUnlockHildirArmorDress8|False|True/False|Remove the progression requirements for buying Yellow Dress with Shawl from Hildir|
|Traders|AlwaysUnlockHildirArmorDress9|False|True/False|Remove the progression requirements for buying Yellow Dress with Beads from Hildir|
|Traders|AlwaysUnlockHildirArmorTunic2|False|True/False|Remove the progression requirements for buying Blue Tunic with Cape from Hildir|
|Traders|AlwaysUnlockHildirArmorTunic3|False|True/False|Remove the progression requirements for buying Blue Tunic with Beads from Hildir|
|Traders|AlwaysUnlockHildirArmorTunic5|False|True/False|Remove the progression requirements for buying Red Tunic with Cape from Hildir|
|Traders|AlwaysUnlockHildirArmorTunic6|False|True/False|Remove the progression requirements for buying Red Tunic with Beads from Hildir|
|Traders|AlwaysUnlockHildirArmorTunic8|False|True/False|Remove the progression requirements for buying Yellow Tunic with Cape from Hildir|
|Traders|AlwaysUnlockHildirArmorTunic9|False|True/False|Remove the progression requirements for buying Yellow Tunic with Beads from Hildir|
|Traders|AlwaysUnlockHildirArmorDress1|False|True/False|Remove the progression requirements for buying Plain Brown Dress from Hildir|
|Traders|AlwaysUnlockHildirArmorDress4|False|True/False|Remove the progression requirements for buying Plain Blue Dress from Hildir|
|Traders|AlwaysUnlockHildirArmorDress7|False|True/False|Remove the progression requirements for buying Plain Yellow Dress from Hildir|
|Traders|AlwaysUnlockHildirArmorTunic1|False|True/False|Remove the progression requirements for buying Plain Blue Tunic from Hildir|
|Traders|AlwaysUnlockHildirArmorTunic4|False|True/False|Remove the progression requirements for buying Plain Red Tunic from Hildir|
|Traders|AlwaysUnlockHildirArmorTunic7|False|True/False|Remove the progression requirements for buying Plain Yellow Tunic from Hildir|
|Traders|AlwaysUnlockHildirArmorHarvester1|False|True/False|Remove the progression requirements for buying Harvest Tunic from Hildir|
|Traders|AlwaysUnlockHildirArmorHarvester2|False|True/False|Remove the progression requirements for buying Harvest Dress from Hildir|
|Traders|AlwaysUnlockHildirHelmetHat1|False|True/False|Remove the progression requirements for buying Blue Tied Headscarf from Hildir|
|Traders|AlwaysUnlockHildirHelmetHat2|False|True/False|Remove the progression requirements for buying Green Twisted Headscarf from Hildir|
|Traders|AlwaysUnlockHildirHelmetHat3|False|True/False|Remove the progression requirements for buying Brown Fur Cap from Hildir|
|Traders|AlwaysUnlockHildirHelmetHat4|False|True/False|Remove the progression requirements for buying Extravagant Green Cap from Hildir|
|Traders|AlwaysUnlockHildirHelmetHat6|False|True/False|Remove the progression requirements for buying Yellow Tied Headscarf from Hildir|
|Traders|AlwaysUnlockHildirHelmetHat7|False|True/False|Remove the progression requirements for buying Red Twisted Headscarf from Hildir|
|Traders|AlwaysUnlockHildirHelmetHat8|False|True/False|Remove the progression requirements for buying Grey Fur Cap from Hildir|
|Traders|AlwaysUnlockHildirHelmetHat9|False|True/False|Remove the progression requirements for buying Extravagant Orange Cap from Hildir|
|Traders|AlwaysUnlockHildirHelmetStrawHat|False|True/False|Remove the progression requirements for buying Straw Hat from Hildir|
|Traders|AlwaysUnlockHildirFireworksRocket_White|False|True/False|Remove the progression requirements for buying Basic Fireworks from Hildir|
|Traps|DisableTriggeredByPlayers|False|True/False|True to stop traps from being triggered by players|
|Traps|DisableFriendlyFire|False|True/False|True to stop traps from damaging players and tames|
|Traps|SelfDamageMultiplier|1|From 0 to Infinity|Multiply the damage the trap takes when it is triggered by this factor. 0 to make the trap take no damage|
|Traps|AutoRearm|False|True/False|True to automatically rearm traps when they are triggered|
|Trophy Spawner|Enable|False|True/False|True to make dropped trophies attract mobs|
|Trophy Spawner|ActivationDelay|3600||Time in seconds before trophies start attracting mobs|
|Trophy Spawner|RespawnDelay|12||Respawn delay in seconds|
|Trophy Spawner|MinSpawnDistance|181|From 0 to 181|Min distance from the trophy mobs can spawn|
|Trophy Spawner|MaxSpawnDistance|181|From 0 to 181|Max distance from the trophy mobs can spawn|
|Trophy Spawner|MaxLevel|3|From 1 to 9|Maximum level of spawned mobs|
|Trophy Spawner|LevelUpChanceOverride|-1|From -1 to 100|Level up chance override for spawned mobs. If < 0, world default is used|
|Trophy Spawner|SpawnLimit|20|From 1 to 10000|Maximum number of mobs of the trophy's type in the active area|
|Trophy Spawner|SuppressDrops|True|True/False|True to suppress drops from mobs spawned by trophies. Does not work reliably (yet)|
|Trophy Spawner|MessageType|InWorld|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a trophy is attracting mobs|
|Turrets|DontTargetPlayers|False|True/False|True to stop ballistas from targeting players|
|Turrets|DontTargetTames|False|True/False|True to stop ballistas from targeting tames|
|Turrets|LoadFromContainers|False|True/False|True to automatically load ballistas from containers|
|Turrets|LoadFromContainersRange|4||Required proxmity of a container to a ballista to be used as ammo source|
|Turrets|AmmoAddedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when ammo is added to a turret|
|Turrets|NoAmmoMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when there is no ammo to add to a turret|
|Windmills|IgnoreWind|False|True/False|True to make windmills ignore wind (Cover still decreases operating efficiency though)|
|World|AssignInteractableOwnershipToClosestPeer|False|True/False|True to assign ownership of some interactable objects (such as smelters or cooking stations) to the closest peer. This should help avoiding the loss of ore, etc. due to networking issues.|
|World|RemoveMistlandsMist|Never|Never, Always, AfterQueenKilled, InsideShield|Condition to remove the mist from the mistlands|
|World Modifiers|SetPresetFromConfig|False|True/False|True to set the world preset according to the 'Preset' config entry|
|World Modifiers|Preset|Default|Easy, Hard, Hardcore, Casual, Hammer, Immersive, Default|World preset. Enable 'SetPresetFromConfig' for this to have an effect|
|World Modifiers|SetModifiersFromConfig|False|True/False|True to set world modifiers according to the following configuration entries|
|World Modifiers|Combat|Default|VeryEasy, Easy, Default, Hard, VeryHard|World modifier 'Combat'. Enable 'SetModifiersFromConfig' for this to have an effect|
|World Modifiers|DeathPenalty|Default|Casual, VeryEasy, Easy, Default, Hard, Hardcore|World modifier 'DeathPenalty'. Enable 'SetModifiersFromConfig' for this to have an effect|
|World Modifiers|Resources|Default|MuchLess, Less, Default, More, MuchMore, Most|World modifier 'Resources'. Enable 'SetModifiersFromConfig' for this to have an effect|
|World Modifiers|Raids|Default|None, MuchLess, Less, Default, More, MuchMore|World modifier 'Raids'. Enable 'SetModifiersFromConfig' for this to have an effect|
|World Modifiers|Portals|Default|Casual, Default, Hard, VeryHard|World modifier 'Portals'. Enable 'SetModifiersFromConfig' for this to have an effect|
|Global Keys|SetGlobalKeysFromConfig|False|True/False|True to set global keys according to the following configuration entries|
|Global Keys|PlayerDamage|100||Sets the value for the 'PlayerDamage' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|EnemyDamage|100||Sets the value for the 'EnemyDamage' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|WorldLevel|0|From 0 to 10|Sets the value for the 'WorldLevel' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|EventRate|100||Sets the value for the 'EventRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|ResourceRate|100||Sets the value for the 'ResourceRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|StaminaRate|100||Sets the value for the 'StaminaRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|MoveStaminaRate|100||Sets the value for the 'MoveStaminaRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|StaminaRegenRate|100||Sets the value for the 'StaminaRegenRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|SkillGainRate|100||Sets the value for the 'SkillGainRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|SkillReductionRate|100||Sets the value for the 'SkillReductionRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|EnemySpeedSize|100||Sets the value for the 'EnemySpeedSize' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|EnemyLevelUpRate|100||Sets the value for the 'EnemyLevelUpRate' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|PlayerEvents|False|True/False|Sets the value for the 'PlayerEvents' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|Fire|False|True/False|Sets the value for the 'Fire' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|DeathKeepEquip|False|True/False|Sets the value for the 'DeathKeepEquip' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|DeathDeleteItems|False|True/False|Sets the value for the 'DeathDeleteItems' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|DeathDeleteUnequipped|False|True/False|Sets the value for the 'DeathDeleteUnequipped' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|DeathSkillsReset|False|True/False|Sets the value for the 'DeathSkillsReset' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|NoBuildCost|False|True/False|Sets the value for the 'NoBuildCost' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|NoCraftCost|False|True/False|Sets the value for the 'NoCraftCost' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|AllPiecesUnlocked|False|True/False|Sets the value for the 'AllPiecesUnlocked' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|NoWorkbench|False|True/False|Sets the value for the 'NoWorkbench' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|AllRecipesUnlocked|False|True/False|Sets the value for the 'AllRecipesUnlocked' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|WorldLevelLockedTools|False|True/False|Sets the value for the 'WorldLevelLockedTools' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|PassiveMobs|False|True/False|Sets the value for the 'PassiveMobs' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|NoMap|False|True/False|Sets the value for the 'NoMap' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|NoPortals|False|True/False|Sets the value for the 'NoPortals' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|NoBossPortals|False|True/False|Sets the value for the 'NoBossPortals' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|DungeonBuild|False|True/False|Sets the value for the 'DungeonBuild' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|TeleportAll|False|True/False|Sets the value for the 'TeleportAll' global key. Enable 'SetGlobalKeysFromConfig' for this to have an effect|
|Global Keys|NoPortalsPreventsContruction|True|True/False|True to change the effect of the 'NoPortals' global key, to prevent the construction of new portals but leave existing portals functional|
