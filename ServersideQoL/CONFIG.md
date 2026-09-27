<details open><summary><b>General</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|True|True/False|Enables/disables the entire mod|
|DiagnosticLogs|False|True/False|Enables/disables diagnostic logs|
|AutoReload|True|True/False|True to automatically reload config files when they are changed. <br>This value is only respected during initial startup and changing it later while the server is running has no effect.|
|AutoReloadPollingIntervalSeconds|0||If &gt; 0, config files will be polled every x seconds in addition to listening for file system events. <br>This should help on file systems for which file system events don't work reliably (e.g. NFS).|
|UnifiedConfig|False|True/False|True to use a single config file for all SQoL mods. <br>DO NOT turn this on unless you've updated all SQoL mods to v2.0.11 minimum.|
|ConfigPerWorld|False|True/False|True to save the config files for each world separately in the world save directory|
|FarMessageRange|64||Max distance a player can have to a modified object to receive messages of type TopLeftFar or CenterFar|
