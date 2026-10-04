Allows the wishbone to find
- dungeons
- vegvisir
- any prefab defined in `$(ValheimInstallDir)/BepInEx/config/ArgusMagnus.ServersideQoL/ArgusMagnus.{PluginName}.Advanced.yml`

<details open>
  <summary><b>Examples:</b></summary>

*$(ValheimInstallDir)/BepInEx/config/ArgusMagnus.ServersideQoL/ArgusMagnus.{PluginName}.Advanced.yml*:

```
Entries:
- PrefabNamePattern: Beehive
  Enabled: true
  Range: 64
  MinQuality: 0
- PrefabNamePattern: goblin_totempole
  Enabled: true
  Range: 64
  MinQuality: 0
- PrefabNamePattern: giant_brain
  Enabled: true
  Range: 64
  MinQuality: 0
- PrefabNamePattern: dvergrprops_crate*
  Enabled: true
  Range: 64
  MinQuality: 0
- PrefabNamePattern: Fish*
  Enabled: true
  Range: 64
  MinQuality: 4
- PrefabNamePattern: Serpent
  Enabled: true
  Range: 64
  MinQuality: 0
- PrefabNamePattern: Abomination
  Enabled: true
  Range: 64
  MinQuality: 0
```

</details>
