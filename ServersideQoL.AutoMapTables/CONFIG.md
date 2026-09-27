<details open><summary><b>AutoMapTables</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|True|True/False|Enables/disables the entire mod|
|MapTableRange|64||If a player enters this range around a map table, their discovered information (portal/ship/ore deposits/etc. position) is transfered to the map table.|
|PortalsPinType|Icon4|None, Icon0, Icon1, Icon2, Icon3, Icon4|The pin type for portals on the map table|
|ShipsPinType|Player|None, Icon0, Icon1, Icon2, Icon3, Icon4, Player|The pin type for ships on the map table|
|DungeonsPinType|Icon2|None, Icon0, Icon1, Icon2, Icon3, Icon4|The pin type for dungeons on the map table|
|DungeonsLabel|Default||The pin label for dungeons|
|DungeonsPinTarget|MapTable|Combination of MapTable, DiscoveringPlayer|Whether to add the pin to map tables, the map of the discovering player or both. <br>WARNING: Pins added to player maps can only be removed by the players themselves and will remain even if the mod is removed.|
|DungeonsDiscoverRange|4||A dungeon is considered 'discovered by a player' when that player is detected within this range around the entrance|
|SofttissuePinType|Icon3|None, Icon0, Icon1, Icon2, Icon3, Icon4|The pin icon to use for Soft Tissue. <br>Pins will only be added to the map table after the ore deposit was hit at least once with a pickaxe.|
|SofttissueLabel|Default||The label to use for Soft Tissue pins|
|IronScrapPinType|Icon3|None, Icon0, Icon1, Icon2, Icon3, Icon4|The pin icon to use for Scrap Iron. <br>Pins will only be added to the map table after the ore deposit was hit at least once with a pickaxe.|
|IronScrapLabel|Fe||The label to use for Scrap Iron pins|
|GoldOrePinType|Icon3|None, Icon0, Icon1, Icon2, Icon3, Icon4|The pin icon to use for Petrified Tissue. <br>Pins will only be added to the map table after the ore deposit was hit at least once with a pickaxe.|
|GoldOreLabel|Default||The label to use for Petrified Tissue pins|
|SilverOrePinType|Icon3|None, Icon0, Icon1, Icon2, Icon3, Icon4|The pin icon to use for Silver Ore. <br>Pins will only be added to the map table after the ore deposit was hit at least once with a pickaxe.|
|SilverOreLabel|Ag||The label to use for Silver Ore pins|
|CopperOrePinType|Icon3|None, Icon0, Icon1, Icon2, Icon3, Icon4|The pin icon to use for Copper Ore. <br>Pins will only be added to the map table after the ore deposit was hit at least once with a pickaxe.|
|CopperOreLabel|Cu||The label to use for Copper Ore pins|
|OreDepositsPinTarget|MapTable|Combination of MapTable, DiscoveringPlayer|Whether to add the pin to map tables, the map of the discovering player or both. <br>WARNING: Pins added to player maps can only be removed by the players themselves and will remain even if the mod is removed.|
|OreDepositsDiscoverRange|32||An ore deposit is considered 'discovered by a player' when that player was within this range around the deposit while it was struck by a pickaxe|
|UpdatedMessageType|None|None, TopLeftNear, TopLeftFar, CenterNear, CenterFar, InWorld|Type of message to show when a map table is updated|
|DiscoveredMessageType|TopLeftFar|None, TopLeftFar, CenterFar, InWorld|Type of message to show to a player when they discovered map information|
|DiscardPlayerPins|False|True/False|True to discard custom player pins from map tables|
