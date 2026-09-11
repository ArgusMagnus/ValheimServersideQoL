Configure every vanilla compatible field in the game.

The main config has toggles for various presets (like enabling infinite fuel for fireplaces/lightsources or making build pieces indestructible/ignore rain damage).
For anything else you'll need to copy and edit `$(ValheimInstallDir)/BepInEx/config/ArgusMagnus.{PluginName}/Prefabs.default.yml` (see example below). This file is generated on startup
and contains all the available component-prefab combinations and their fields with default values. If the yml-file contains entries that conflict with a preset
set in the main config, the yml-file will win.

<details>
  <summary><b>Examples:</b></summary>

Omitting `PrefabNames` will apply the configuration to all prefabs of the specified component.

*$(ValheimInstallDir)/BepInEx/config/ArgusMagnus.{PluginName}/Prefabs.yml*:

```
Entries:
# Increase range of stone cutter/black forge
- Component: CraftingStation
  PrefabNames:
  - piece_stonecutter
  - blackforge
  Fields:
    m_rangeBuild: 40
  
# Allow crafting station upgrades to connect to their crafting station from farther away
- Component: StationExtension
  Fields:
    m_maxStationDistance: 64
    
# Halve fermentation duration for all fermenters
- Component: Fermenter
  Fields:
    m_fermentationDuration: /2

# Ignore wind intensity for windmills (run full power even if there is no wind)
- Component: Windmill
  Fields:
    m_minWindSpeed: -3.4028235E+38 # float.MinValue

# Disable unsummoning of skeletts
- Component: Tameable
  Fields:
    m_unsummonDistance: 0 # Disable distance-based unsummoning of summoned skeletts
    m_unsummonOnOwnerLogoutSeconds: 0 # Disable logout time-based unsummoning of summoned skeletts
```

</details>

This mod is intentionally kept simple. If you want powerful scripting support, use Expand World Prefabs.
