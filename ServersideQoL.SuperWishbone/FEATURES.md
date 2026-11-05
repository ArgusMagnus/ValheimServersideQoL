Allows the wishbone to find
- dungeons
- vegvisir
- any prefab defined in `BepInEx/config/ArgusMagnus.ServersideQoL/ArgusMagnus.{PluginName}.Advanced.yml`

<details open>
  <summary><b>Examples:</b></summary>

*BepInEx/config/ArgusMagnus.ServersideQoL/ArgusMagnus.{PluginName}.Advanced.yml*:

```
Entries:
- PrefabNamePattern: Beehive
  Enabled: true
  Range: 64
  MinLevel: 1
  MinQuality: 1
- PrefabNamePattern: goblin_totempole
  Enabled: true
  Range: 64
  MinLevel: 1
  MinQuality: 1
- PrefabNamePattern: giant_brain
  Enabled: true
  Range: 64
  MinLevel: 1
  MinQuality: 1
- PrefabNamePattern: dvergrprops_crate*
  Enabled: true
  Range: 64
  MinLevel: 1
  MinQuality: 1
- PrefabNamePattern: Fish*
  Enabled: true
  Range: 64
  MinLevel: 1
  MinQuality: 4
- PrefabNamePattern: Serpent
  Enabled: true
  Range: 0 # Use max distance based on simulation/render distance settings
  MinLevel: 1
  MinQuality: 1

# Mark any container/anything that could drop idols
- PrefabNamePattern: Upgrader*Armor
  Enabled: true
  Range: 0
  MinLevel: 1
  MinQuality: 1
- PrefabNamePattern: Upgrader*Weapon
  Enabled: true
  Range: 0
  MinLevel: 1
  MinQuality: 1
```

</details>
