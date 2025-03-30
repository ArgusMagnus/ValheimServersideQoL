### v0.2.7
- New feature: in-world config room: A generated room that allows admins to make changes to this mods config by editing signs
- Fix for Exception on load [[19]](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/19)

### v0.2.6
- Fix for `NotSupportedException` on load [[19]](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/19)

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
- Fix boats getting destroyed [[8]](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/8)
- Fix for infinite stamina related issues [[4]](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/4)

### v0.2.2
- Change default config: disable all features by default [[7]](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/7)
- Potential fix for infinite stamina related issues [[4]](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/4)
- Fix certain build pieces getting duplicated [[9]](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/9)

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