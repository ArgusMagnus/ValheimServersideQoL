# Serverside QoL
This mod adds some QoL features as a serverside-only mod.

It's designed and tested for **dedicated servers** with **vanilla clients** (e.g. xbox users).

## Disclaimer
This mod is in early development and the features experimental. Backup your world before using this mod.

## Features
**All of these features are disabled by default and must be enabled in the configuration first**
- Time Signs: Signs which show the ingame time
- Map Tables: Automatically add pins for portals and ships to map tables
- Tames
    - Commandable: Make all tames commandable (like wolves)
    - Taming progress: Show taming progress to nearby players
    - Teleport follow: Teleport following tames to the players location if they get too far away from the player
    - Always fed: Make all tames always fed (not hungry)
    - Growing progress: Show growing progress of offspring/eggs to nearby players
- Fireplaces (including torches/sconces/braziers/etc.)
    - Toggleable: Make all fireplaces toggleable (you can turn them on/off)
    - Infinite fuel: Make all fireplaces have infinite fuel
- Containers
    - Automatically sort inventories
    - Configure inventory sizes
- Item drops: Automatically put dropped items into chests
- Smelters (including windmills/hot-tubs/shield generators/etc.)
    - Feed/refuel smelters from nearby containers
- Windmills: Make windmills ignore wind intensity
- Doors: Automatically close doors
- Infinite Stamina: Give players infinite stamina when building/farming or always
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
- Global Keys
    - Set world preset/modifiers and global keys via config
    - NoPortals: Change the behavior of the NoPortals key to prevent the construction of new portals, but leave existing portals functional

### Feature Requests
I'm developing this mod mainly for myself, so the main deciding factor if a feature gets implemented or not (besides if it is technically possible) is
if I think it's something I may want to use myself.

However, if you have an idea you think might fit this mod, I invite you to create a feature request issue in the [github project](https://github.com/ArgusMagnus/ValheimServersideQoL/issues).

## Known Issues
- Modifying the inventory size of ships causes them to stay in the air after construction, until touched by a player
- Removing trader item progression requirements may cause Hunin/Munin to appear with hints to undiscovered biomes, etc.
  Going to the trader at night may result in night-time spawns of undefeated bosses.

If you experience an issue, please file a report in the [github project](https://github.com/ArgusMagnus/ValheimServersideQoL/issues).

## Configuration
The configuration is loaded from `$(ValheimInstallDir)/BepInEx/config/argusmagnus.ServersideQoL.cfg`. Start the server once to generate the file if it does not exist.

