# Serverside QoL
This mod adds some QoL features as a serverside-only mod. It's designed and tested for dedicated servers and clients running vanilla (e.g. xbox users).

## Disclaimer
This mod is in early development and the features experimental. Backup your world before using this mod.

## Features
- Time Signs: Signs which show the ingame time
- Map Tables: Automatically add pins for portals and ships to map tables
- Tames
    - Commandable: Make all tames commandable (like wolves)
    - Taming progress: Show taming progress messages to nearby players
    - Teleport follow: Teleport following tames to the players location if they get too far away from the player
    - Always feds: Make all tames always fed (not hungry)
- Fireplaces (including torches/sconces/braziers/etc.)
    - Toggleable: Make all fireplaces toggleable (you can turn them on/off)
    - Infinite fuel: Make all fireplaces have infinite fuel
- Containers
    - Automatically sort inventories
    - Configure inventory sizes
- Item drops: Automatically put dropped items into chests
- Smelters: Feed/refuel smelters from nearby containers
- Windmills: Make windmills ignore wind intensity
- Doors: Automatically close doors
- Infinite Stamina: Give players infinite stamina when building/farming or always
- Ballista
    - Dont target players: stop ballistas from targeting players
    - Dont target tames: stop ballistas from targeting tames
    - Load from containers: reload ballistas with ammo from nearby containers
- Global Keys
    - Set world preset/modifiers and global keys via config
    - NoPortals: Change the behavior of the NoPortals key to prevent the construction of new portals, but leave existing portals functional

All of these features can be enabled/disabled separately via config.

## Known Issues
- Modifying the inventory size of ships causes them to stay in the air after construction, until touched by a player