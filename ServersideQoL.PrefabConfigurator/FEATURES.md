Configure every vanilla compatible field in the game.

On startup, a template configuration file with all the available component-prefab combinations and their fields with default values
will be generated.
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

# Give all fireplaces infinite fuel
- Component: Fireplace
  Fields:
    # m_infiniteFuel: true disables toggling, this way toggling still works
    m_secPerFuel: 0
    m_canRefill: false
    # m_canTurnOff: true # Make all toggleable
    
# Disable space requirements and halve grow time for all plants
- Component: Plant
  Fields:
    m_growRadius: 0
    m_destroyIfCantGrow: false
    m_growTime: /2
    m_growTimeMax: /2

# Allow all ships/carts to be deconstructed with the build hammer
- Component: Piece
  PrefabNames:
  - Raft
  - Karve
  - VikingShip
  - VikingShip_Ashlands
  - Cart
  - BatteringRam
  - Catapult
  Fields:
    m_canBeRemoved: true

# Ignore wind intensity for windmills (run full power even if there is no wind)
- Component: Windmill
  Fields:
    m_minWindSpeed: -3.4028235E+38
```

</details>
