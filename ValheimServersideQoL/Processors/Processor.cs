using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using Valheim.ServersideQoL.HarmonyPatches;
using RoutedRPCData = ZRoutedRpc.RoutedRPCData;

namespace Valheim.ServersideQoL.Processors;

abstract class Processor
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ProcessorAttribute : Attribute
    {
        public required int Priority { get; init; }
    }

    static IReadOnlyList<Processor>? _defaultProcessors;
    public static IReadOnlyList<Processor> DefaultProcessors => _defaultProcessors ??= [.. typeof(Processor).Assembly.GetTypes()
        .Where(static x => x is { IsClass: true, IsAbstract: false } && x.IsSubclassOf(typeof(Processor)))
        .OrderByDescending(static x => x.GetCustomAttribute<ProcessorAttribute>()?.Priority ?? 0)
        .Select(static x => (Processor)Activator.CreateInstance(x))];

#if DEBUG
    static Processor()
    {
        var list = DefaultProcessors
            .GroupBy(static x => x.Id)
            .Where(static x => x.Count() is not 1)
            .Select(static x => $"({string.Join(", ", x.Select(static x => x.GetType().Name))})").ToList();
        if (list.Count is 0)
            return;

        Main.Instance.Logger.LogError($"Processor Ids must be unique. Offenders: {string.Join(", ", list)}");
        throw new OperationCanceledException();
    }
#endif

    static class InstanceCache<T> where T : Processor, new()
    {
        public static T Instance { get; } = DefaultProcessors.OfType<T>().First();
    }
    public static T Instance<T>() where T : Processor, new() => InstanceCache<T>.Instance;

    protected ManualLogSource Logger => Main.Instance.Logger;
    protected ModConfig Config => Main.Instance.Config;
    protected abstract Guid Id { get; }

    public bool DestroyZdo { get; protected set; }
    public bool RecreateZdo { get; protected set; }
    public bool UnregisterZdoProcessor { get; protected set; }

    readonly Stopwatch _watch = new();
    protected HashSet<ExtendedZDO> PlacedObjects { get; } = [];
    static bool __initialized;
    static ExtendedZDO? _dataZDO;

    public TimeSpan ProcessingTime => _watch.Elapsed;
    long _totalProcessingTimeTicks;
    public TimeSpan TotalProcessingTime => new(_totalProcessingTimeTicks + _watch.ElapsedTicks);

    public virtual void Initialize(bool firstTime)
    {
        __teleportableItems = null;
        ZoneSystemSendGlobalKeys.GlobalKeysChanged -= UpdateTeleportableItems;

        if (!firstTime)
        {
            __initialized = false;
            return;
        }

        if (__initialized)
            return;
        __initialized = true;
        _dataZDO = null;

        foreach (ExtendedZDO zdo in ZDOMan.instance.GetObjects())
        {
            if (zdo.IsModCreator(out var marker))
            {
                switch (marker)
                {
                    case CreatorMarkers.DataZDO:
                        if (_dataZDO is null)
                            _dataZDO = zdo;
                        else
                        {
                            Logger.LogError("More then one DataZDO found, destroying the second one");
                            zdo.Destroy();
                        }
                        break;

                    case CreatorMarkers.ProcessorOwned:
                        var id = zdo.Vars.GetProcessorId();
                        foreach (var processor in DefaultProcessors)
                        {
                            if (processor.Id == id)
                            {
                                processor.PlacedObjects.Add(zdo);
                                break;
                            }
                        }
                        break;

                    default:
                        zdo.Destroy();
                        break;
                }
            }
        }
    }

    protected static ExtendedZDO DataZDO
    {
        get
        {
            if (_dataZDO is null)
            {
                _dataZDO = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(new(WorldGenerator.waterEdge * 10, -1000f, WorldGenerator.waterEdge * 10), Prefabs.Sconce);
                _dataZDO.SetPrefab(Prefabs.Sconce);
                _dataZDO.Persistent = true;
                _dataZDO.Distant = false;
                _dataZDO.Type = ZDO.ObjectType.Default;
                _dataZDO.SetModAsCreator(CreatorMarkers.DataZDO);
                _dataZDO.Vars.SetHealth(-1);
                _dataZDO.Fields<Piece>().Set(static x => x.m_canBeRemoved, false);
                _dataZDO.Fields<WearNTear>().Set(static x => x.m_noRoofWear, false).Set(static x => x.m_noSupportWear, false).Set(static x => x.m_health, -1);
                _dataZDO.UnregisterAllProcessors();
            }
            return _dataZDO;
        }
    }

    public void PreProcess(IEnumerable<Peer> peers)
    {
        _totalProcessingTimeTicks += _watch.ElapsedTicks;
        _watch.Restart();
        PreProcessCore(peers);
        _watch.Stop();
    }

    protected virtual void PreProcessCore(IEnumerable<Peer> peers) { }

    public virtual void PostProcess()
    {
        _watch.Start();
        PostProcessCore();
        _watch.Stop();
    }

    protected virtual void PostProcessCore() { }

    public virtual bool ClaimExclusive(ExtendedZDO zdo) => PlacedObjects.Contains(zdo);

    protected abstract bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers);
    public void Process(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        _watch.Start();

        DestroyZdo = false;
        RecreateZdo = false;
        UnregisterZdoProcessor = false;

        if (zdo.CheckProcessorDataRevisionChanged(this))
        {
            if (ProcessCore(zdo, peers))
                zdo.UpdateProcessorDataRevision(this);
        }

        _watch.Stop();
    }

    protected bool CheckMinDistance(IEnumerable<Peer> peers, ZDO zdo)
        => CheckMinDistance(peers, zdo, Config.General.MinPlayerDistance.Value);

    protected static bool CheckMinDistance(IEnumerable<Peer> peers, ZDO zdo, float minDistance)
    {
        minDistance *= minDistance;
        foreach (var peer in peers)
        {
            if (Utils.DistanceSqr(peer.m_refPos, zdo.GetPosition()) < minDistance)
                return false;
        }
        return true;
    }

    protected ExtendedZDO PlaceObject(Vector3 pos, int prefab, float rot, CreatorMarkers marker = CreatorMarkers.None)
        => PlaceObject(pos, prefab, Quaternion.Euler(0, rot, 0), marker);

    protected ExtendedZDO PlaceObject(Vector3 pos, int prefab, Quaternion rot, CreatorMarkers marker = CreatorMarkers.None)
    {
        var zdo = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, prefab);
        PlacedObjects.Add(zdo);

        zdo.SetPrefab(prefab);
        zdo.Persistent = true;
        zdo.Distant = false;
        zdo.Type = ZDO.ObjectType.Default;
        zdo.SetRotation(rot);
        zdo.SetModAsCreator(marker);
        zdo.Vars.SetHealth(-1);
        if (marker.HasFlag(CreatorMarkers.ProcessorOwned))
            zdo.Vars.SetProcessorId(Id);

        return zdo;
    }

    protected ExtendedZDO PlacePiece(Vector3 pos, int prefab, float rot, CreatorMarkers marker = CreatorMarkers.None)
        => PlacePiece(pos, prefab, Quaternion.Euler(0, rot, 0), marker);

    protected ExtendedZDO PlacePiece(Vector3 pos, int prefab, Quaternion rot, CreatorMarkers marker = CreatorMarkers.None)
    {
        var zdo = PlaceObject(pos, prefab, rot, marker);
        zdo.Fields<Piece>().Set(static x => x.m_canBeRemoved, false);
        zdo.Fields<WearNTear>().Set(static x => x.m_noRoofWear, false).Set(static x => x.m_noSupportWear, false).Set(static x => x.m_health, -1);
        return zdo;
    }

    protected ExtendedZDO RecreatePiece(ExtendedZDO zdo)
    {
        if (!PlacedObjects.Remove(zdo))
            throw new ArgumentException();
        PlacedObjects.Add(zdo = zdo.Recreate());
        return zdo;
    }

    protected void DestroyObject(ExtendedZDO zdo)
    {
        if (!PlacedObjects.Remove(zdo))
            throw new ArgumentException();
        zdo.Destroy();
    }

    protected static Heightmap GetHeightmap(Vector3 pos) => Heightmap.FindHeightmap(pos) ?? SharedProcessorState.CreateHeightmap(pos);
    protected static Heightmap.Biome GetBiome(Vector3 pos) => GetHeightmap(pos).GetBiome(pos);

    protected static string ConvertToRegexPattern(string searchPattern)
    {
        searchPattern = Regex.Escape(searchPattern);
        searchPattern = searchPattern.Replace("\\*", ".*").Replace("\\?", ".?");
        return $"(?i)^{searchPattern}$";
    }

    static IReadOnlyDictionary<ItemDrop.ItemData, GameObject>? __teleportableItems;
    protected IReadOnlyDictionary<ItemDrop.ItemData, GameObject> TeleportableItems
    {
        get
        {
            if (__teleportableItems is null)
                UpdateTeleportableItems();
            return __teleportableItems;
        }
    }

    protected bool IsItemTeleportable(ItemDrop.ItemData item)
    {
        if (item.m_shared.m_teleportable || ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll))
            return true;
        if (!Config.NonTeleportableItems.Enable.Value)
            return false;

        if (__teleportableItems is null)
            UpdateTeleportableItems();
        return __teleportableItems.ContainsKey(item);
    }

    protected bool HasNonTeleportableItem(IEnumerable<ItemDrop.ItemData> items)
    {
        foreach (var item in items)
        {
            if (!IsItemTeleportable(item))
                return true;
        }
        return false;
    }

    [MemberNotNull(nameof(__teleportableItems))]
    void UpdateTeleportableItems()
    {
        var set = __teleportableItems as Dictionary<ItemDrop.ItemData, GameObject>;
        set?.Clear();

        if (Config.NonTeleportableItems.Enable.Value)
        {
            foreach (var entry in Config.NonTeleportableItems.Entries)
            {
                if (string.IsNullOrEmpty(entry.Config.Value))
                    continue;

                if (ZoneSystem.instance.GetGlobalKey(entry.Config.Value))
                    (set ??= []).Add(entry.ItemDrop.m_itemData, entry.ItemDrop.gameObject);
            }

            if (__teleportableItems is null)
                ZoneSystemSendGlobalKeys.GlobalKeysChanged += UpdateTeleportableItems;
        }
        __teleportableItems = set ?? ReadOnlyDictionary<ItemDrop.ItemData, GameObject>.Empty;
    }

    static List<object?> __args = [];
    static bool HandleRoutedRPCPrefix(RoutedRPCData data)
    {
        if (__methods.TryGetValue(data.m_methodHash, out var rpcMethod))
        {
            ExtendedZDO? zdo = null;
            for (int i = 0; i < rpcMethod.Delegates.Count; i++)
            {
                var del = rpcMethod.Delegates[i];
                try
                {
                    __args.Clear();
                    ZRpc.Deserialize(del.Parameters, data.m_parameters, ref __args);
                    data.m_parameters.SetPos(0);
                    if (del.DataParameterIndex > -1)
                        __args.Insert(Math.Min(del.DataParameterIndex, __args.Count), data);
                    if (del.ZdoParameterIndex > -1)
                        __args.Insert(del.ZdoParameterIndex + ((del.DataParameterIndex > -1 && del.DataParameterIndex < del.ZdoParameterIndex) ? 1 : 0), zdo ??= ZDOMan.instance.GetExtendedZDO(data.m_targetZDO));

                    if (del.Delegate.DynamicInvoke([.. __args!]) is bool success && !success)
                    {
                        //Main.Instance.Logger.DevLog($"Invokation of {rpcMethod.Name} cancelled");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Main.Instance.Logger.LogError($"{rpcMethod.Name}: {del.Delegate.Method.DeclaringType.Name}.{del.Delegate.Method.Name}: {ex}");
                    rpcMethod.Delegates.RemoveAt(i--);
                    if (rpcMethod.Delegates.Count is 0 && __methods.Remove(data.m_methodHash) && __methods.Count is 0)
                        Main.HarmonyInstance.Unpatch(__handleRoutedRPCMethod, __handleRoutedRPCPrefix);
                }
            }
        }
        return true;
    }

    sealed class RpcDelegate
    {
        public Delegate Delegate { get; }
        public ParameterInfo[] Parameters { get; }
        public int DataParameterIndex { get; }
        public int ZdoParameterIndex { get; }
        public RpcDelegate(Delegate del)
        {
            Delegate = del;
            Parameters = del.Method.GetParameters();
            var pars = Parameters.Select(static x => x.ParameterType).ToList();
            DataParameterIndex = pars.IndexOf(typeof(RoutedRPCData));
            ZdoParameterIndex = pars.IndexOf(typeof(ExtendedZDO));
        }
    }

    sealed record RpcMethod(string Name, List<RpcDelegate> Delegates);
    static readonly Dictionary<int, RpcMethod> __methods = [];
    static readonly MethodInfo __handleRoutedRPCMethod = typeof(ZRoutedRpc).GetMethod("HandleRoutedRPC", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly MethodInfo __handleRoutedRPCPrefix = new Func<RoutedRPCData, bool>(HandleRoutedRPCPrefix).Method;

    protected static void UpdateRpcSubscription(string methodName, Delegate handler, bool subscribe)
    {
        var methodHash = methodName.GetStableHashCode();
        if (!__methods.TryGetValue(methodHash, out var rpcMethod) && subscribe)
            __methods.Add(methodHash, rpcMethod = new(methodName, []));
        if (subscribe)
        {
            if (!rpcMethod.Delegates.Any(x => x.Delegate == handler))
                rpcMethod.Delegates.Add(new(handler));
        }
        else if (rpcMethod is not null)
        {
            var idx = rpcMethod.Delegates.FindIndex(x => x.Delegate == handler);
            if (idx > -1)
            {
                rpcMethod.Delegates.RemoveAt(idx);
                if (rpcMethod.Delegates.Count is 0)
                    __methods.Remove(methodHash);
            }
        }

        Main.HarmonyInstance.Unpatch(__handleRoutedRPCMethod, __handleRoutedRPCPrefix);
        if (__methods.Count > 0)
            Main.HarmonyInstance.Patch(__handleRoutedRPCMethod, prefix: new(__handleRoutedRPCPrefix));
    }

    [Flags]
    public enum CreatorMarkers : uint
    {
        None = 0,
        DataZDO = 1u << 0,
        ProcessorOwned = 1u << 1
    }

    public static class PrefabNames
    {
        public const string Megingjord = "BeltStrength";
        public const string CryptKey = "CryptKey";
        public const string Wishbone = "Wishbone";
        public const string TornSpirit = "YagluthDrop";
        public const string BlackmetalChest = "piece_chest_blackmetal";
        public const string ReinforcedChest = "piece_chest";
        public const string WoodChest = "piece_chest_wood";
        public const string Barrel = "piece_chest_barrel";
        public const string Incinerator = "incinerator";
        public const string GiantBrain = "giant_brain";
    }

    public static class Prefabs
    {
        public static int GraustenFloor4x4 { get; } = "Piece_grausten_floor_4x4".GetStableHashCode();
        public static int GraustenWall4x2 { get; } = "Piece_grausten_wall_4x2".GetStableHashCode();
        public static int PortalWood { get; } = "portal_wood".GetStableHashCode();
        public static int Portal { get; } = "portal".GetStableHashCode();
        public static int Sconce { get; } = "piece_walltorch".GetStableHashCode();
        public static int DvergerGuardstone { get; } = "dverger_guardstone".GetStableHashCode();
        public static int Sign { get; } = "sign".GetStableHashCode();
        public static int Candle { get; } = "Candle_resin".GetStableHashCode();
        public static int BlackmetalChest { get; } = PrefabNames.BlackmetalChest.GetStableHashCode();
        public static int ReinforcedChest { get; } = PrefabNames.ReinforcedChest.GetStableHashCode();
        public static int Barrel { get; } = PrefabNames.Barrel.GetStableHashCode();
        public static int WoodChest { get; } = PrefabNames.WoodChest.GetStableHashCode();
        public static int Incinerator { get; } = PrefabNames.Incinerator.GetStableHashCode();
        public static int PrivateChest { get; } = "piece_chest_private".GetStableHashCode();
        public static int StandingIronTorch { get; } = "piece_groundtorch".GetStableHashCode();
        public static int StandingIronTorchGreen { get; } = "piece_groundtorch_green".GetStableHashCode();
        public static int StandingIronTorchBlue { get; } = "piece_groundtorch_blue".GetStableHashCode();
        //public static IReadOnlyList<int> Banners { get; } = [.. Enumerable.Range(1, 10).Select(static x => $"piece_banner{x:D2}".GetStableHashCode())];
        public static int MountainRemainsBuried { get; } = "Pickable_MountainRemains01_buried".GetStableHashCode();
    }

    protected static class StatusEffects
    {
        public static int Wishbone { get; } = "Wishbone".GetStableHashCode();
        public static int Demister { get; } = "Demister".GetStableHashCode();
        public static int Megingjord { get; } = "BeltStrength".GetStableHashCode();
    }

    protected static void ShowMessage(IEnumerable<Peer> peers, Vector3 pos, string message, MessageTypes type, DamageText.TextType inWorldTextType = DamageText.TextType.Normal)
    {
        //Main.Instance.Logger.DevLog($"ShowMessage: {message}", LogLevel.Info);
        switch (type)
        {
            case MessageTypes.TopLeftNear:
            case MessageTypes.CenterNear:
            case MessageTypes.InWorld:
                peers = peers.Where(x => Vector3.Distance(x.m_refPos, pos) <= DamageText.instance.m_maxTextDistance);
                break;

            case MessageTypes.TopLeftFar:
            case MessageTypes.CenterFar:
                peers = peers.Where(x => Vector3.Distance(x.m_refPos, pos) <= Main.Instance.Config.General.FarMessageRange.Value);
                break;

            default:
                return;
        }

        if (type is MessageTypes.InWorld)
            RPC.ShowInWorldText(peers.Select(static x => x.m_uid), inWorldTextType, pos, message.RemoveRichTextTags());
        else
        {
            var msgType = type is MessageTypes.TopLeftNear or MessageTypes.TopLeftFar ? MessageHud.MessageType.TopLeft : MessageHud.MessageType.Center;
            foreach (var peer in peers)
                RPC.ShowMessage(peer.m_uid, msgType, message);
        }
    }

    protected static void ShowMessage(IEnumerable<Peer> peers, ExtendedZDO zdo, string message, MessageTypes type, DamageText.TextType inWorldTextType = DamageText.TextType.Normal)
        => ShowMessage(peers, zdo.GetPosition(), message, type, inWorldTextType);


    [Conditional("DEBUG")]
    protected static void DevShowMessage(ZDO zdo, string message, DamageText.TextType type = DamageText.TextType.Normal, [CallerFilePath] string callerFile = default!, [CallerLineNumber] int callerLineNo = default)
    {
#if DEBUG
        RPC.ShowInWorldText([0], type, zdo.GetPosition(), $"{Path.GetFileNameWithoutExtension(callerFile)} L{callerLineNo}: {message}");
#endif
    }

    protected static class RPC
    {
        public static void ShowMessage(long targetPeerId, MessageHud.MessageType type, string message)
        {
            /// Invoke <see cref="MessageHud.RPC_ShowMessage"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerId, "ShowMessage", (int)type, message);
        }

        //public static void ShowMessage(MessageHud.MessageType type, string message)
        //    => ShowMessage(ZRoutedRpc.Everybody, type, message);

        public static void ShowMessage(Peer peer, MessageHud.MessageType type, string message)
            => ShowMessage(peer.m_uid, type, message);

        public static void ShowMessage(IEnumerable<Peer> peers, MessageHud.MessageType type, string message)
        {
            foreach (var peer in peers)
                ShowMessage(peer, type, message);
        }

        public static void UseStamina(ExtendedZDO playerZdo, float value)
        {
            playerZdo.AssertIs<Player>();
            /// <see cref="Player.UseStamina(float)"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(playerZdo.GetOwner(), playerZdo.m_uid, "UseStamina", value);
        }

        public static void SendGlobalKeys(Peer peer, List<string> keys)
        {
            /// <see cref="ZoneSystem.SendGlobalKeys"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "GlobalKeys", keys);
        }

        public static void ShowInWorldText(IEnumerable<long> targetPeerIds, DamageText.TextType type, Vector3 pos, string text)
        {
            /// <see cref="DamageText.ShowText(DamageText.TextType, Vector3, string, bool)"/>
            ZPackage zPackage = new ZPackage();
            zPackage.Write((int)type);
            zPackage.Write(pos);
            zPackage.Write(text);
            zPackage.Write(false);
            foreach (var peer in targetPeerIds)
                ZRoutedRpc.instance.InvokeRoutedRPC(peer, "RPC_DamageText", zPackage);
        }

        //public static void ShowInWorldText(IEnumerable<Peer> peers, DamageText.TextType type, Vector3 pos, string text)
        //    => ShowInWorldText(peers.Where(static x => Vector3.Distance(x.m_refPos, pos) <= DamageText.instance.m_maxTextDistance).Select(static x => x.m_uid), type, pos, text);

        //public static void ShowInWorldText(DamageText.TextType type, Vector3 pos, string text)
        //    => ShowInWorldText([ZRoutedRpc.Everybody], type, pos, text);

        //public static void ShowInWorldText(Peer peer, DamageText.TextType type, Vector3 pos, string text)
        //    => ShowInWorldText([peer.m_uid], type, pos, text);

        static void TeleportPlayer(long targetPeerID, Vector3 pos, Quaternion rot, bool distantTeleport)
        {
            /// <see cref="Chat.TeleportPlayer(long, Vector3, Quaternion, bool)"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerID, "RPC_TeleportPlayer", pos, rot, distantTeleport);
        }

        public static void TeleportPlayer(Peer peer, Vector3 pos, Quaternion rot, bool distantTeleport)
            => TeleportPlayer(peer.m_uid, pos, rot, distantTeleport);

        public static void TeleportPlayer(ExtendedZDO player, Vector3 pos, Quaternion rot, bool distantTeleport)
        {
            player.AssertIs<Player>();
            /// <see cref="Player.TeleportTo(Vector3, Quaternion, bool)"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(player.GetOwner(), player.m_uid, "RPC_TeleportTo", pos, rot, distantTeleport);
        }

        public static void Remove(ExtendedZDO piece, bool blockDrop = false)
        {
            piece.AssertIs<Piece>();
            /// <see cref="WearNTear.RPC_Remove"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(piece.GetOwner(), piece.m_uid, "RPC_Remove", false);
        }

        public static void AddStatusEffect(ExtendedZDO character, int nameHash, bool resetTime = false, int itemLevel = 0, float skillLevel = 0f)
        {
            character.AssertIs<Character>();
            /// <see cref="SEMan.AddStatusEffect"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(character.GetOwner(), character.m_uid, "RPC_AddStatusEffect", [nameHash, resetTime, itemLevel, skillLevel]);
        }

        public static void RequestStack(ExtendedZDO container, ExtendedZDO player, long playerID = 0)
        {
            container.AssertIs<Container>();
            player.AssertIs<Player>();

            /// <see cref="Container.RPC_RequestStack"/>
            if (playerID is 0)
                playerID = player.Vars.GetPlayerID();
            InvokeRoutedRPCAsSender(player.GetOwner(), container.GetOwner(), container.m_uid, "RPC_RequestStack", [playerID]);
        }

        public static void StackResponse(ExtendedZDO container, bool granted)
        {
            container.AssertIs<Container>();

            /// <see cref="Container.RPC_StackResponse"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(container.GetOwner(), container.m_uid, "RPC_StackResponse", [granted]);
        }

        public static void TakeAllResponse(ExtendedZDO container, bool granted)
        {
            container.AssertIs<Container>();
            /// <see cref="Container.RPC_TakeAllRespons"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(container.GetOwner(), container.m_uid, "TakeAllRespons", [granted]);
        }

        public static void RequestStateChange(ExtendedZDO trap, int state)
        {
            trap.AssertIs<Trap>();

            /// <see cref="Trap.RPC_RequestStateChange"/>"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(trap.GetOwner(), trap.m_uid, "RPC_RequestStateChange", [state]);
        }

        public static void SetTamed(ExtendedZDO character, bool tamed)
        {
            character.AssertIs<Character>();

            /// <see cref="Character.SetTamed(bool)"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(character.GetOwner(), character.m_uid, "RPC_SetTamed", [tamed]);
        }

        public static void Damage(ExtendedZDO character, HitData hitData)
        {
            character.AssertIs<Character>();

            /// <see cref="Character.Damage(HitData)"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(character.GetOwner(), character.m_uid, "RPC_Damage", [hitData]);
        }

        public static void RequestOwn(ExtendedZDO itemDrop, [CallerFilePath] string callerFile = default!, [CallerLineNumber] int callerLineNo = default)
        {
            itemDrop.AssertIs<ItemDrop>();
            //DevShowMessage(itemDrop, "Ownership requested", DamageText.TextType.Normal, callerFile, callerLineNo);
            /// <see cref="ItemDrop.RequestOwn"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(itemDrop.GetOwner(), itemDrop.m_uid, "RPC_RequestOwn");
        }

        public static void RequestOpen(ExtendedZDO container, long playerID)
        {
            container.AssertIs<Container>();
            /// <see cref="Container.RPC_RequestOpen"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(container.GetOwner(), container.m_uid, "RequestOpen", [playerID]);
        }

        public static void OpenResponse(ExtendedZDO container, bool granted)
        {
            container.AssertIs<Container>();
            /// <see cref="Container.RPC_OpenRespons"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(container.GetOwner(), container.m_uid, "OpenRespons", [granted]);
        }

        static Action<ZRoutedRpc, long, ZDOID, string, object[], long>? __invokeRouteRPCAsSender;

        static void InvokeRoutedRPCAsSender(long senderPeerId, long targetPeerID, ZDOID targetZDO, string methodName, object[] parameters)
        {
            __invokeRouteRPCAsSender ??= GetDelegate();
            __invokeRouteRPCAsSender(ZRoutedRpc.instance, targetPeerID, targetZDO, methodName, parameters, senderPeerId);

            static Action<ZRoutedRpc, long, ZDOID, string, object[], long> GetDelegate()
            {
                var senderPeerIDField = GetField(static (RoutedRPCData x) => x.m_senderPeerID);
                var idField = typeof(ZRoutedRpc).GetField("m_id", BindingFlags.NonPublic | BindingFlags.Instance);

                var original = new Action<long, ZDOID, string, object[]>(ZRoutedRpc.instance.InvokeRoutedRPC).Method;
                var method = new DynamicMethodDefinition(original) { Name = "InvokeRoutedRPC_InjectSender" };
                typeof(DynamicMethodDefinition).GetProperty(nameof(DynamicMethodDefinition.OriginalMethod)).SetValue(method, null);
                method.Definition.Parameters.Add(new("senderPeerID", Mono.Cecil.ParameterAttributes.None, method.Module.ImportReference(typeof(long))));
                var instructions = method.Definition.Body.Instructions;

                var success = false;
                for (var i = 2; i < instructions.Count; i++)
                {
                    if (instructions[i].MatchStfld(senderPeerIDField) && instructions[i - 1].MatchLdfld(idField) && instructions[i - 2].OpCode == OpCodes.Ldarg_0)
                    {
                        instructions[i - 1] = method.GetILProcessor().Create(OpCodes.Ldarg, method.Definition.Parameters.Count - 1);
                        instructions.RemoveAt(i - 2);
                        success = true;
                        break;
                    }
                }

                if (!success)
                    throw new Exception("Failed");

                //foreach (var instruction in method.Definition.Body.Instructions)
                //    Main.Instance.Logger.DevLog($"{instruction.OpCode.Name}: {instruction.Operand}", LogLevel.Warning);

                var mi = method.Generate();
                return mi.CreateDelegate<Action<ZRoutedRpc, long, ZDOID, string, object[], long>>();

                static FieldInfo GetField<T, TField>(Expression<Func<T, TField>> expression)
                {
                    if (expression.Body is MemberExpression member)
                        return (FieldInfo)member.Member;
                    throw new ArgumentException();
                }
            }
        }
    }
}
