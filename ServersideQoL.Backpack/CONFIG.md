<details open><summary><b>Backpack</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|true|True/False|Enables/disables the entire mod|
|OpenBackpackEmote|Wave|-2, Wave, Sit, Challenge, Cheer, NoNoNo, ThumbsUp, Point, BlowKiss, Bow, Cower, Cry, Despair, Flex, ComeHere, Headbang, Kneel, Laugh, Roar, Shrug, Dance, Relax, Toast, Rest, Vibe, LoveYou, Count|Emote to open the backpack. <br>If a player uses this emote, a virtual container acting as their backpack will open. <br>-2 to use any emote as trigger. <br>You can bind emotes to buttons with chat commands. <br>For example, on xbox you can bind the Y-Button to the wave-emote by entering "/bind JoystickButton3 Wave" in the in-game chat. <br>If you use emotes exclusively for this feature, it is recommended to set the value to -2 as it is more reliably detected than specific emotes, especially on bad connection/with crossplay.|
|InitialBackpackSlots|4||Initial available slots in the backpack|
|AdditionalBackpackSlotsPerDefeatedBoss|4||Additional backpack slots per defeated boss|
|MaxBackpackWeight|-1||Maximum backpack weight. -1 for no limit.|
|BackpackOnDeath|SameAsInventory|One of SameAsInventory, Keep, Destroy, DropTombStone, DropItems|What happens to backpack contents on player death|
