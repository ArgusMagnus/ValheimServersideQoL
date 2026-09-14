### v2.0.11
- New option to use a single config file for all SQoL mods: `UnifiedConfig`
- New option to have a separate set of config files for each world: `ConfigPerWorld`

### v2.0.10
- Required for PrefabConfigurator

### v2.0.9
- Fix: hard crash that stopped all SQoL mods [#214](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/214)
- Fix: Some TameAssist features not working properly [#212](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/212)

### v2.0.8
- Fix: prevent exceptions in one SQoL mod from killing all other SQoL mods [#206](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/206)
- Support for [backpack](https://valheim.hexium.gg/mods/ArgusMagnus/ServersideQoL_Backpack) mod
- Bugfixes

### v2.0.7
- Fixed bug in the core processing logic that caused a lot of issues in ServersideQoL mods

### v2.0.6
- Update for valheim 1.0.12

### v2.0.5
- Log error and abort when the patcher was not installed correctly

### v2.0.4
- ContainerSigns: fixed signs do not appear when the container size is also changed [#190](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/190)

### v2.0.3
- Fixed InvalidOperationException (missing PrefabInfo for Processor.DataZDO)

### v2.0.2
- Fixed ArgumentException: An item with the same key has already been added. Key: SlowUpdate [#188](https://github.com/ArgusMagnus/ValheimServersideQoL/issues/188)

### v2.0.0
- Major rewrite, split up features into [separate mods](https://valheim.hexium.gg/?q=ArgusMagnus) (see README)