<details open><summary><b>AutoPortalHub</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|true|True/False|Enables/disables the entire mod. <br>True to automatically generate a portal hub. <br>Placed portals which don't have a paired portal in the world will be connected to the portal hub.|
|Exclude|||Portals with a tag that matches this filter are not connected to the portal hub|
|Include|*||Only portals with a tag that matches this filter are connected to the portal hub|
|AutoNameNewPortals|false|True/False|True to automatically name new portals|
|AutoNameNewPortalsFormat|{0} {1:D2}|.NET Format strings for 2 arguments (String, Int32): https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-string-format#get-started-with-the-stringformat-method|Format string for auto-naming portals, the first argument is the biome name, the second is an automatically incremented integer|
