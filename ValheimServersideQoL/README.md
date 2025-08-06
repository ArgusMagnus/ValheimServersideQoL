# Serverside QoL
This mod adds some QoL features as a serverside-only mod.

It's designed and tested for **dedicated servers** with **vanilla clients** (e.g. xbox users).

If you'd like to support me, [subscribe as a test user](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/98) and/or [buy me a coffee](https://coff.ee/argusmagnus).

## Features
**All of these features are disabled by default and must be enabled in the configuration first.**
This overview may not be complete, check the config options below for a complete list of features.
- Generated in-world room where admins can change the mod's configuration by editing signs and toggling candles
- Assign ownership of some interactable objects (smelters, cooking stations, etc.) to the nearest player to help avoid the loss of ore/etc. on bad connections/crossplay
- Automatically generated portal hub to connect all portals
- Crafting Stations: Modify range and max extension distance of crafting stations
- Signs:
    - Set the default text color of signs
    - Signs which show the ingame time
- Map Tables: Automatically add pins for portals and ships to map tables
- Tames
    - Commandable: Make all tames commandable (like wolves)
    - Taming progress: Show taming progress to nearby players
    - Teleport follow: Teleport following tames to the players location if they get too far away from the player
    - Always fed: Make all tames always fed (not hungry)
    - Growing progress: Show growing progress of offspring/eggs to nearby players
- Creatures
    - Show stars for higher level creatures (> 2 stars)
    - Show aura for higher level creatures (> 2 stars)
    - Adjust creature max level, optionally dependent on defeated bosses
- Fireplaces (including torches/sconces/braziers/etc.)
    - Toggleable: Make all fireplaces toggleable (you can turn them on/off)
    - Infinite fuel: Make all fireplaces have infinite fuel
    - Ignore rain: Make all fireplaces ignore rain
- Containers
    - Automatically sort inventories
    - Configure inventory sizes
    - Automatically put signs on chests
    - Use obliterator to teleport items
- Item drops: Automatically put dropped items into chests
- Smelters (including windmills/hot-tubs/shield generators/etc.)
    - Feed/refuel smelters from nearby containers
- Windmills: Make windmills ignore wind intensity
- Doors: Automatically close doors
- Infinite Stamina: Give players infinite stamina when building/farming/mining or always
- Ballista
    - Dont target players: stop ballistas from targeting players
    - Dont target tames: stop ballistas from targeting tames
    - Load from containers: reload ballistas with ammo from nearby containers
- Build Pieces
    - Disable rain damage
    - Disable support requirements for player-built pieces and world pieces (e.g. Ashland structures) seperately
- Traders: always unlock trader items (remove progression requirements for buying from trader items). Only supported on dedicated servers.
- Plants: modify plant grow time and space requirement
- Traps: prevent traps from being triggered by players, friendly fire and damaging themselves
- World: remove mist from Mistlands
- Global Keys
    - Set world preset/modifiers and global keys via config
    - NoPortals: Change the behavior of the NoPortals key to prevent the construction of new portals, but leave existing portals functional

### Feature Requests
If you have an idea you think might fit this mod, you can create a feature request issue in the [github project](https://github.com/ArgusMagnus/ValheimServersideQoL/issues?q=is%3Aissue%20label%3Aenhancement%20).

## Known Issues
Known issues are listed in the [github project](https://github.com/ArgusMagnus/ValheimServersideQoL/issues?q=is%3Aissue%20label%3Abug%20(state%3Aopen%20OR%20label%3Awontfix)).
If you experience an issue, please file a report there.

## Uninstallation/Removal
Many of this mod's features permanently modify the world, if you simply uninstall the mod, these changes will persist in the world.
To reverse these changes, you can reset the configuration to default values (e.g. by deleting the config file) and leave the mod running for a while (until the affected zones have been visited by players).
After that, it can be uninstalled normally.

## Configuration
The configuration is loaded from `$(ValheimInstallDir)/BepInEx/config/argusmagnus.ServersideQoL.cfg`. Start the server once to generate the file if it does not exist.

