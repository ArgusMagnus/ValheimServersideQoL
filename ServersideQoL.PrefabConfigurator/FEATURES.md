Configure every vanilla compatible field in the game. For example,
```
Entries:
- Component: Humanoid
  PrefabNames:
  - Wolf
  Fields:
    m_flying: true
```
will give you flying wolves. Omitting `PrefabNames` will apply the configuration to all prefabs of the specified component.

On startup, a template configuration file with all the available component-prefab combinations and their fields with default values
will be generated.
