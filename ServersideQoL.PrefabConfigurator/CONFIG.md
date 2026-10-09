<details open><summary><b>BuildPieces</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|DisableRainDamage|false|True/False|True to prevent rain from damaging build pieces|
|DisableSupportRequirements|None|PlayerBuilt, World|Ignore support requirements on build pieces|
|MakeIndestructible|false|True/False|True to make player-built pieces indestructible|
|EnableAsSupport|false|True/False|True to make player-built pieces valid support. <br>For example, this will allow players to place chests or signs on other chests.|
</details>
<details open><summary><b>Carts</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|ContentMassMultiplier|1||Multiplier for a carts content weight. E.g. set to 0 to ignore a cart's content weight|
|DeconstructWithHammer|false|True/False|If enabled, carts can be deconstructed with the build hammer|
</details>
<details open><summary><b>CraftingStations</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|BuildRange_blackforge|20||Build range for 'Black Forge' in meters. Game default: 20. -1 to skip this station and keep the game default.|
|EnemySpawnRange_blackforge|20||Enemy spawn exclusion radius for 'Black Forge' in meters. Game default: 20. 0 disables spawn exclusion. <br>-1 to match 'BuildRange_blackforge' (game default: 20). <br>Note: this area also determines whether dropped items count as being inside a player base (they do not despawn there).|
|BuildRange_forge|20||Build range for 'Forge' in meters. Game default: 20. -1 to skip this station and keep the game default.|
|EnemySpawnRange_forge|20||Enemy spawn exclusion radius for 'Forge' in meters. Game default: 20. 0 disables spawn exclusion. <br>-1 to match 'BuildRange_forge' (game default: 20). <br>Note: this area also determines whether dropped items count as being inside a player base (they do not despawn there).|
|BuildRange_piece_artisanstation|40||Build range for 'Artisan Table' in meters. Game default: 40. -1 to skip this station and keep the game default.|
|EnemySpawnRange_piece_artisanstation|20||Enemy spawn exclusion radius for 'Artisan Table' in meters. Game default: 20. 0 disables spawn exclusion. <br>-1 to match 'BuildRange_piece_artisanstation' (game default: 40). <br>Note: this area also determines whether dropped items count as being inside a player base (they do not despawn there).|
|BuildRange_piece_magetable|20||Build range for 'Galdr Table' in meters. Game default: 20. -1 to skip this station and keep the game default.|
|EnemySpawnRange_piece_magetable|20||Enemy spawn exclusion radius for 'Galdr Table' in meters. Game default: 20. 0 disables spawn exclusion. <br>-1 to match 'BuildRange_piece_magetable' (game default: 20). <br>Note: this area also determines whether dropped items count as being inside a player base (they do not despawn there).|
|BuildRange_piece_stonecutter|20||Build range for 'Stonecutter' in meters. Game default: 20. -1 to skip this station and keep the game default.|
|EnemySpawnRange_piece_stonecutter|20||Enemy spawn exclusion radius for 'Stonecutter' in meters. Game default: 20. 0 disables spawn exclusion. <br>-1 to match 'BuildRange_piece_stonecutter' (game default: 20). <br>Note: this area also determines whether dropped items count as being inside a player base (they do not despawn there).|
|BuildRange_piece_workbench|20||Build range for 'Workbench' in meters. Game default: 20. -1 to skip this station and keep the game default.|
|EnemySpawnRange_piece_workbench|20||Enemy spawn exclusion radius for 'Workbench' in meters. Game default: 20. 0 disables spawn exclusion. <br>-1 to match 'BuildRange_piece_workbench' (game default: 20). <br>Note: this area also determines whether dropped items count as being inside a player base (they do not despawn there).|
</details>
<details open><summary><b>Fireplaces</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|MakeToggleable|false|True/False|True to make all fireplaces/lightsources (including torches, braziers, etc.) toggleable. <br>BEWARE: toggleable fireplaces/lightsources will be toggled off automatically by rain/heavy wind.|
|InfiniteFuel|false|True/False|True to make all fireplaces have infinite fuel|
</details>
<details open><summary><b>Plants</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|GrowTimeMultiplier|1||Multiply plant grow time by this factor. 0 to make them grow almost instantly.|
|SpaceRequirementMultiplier|1||Multiply plant space requirement by this factor. 0 to disable space requirements.|
|DontDestroyIfCantGrow|false|True/False|True to keep plants that can't grow alive|
</details>
<details open><summary><b>PrefabConfigurator</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|Enabled|true|True/False|Enables/disables the entire mod|
</details>
<details open><summary><b>Ships</b></summary>

|Option|Default Value|Acceptable Values|Description|
|------|-------------|-----------------|-----------|
|DeconstructWithHammer|false|True/False|If enabled, ships can be deconstructed with the build hammer|
