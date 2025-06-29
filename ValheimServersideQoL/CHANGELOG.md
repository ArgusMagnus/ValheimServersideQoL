### v1.1.0 (BETA 1)
- New features:
    - Add config option to limit max container pickup range configured by chest sign [#76](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/76)
    - Take tamed creatures into dungeons [#95](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/95)
    - Make all hostile summoned creatures friendly [#92](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/92)
    - Unsummon the oldest troll when hitting the limit with Trollstav (behaves more like Dead Raiser) [#96](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/96)
    - Make hostile summons (like summoned troll) follow the summoner [#97](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/97)

### v1.0.0
- Portal Hub: Distinguish between portals that are connected to regular portals and those that are connected to stone portals
- Fixed: Chest signs disappear after config changes [#83](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/83)
- Fixed: Dropped food items freezing in the air [#75](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/75)
- Fixed some issues with unlinked portals in the portal hub [#81](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/81) [#84](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/84) [#86](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/86)
- Fixed: Obliterator teleporters are unlinked until their respective zones have been visited by players [#85](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/85)

### v0.2.27
- New features:
    - Modify range and max extension distance of crafting stations [#14](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/14)
    - Show content on chest signs [#68](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/68)
    - Assign ownership of some interactable objects (smelters, cooking stations, etc.) to the nearest player to help avoid the loss of ore/etc. on bad connections/crossplay
- New chest sign options: `TopLongitudinal` and `TopLateral` to show the chest content on the top of the chest [#74](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/74)
- Trophy spawner: make spawn distance to trophy configurable [#73](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/73)
- Fixed: Tags disappear when edited in In-world room [#71](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/71)

### v0.2.26
- Fixed eggs not being picked up by container auto-pickup [#67](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/67)
- Fixed general issues with containers and auto-pickup
- Fixed issue with stacking from player inventories (via emote) which could lead to items being lost
- New features:
    - Set default text color for signs
    - Auto rearm traps after they are triggered

### v0.2.25
- New feature:
    - Players can permanently gain increased carrying weight by sacrificing a megingjord in an obliterator
    - Players can permanently gain the ability to open sunken crypt doors by sacrificing a crypt key in an obliterator
    - Players can permanently gain the ability to sense hidden objects by sacrificing a wishbone in an obliterator
    - Players can permanently gain a wisp companion by sacrificing a torn spirit in an obliterator
- Removed feature which allowed players to open sunken crypt doors after defeating the elder
- Slight adjustments to obliterator item teleporter
- Fixed issue with dissappearing chest/obliterator signs

### v0.2.24
- Obliterator item teleporter: separate options for teleporting all items and just teleportable items (according to world settings)

### v0.2.23
- New features:
    - Stack items from player inventories into containers (config: `StackInventoryIntoContainersEmote`) [#44](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/44)
    - Infinite stamina when encumbered
    - Make player-built pieces indestructible [#59](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/59)
    - Use obliterator to teleport items
- Portal hub: visualize portal ID with colored torches to facilitate portal identication even if the position in the hub changes
- Types of shown messages (e.g. container sorted, taming progress, etc.) are now configurable [#18](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/18)

### v0.2.22
- New features:
    - Modify the ore/fuel capacity of smelters
    - Allow unlocking of crypt doors without the key if the Elder has been defeated [#50](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/50)
- Chest auto pickup/smelter feeding range is now configurable via chest sign text
- Changed trophy spawners to spawn creatures farther away
- Fix for issues with setting global keys via config [#54](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/54)

### v0.2.21
- Fix for [#53](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/53): Unable to use tools/weapons

### v0.2.20
- New features:
    - Infinite mining/wood cutting stamina
    - Support for one config file per world [#49](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/49)
- Changed infinite farming/building/mining/wood cutting stamina to only restore the necessary stamina for the action
- Potential fix for Mod sometimes stops working after a player disconnects [#45](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/45)
- Fixed Auto close only player-built doors [#48](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/48)

### v0.2.19
- New features:
    - Automatically put signs on chests
    - Show stars for higher level creatures (> 2 stars)
    - Show aura for higher level creatures (> 2 stars)
    - New config options for trophy spawners (MaxLevel/LevelUpChance)
- Fix NullReferenceException After Logging Out and Back In Without Quitting Game [#40](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/40)
- Improve performance

### v0.2.18
- Fix mod not working correctly on non-dedicated servers [#38](https://github.com/ArgusMagnus/ValheimServersideQoL/discussions/38)

### v0.2.17
- Performance improvements

### v0.2.16
- New features:
    - Remove mist from Mistlands
    - Auto name portals when automatically generated portal hub is enabled
    - Spawn creatures from dropped trophies

### v0.2.15
- Fix bug introduced in v0.2.14 which lead to many features not working correctly
- When fireplaces are configured to ignore rain, also protect against strong wind
- Use sconces instead of candles in the config room, because candles get turned off by rain when teleporting in [#35](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/35)

### v0.2.14
- New config option: AutoPickupExcludeFodder: exclude food items for tames from auto-pickup if tames are within search range (defaults to true)
- Fix Auto-Collection Stops After Instant Resource Consumption [#32](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/32)
- Tweaks and bug fixes

### v0.2.13
- Fix bug with generated portal hub

### v0.2.12
- New feature: Automatically generated portal hub
- Fix NoPortalsPreventsContruction not working [#29](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/29)

### v0.2.11
- New feature: Make fireplaces ignore rain

### v0.2.10
- Fix for config room portals being added as automatic map table pins [#25](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/25)
- Tweaks to in-world config room: Config values which have a defined set of acceptable values can now be changed by toggling candles

### v0.2.9
- Fix config initialization issue [#22](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/22)

### v0.2.8
- Fix initialization issue [#21](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/21)
- Tweaks to in-world config room

### v0.2.7
- **BREAKING CHANGE**: split `Global Keys` config section into 2 separate sections for world preset/modifiers and global keys
- New feature: in-world config room: A generated room that allows admins to make changes to this mods config by editing signs
- Fix for Exception on load [#19](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/19)

### v0.2.6
- Fix for `NotSupportedException` on load [#19](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/19)

### v0.2.5
- New features:
    - Modify plant grow time and space requirement, option to keep plants which can't grow alive
    - Modify unsummon distance/time of summons (Skeletons)
    - Show growing progress for tames' offspring/eggs
    - Include shield generators when auto-feeding smelters
    - Prevent traps from being triggered by players, friendly fire and damaging themselves
- Changed taming progress message to be displayed in world
- Fix:
    - Exclude personal chests from auto-pickup/auto-feeding smelters, etc.
    - Don't auto-pickup growing eggs

### v0.2.4
- New feature: always unlock trader items (remove progression requirements for buying from trader items). Only supported on dedicated servers.
- Changed container sorting algorithm to first try filling whole rows/columns with one item type

### v0.2.3
- Fix boats getting destroyed [#8](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/8)
- Fix for infinite stamina related issues [#4](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/4)

### v0.2.2
- Change default config: disable all features by default [#7](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/7)
- Potential fix for infinite stamina related issues [#4](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/4)
- Fix certain build pieces getting duplicated [#9](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/9)

### v0.2.1
- New icon

### v0.2.0
- Config for enabling/disabling logs. Disable logs by default

### v0.1.9
- New features:
    - Build pieces: disable rain damage
    - Build pieces: disable support/stability requirements
- Fix doors auto-closing in players faces
- Various bugfixes / performance tweaks

### v0.1.8
- New features:
    - Stop ballistas from targeting players/tames
    - Auto-load ballistas from nearby containers
- Various bugfixes / performance tweaks

### v0.1.7
- New features:
    - Give players infinite stamina when building/farming
- Various bugfixes / performance tweaks

### v0.1.6
- Update to patch 0.220.4
- New features:
    - Configure container inventory sizes
- Various bugfixes / performance tweaks

### v0.1.5
- Various bugfixes / performance tweaks

### v0.1.4
- Auto-sorting containers: merge partial stacks
- New features:
    - Make tames always fed
    - Teleport following tames to player
    - Change behavior of NoPortals global key
    - Item weight multiplier for carts
    - Auto-close doors
    - Setting world preset/modifiers and global keys through configuration
- Various bugfixes / performance tweaks

### v0.1.3
- Various bugfixes / performance tweaks

### v0.1.2
- Game version compatibility checks
- New features:
    - Show taming progress messages to nearby players
    - Windmills: ignore wind intensity
- Various bugfixes / performance tweaks

### v0.1.1
- Various bugfixes / performance tweaks

### v0.1.0
- Initial release
- Features
    - Time Signs
    - Map Tables: auto pins for portals and ships
    - Make all tameables commandable
    - Toggleable fireplaces
    - Auto-sort containers
    - Auto-pickup drops
    - Auto-feed smelters