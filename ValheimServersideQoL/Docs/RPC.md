# RPC

|Type|Method|Parameters|
|----|------|----------|
|AnimalAI|RPC_OnNearProjectileHit|Int64 sender, Vector3 center, Single range, ZDOID attacker|
|ArmorStand|RPC_DestroyAttachment|Int64 sender, Int32 index|
|ArmorStand|RPC_DropItem|Int64 sender, Int32 index|
|ArmorStand|RPC_DropItemByName|Int64 sender, String name|
|ArmorStand|RPC_RequestOwn|Int64 sender|
|ArmorStand|RPC_SetPose|Int64 sender, Int32 index|
|ArmorStand|RPC_SetVisualItem|Int64 sender, Int32 index, String itemName, Int32 variant|
|BaseAI|RPC_Alert|Int64 sender|
|BaseAI|RPC_OnNearProjectileHit|Int64 sender, Vector3 center, Single range, ZDOID attacker|
|BaseAI|RPC_SetAggravated|Int64 sender, Boolean aggro, Int32 reason|
|Bed|RPC_SetOwner|Int64 sender, Int64 uid, String name|
|Beehive|RPC_Extract|Int64 caller|
|Catapult|RPC_OnLegUse|Int64 sender, Boolean value|
|Catapult|RPC_SetLoadedVisual|Int64 sender, String name|
|Catapult|RPC_Shoot|Int64 sender|
|Character|RPC_AddNoise|Int64 sender, Single range|
|Character|RPC_Damage|Int64 sender, HitData hit|
|Character|RPC_FreezeFrame|Int64 sender, Single duration|
|Character|RPC_Heal|Int64 sender, Single hp, Boolean showText|
|Character|RPC_ResetCloth|Int64 sender|
|Character|RPC_SetTamed|Int64 sender, Boolean tamed|
|Character|RPC_Stagger|Int64 sender, Vector3 forceDirection|
|Character|RPC_TeleportTo|Int64 sender, Vector3 pos, Quaternion rot, Boolean distantTeleport|
|Chat|RPC_ChatMessage|Int64 sender, Vector3 position, Int32 type, UserInfo userInfo, String text|
|Chat|RPC_TeleportPlayer|Int64 sender, Vector3 pos, Quaternion rot, Boolean distantTeleport|
|Container|RPC_OpenRespons|Int64 uid, Boolean granted|
|Container|RPC_RequestOpen|Int64 uid, Int64 playerID|
|Container|RPC_RequestStack|Int64 uid, Int64 playerID|
|Container|RPC_RequestTakeAll|Int64 uid, Int64 playerID|
|Container|RPC_StackResponse|Int64 uid, Boolean granted|
|Container|RPC_TakeAllRespons|Int64 uid, Boolean granted|
|CookingStation|RPC_AddFuel|Int64 sender|
|CookingStation|RPC_AddItem|Int64 sender, String itemName|
|CookingStation|RPC_RemoveDoneItem|Int64 sender, Vector3 userPoint, Int32 amount|
|CookingStation|RPC_SetSlotVisual|Int64 sender, Int32 slot, String item|
|DamageText|RPC_DamageText|Int64 sender, ZPackage pkg|
|Destructible|RPC_CreateFragments|Int64 peer|
|Destructible|RPC_Damage|Int64 sender, HitData hit|
|Door|RPC_UseDoor|Int64 uid, Boolean forward|
|Feast|RPC_EatConfirmation|Int64 sender|
|Feast|RPC_OnEat|Int64 sender|
|Feast|RPC_TryEat|Int64 sender|
|Fermenter|RPC_AddItem|Int64 sender, String name|
|Fermenter|RPC_Tap|Int64 sender|
|Fireplace|RPC_AddFuel|Int64 sender|
|Fireplace|RPC_AddFuelAmount|Int64 sender, Single amount|
|Fireplace|RPC_SetFuelAmount|Int64 sender, Single fuel|
|Fireplace|RPC_ToggleOn|Int64 sender|
|Fish|RPC_Pickup|Int64 uid|
|Fish|RPC_RequestPickup|Int64 uid|
|FishingFloat|RPC_Nibble|Int64 sender, ZDOID fishID, Boolean correctBait|
|FootStep|RPC_Step|Int64 sender, Int32 effectIndex, Vector3 point|
|Game|RPC_DiscoverClosestLocation|Int64 sender, String name, Vector3 point, String pinName, Int32 pinType, Boolean showMap, Boolean discoverAll|
|Game|RPC_DiscoverLocationResponse|Int64 sender, String pinName, Int32 pinType, Vector3 pos, Boolean showMap|
|Game|RPC_Ping|Int64 sender, Single time|
|Game|RPC_Pong|Int64 sender, Single time|
|Game|RPC_SetConnection|Int64 sender, ZDOID portalID, ZDOID connectionID|
|Humanoid|RPC_TeleportTo|Int64 sender, Vector3 pos, Quaternion rot, Boolean distantTeleport|
|Incinerator|RPC_AnimateLever|Int64 uid|
|Incinerator|RPC_AnimateLeverReturn|Int64 uid|
|Incinerator|RPC_IncinerateRespons|Int64 uid, Int32 r|
|Incinerator|RPC_RequestIncinerate|Int64 uid, Int64 playerID|
|ItemDrop|RPC_MakePiece|Int64 sender|
|ItemDrop|RPC_RequestOwn|Int64 uid|
|ItemStand|RPC_DestroyAttachment|Int64 sender|
|ItemStand|RPC_DropItem|Int64 sender|
|ItemStand|RPC_RequestOwn|Int64 sender|
|ItemStand|RPC_SetVisualItem|Int64 sender, String itemName, Int32 variant, Int32 quality|
|MapTable|RPC_MapData|Int64 sender, ZPackage pkg|
|MasterClient|RPC_ServerList|ZRpc rpc, ZPackage pkg|
|MaterialVariation|RPC_UpdateMaterial|Int64 sender, Int32 index|
|MessageHud|RPC_ShowMessage|Int64 sender, Int32 type, String text|
|MineRock|RPC_Hide|Int64 sender, Int32 index|
|MineRock|RPC_Hit|Int64 sender, HitData hit, Int32 hitAreaIndex|
|MineRock5|RPC_Damage|Int64 sender, HitData hit, Int32 hitAreaIndex|
|MineRock5|RPC_SetAreaHealth|Int64 sender, Int32 index, Single health|
|MonsterAI|RPC_OnNearProjectileHit|Int64 sender, Vector3 center, Single range, ZDOID attackerID|
|MonsterAI|RPC_Wakeup|Int64 sender|
|MusicVolume|RPC_PlayMusic|Int64 sender|
|OfferingBowl|RPC_BossSpawnInitiated|Int64 senderId|
|OfferingBowl|RPC_RemoveBossSpawnInventoryItems|Int64 senderId|
|OfferingBowl|RPC_SpawnBoss|Int64 senderId, Vector3 point, Boolean removeItemsFromInventory|
|Pickable|RPC_Pick|Int64 sender, Int32 bonus|
|Pickable|RPC_SetPicked|Int64 sender, Boolean picked|
|PickableItem|RPC_Pick|Int64 sender|
|Player|RPC_Message|Int64 sender, Int32 type, String msg, Int32 amount|
|Player|RPC_OnDeath|Int64 sender|
|Player|RPC_OnTargeted|Int64 sender, Boolean sensed, Boolean alerted|
|Player|RPC_TeleportTo|Int64 sender, Vector3 pos, Quaternion rot, Boolean distantTeleport|
|Player|RPC_UseEitr|Int64 sender, Single v|
|Player|RPC_UseStamina|Int64 sender, Single v|
|PrivateArea|RPC_FlashShield|Int64 uid|
|PrivateArea|RPC_ToggleEnabled|Int64 uid, Int64 playerID|
|PrivateArea|RPC_TogglePermitted|Int64 uid, Int64 playerID, String name|
|Projectile|RPC_Attach|Int64 sender, ZDOID parent|
|Projectile|RPC_OnHit|Int64 sender|
|RandEventSystem|RPC_ConsoleResetRandomEvent|Int64 sender|
|RandEventSystem|RPC_ConsoleStartRandomEvent|Int64 sender|
|RandEventSystem|RPC_SetEvent|Int64 sender, String eventName, Single time, Vector3 pos|
|ResourceRoot|RPC_Drain|Int64 caller, Single amount|
|Sadle|RPC_Controls|Int64 sender, Vector3 rideDir, Int32 rideSpeed, Single skill|
|Sadle|RPC_ReleaseControl|Int64 sender, Int64 playerID|
|Sadle|RPC_RemoveSaddle|Int64 sender, Vector3 userPoint|
|Sadle|RPC_RequestControl|Int64 sender, Int64 playerID|
|Sadle|RPC_RequestRespons|Int64 sender, Boolean granted|
|SapCollector|RPC_Extract|Int64 caller|
|SapCollector|RPC_UpdateEffects|Int64 caller|
|SEMan|RPC_AddStatusEffect|Int64 sender, Int32 nameHash, Boolean resetTime, Int32 itemLevel, Single skillLevel|
|ShieldGenerator|RPC_AddFuel|Int64 sender|
|ShieldGenerator|RPC_Attack|Int64 sender|
|ShieldGenerator|RPC_HitNow|Int64 sender|
|Ship|RPC_Backward|Int64 sender|
|Ship|RPC_Forward|Int64 sender|
|Ship|RPC_Rudder|Int64 sender, Single value|
|Ship|RPC_Stop|Int64 sender|
|ShipControlls|RPC_ReleaseControl|Int64 sender, Int64 playerID|
|ShipControlls|RPC_RequestControl|Int64 sender, Int64 playerID|
|ShipControlls|RPC_RequestRespons|Int64 sender, Boolean granted|
|Smelter|RPC_AddFuel|Int64 sender|
|Smelter|RPC_AddOre|Int64 sender, String name|
|Smelter|RPC_EmptyProcessed|Int64 sender|
|Talker|RPC_Say|Int64 sender, Int32 ctype, UserInfo user, String text|
|Tameable|RPC_AddSaddle|Int64 sender|
|Tameable|RPC_Command|Int64 sender, ZDOID characterID, Boolean message|
|Tameable|RPC_SetName|Int64 sender, String name, String authorId|
|Tameable|RPC_SetSaddle|Int64 sender, Boolean enabled|
|Tameable|RPC_UnSummon|Int64 sender|
|TeleportWorld|RPC_SetConnected|Int64 sender, ZDOID portalID|
|TeleportWorld|RPC_SetTag|Int64 sender, String tag, String authorId|
|TerrainComp|RPC_ApplyOperation|Int64 sender, ZPackage pkg|
|Trap|RPC_OnStateChanged|Int64 uid, Int32 value, Int64 idOfClientModifyingState|
|Trap|RPC_RequestStateChange|Int64 senderID, Int32 value|
|TreeBase|RPC_Damage|Int64 sender, HitData hit|
|TreeBase|RPC_Grow|Int64 uid|
|TreeBase|RPC_Shake|Int64 uid|
|TreeLog|RPC_Damage|Int64 sender, HitData hit|
|TriggerSpawner|RPC_Trigger|Int64 sender|
|Turret|RPC_AddAmmo|Int64 sender, String name|
|Turret|RPC_SetTarget|Int64 sender, ZDOID character|
|Vagon|RPC_RequestDenied|Int64 sender|
|Vagon|RPC_RequestOwn|Int64 sender|
|WearNTear|RPC_ClearCachedSupport|Int64 sender|
|WearNTear|RPC_CreateFragments|Int64 peer|
|WearNTear|RPC_Damage|Int64 sender, HitData hit|
|WearNTear|RPC_HealthChanged|Int64 peer, Single health|
|WearNTear|RPC_Remove|Int64 sender, Boolean blockDrop|
|WearNTear|RPC_Repair|Int64 sender|
|ZDOMan|RPC_DestroyZDO|Int64 sender, ZPackage pkg|
|ZDOMan|RPC_RequestZDO|Int64 sender, ZDOID id|
|ZDOMan|RPC_ZDOData|ZRpc rpc, ZPackage pkg|
|ZNet|RPC_AdminList|ZRpc rpc, ZPackage pkg|
|ZNet|RPC_Ban|ZRpc rpc, String user|
|ZNet|RPC_CharacterID|ZRpc rpc, ZDOID characterID|
|ZNet|RPC_ClientHandshake|ZRpc rpc, Boolean needPassword, String serverPasswordSalt|
|ZNet|RPC_Disconnect|ZRpc rpc|
|ZNet|RPC_Error|ZRpc rpc, Int32 error|
|ZNet|RPC_Kick|ZRpc rpc, String user|
|ZNet|RPC_Kicked|ZRpc rpc|
|ZNet|RPC_NetTime|ZRpc rpc, Double time|
|ZNet|RPC_PeerInfo|ZRpc rpc, ZPackage pkg|
|ZNet|RPC_PlayerList|ZRpc rpc, ZPackage pkg|
|ZNet|RPC_PrintBanned|ZRpc rpc|
|ZNet|RPC_RemoteCommand|ZRpc rpc, String command|
|ZNet|RPC_RemotePrint|ZRpc rpc, String text|
|ZNet|RPC_Save|ZRpc rpc|
|ZNet|RPC_SavePlayerProfile|ZRpc rpc|
|ZNet|RPC_ServerHandshake|ZRpc rpc|
|ZNet|RPC_ServerSyncedPlayerData|ZRpc rpc, ZPackage data|
|ZNet|RPC_Unban|ZRpc rpc, String user|
|ZNetScene|RPC_SpawnObject|Int64 spawner, Vector3 pos, Quaternion rot, Int32 prefabHash|
|ZoneSystem|RPC_GlobalKeys|Int64 sender, List`1 keys|
|ZoneSystem|RPC_LocationIcons|Int64 sender, ZPackage pkg|
|ZoneSystem|RPC_RemoveGlobalKey|Int64 sender, String name|
|ZoneSystem|RPC_SetGlobalKey|Int64 sender, String name|
|ZRoutedRpc|RPC_RoutedRPC|ZRpc rpc, ZPackage pkg|
|ZSyncAnimation|RPC_SetTrigger|Int64 sender, String name|
