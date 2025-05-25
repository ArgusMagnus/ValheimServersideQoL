using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using RoutedRPCData = ZRoutedRpc.RoutedRPCData;

namespace Valheim.ServersideQoL.Processors;

abstract class Processor
{
    static IReadOnlyList<Processor>? _defaultProcessors;
    public static IReadOnlyList<Processor> DefaultProcessors => _defaultProcessors ??= [.. typeof(Processor).Assembly.GetTypes()
        .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(Processor).IsAssignableFrom(x))
        .Select(x => (Processor)Activator.CreateInstance(x))];

    static class InstanceCache<T> where T : Processor
    {
        public static T Instance { get; } = DefaultProcessors.OfType<T>().First();
    }
    public static T Instance<T>() where T : Processor => InstanceCache<T>.Instance;

    protected ManualLogSource Logger => Main.Instance.Logger;
    protected ModConfig Config => Main.Instance.Config;

    public bool DestroyZdo { get; protected set; }
    public bool RecreateZdo { get; protected set; }
    public bool UnregisterZdoProcessor { get; protected set; }

    readonly System.Diagnostics.Stopwatch _watch = new();
    protected HashSet<ExtendedZDO> PlacedPieces { get; } = new();
    static bool __initialized;

    public TimeSpan ProcessingTime => _watch.Elapsed;
    long _totalProcessingTimeTicks;
    public TimeSpan TotalProcessingTime => new(_totalProcessingTimeTicks + _watch.ElapsedTicks);

    public virtual void Initialize(bool firstTime)
    {
        if (__initialized)
            return;
        __initialized = true;

        foreach (var zdo in ZDOMan.instance.GetObjectsByID().Values.Cast<ExtendedZDO>().Where(x => x.Vars.GetCreator() == Main.PluginGuidHash))
            zdo.Destroy();
    }

    //protected void RegisterZdoDestroyed()
    //{
    //    void OnDestroyed(ZDO zdo) => OnZdoDestroyed((ExtendedZDO)zdo);
    //    ZDOMan.instance.m_onZDODestroyed -= OnDestroyed;
    //    ZDOMan.instance.m_onZDODestroyed += OnDestroyed;
    //}

    //protected virtual void OnZdoDestroyed(ExtendedZDO zdo) { }

    public void PreProcess()
    {
        _totalProcessingTimeTicks += _watch.ElapsedTicks;
        _watch.Restart();
        PreProcessCore();
        _watch.Stop();
    }

    protected virtual void PreProcessCore() { }

    public virtual bool ClaimExclusive(ExtendedZDO zdo) => PlacedPieces.Contains(zdo);

    protected abstract bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers);
    public void Process(ExtendedZDO zdo, IEnumerable<Peer> peers)
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
        => peers.Min(x => Utils.DistanceSqr(x.m_refPos, zdo.GetPosition())) >= minDistance * minDistance;

    protected ExtendedZDO PlacePiece(Vector3 pos, int prefab, float rot)
    {
        var zdo = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, prefab);
        zdo.SetPrefab(prefab);
        zdo.Persistent = true;
        zdo.Distant = false;
        zdo.Type = ZDO.ObjectType.Default;
        zdo.SetRotation(Quaternion.Euler(0, rot, 0));
        zdo.Vars.SetCreator(Main.PluginGuidHash);
        zdo.Vars.SetHealth(-1);
        PlacedPieces.Add(zdo);
        zdo.Fields<Piece>().Set(x => x.m_canBeRemoved, false);
        zdo.Fields<WearNTear>().Set(x => x.m_noRoofWear, false).Set(x => x.m_noSupportWear, false).Set(x => x.m_health, -1);
        return zdo;
    }

    protected void DestroyPiece(ExtendedZDO zdo)
    {
        if (!PlacedPieces.Remove(zdo))
            throw new ArgumentException();
        zdo.Destroy();
    }

    protected static string ConvertToRegexPattern(string searchPattern)
    {
        searchPattern = Regex.Escape(searchPattern);
        searchPattern = searchPattern.Replace("\\*", ".*").Replace("\\?", ".?");
        return $"(?i)^{searchPattern}$";
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
                        __args.Insert(del.DataParameterIndex, data);
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
            var pars = Parameters.Select(x => x.ParameterType).ToList();
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

    protected static class Prefabs
    {
        public static int GraustenFloor4x4 { get; } = "Piece_grausten_floor_4x4".GetStableHashCode();
        public static int GraustenWall4x2 { get; } = "Piece_grausten_wall_4x2".GetStableHashCode();
        public static int PortalWood { get; } = "portal_wood".GetStableHashCode();
        public static int Sconce { get; } = "piece_walltorch".GetStableHashCode();
        public static int DvergerGuardstone { get; } = "dverger_guardstone".GetStableHashCode();
        public static int Sign { get; } = "sign".GetStableHashCode();
        public static int Candle { get; } = "Candle_resin".GetStableHashCode();
        public static int BlackmetalChest { get; } = "piece_chest_blackmetal".GetStableHashCode();
        public static int ReinforcedChest { get; } = "piece_chest".GetStableHashCode();
        public static int WoodChest { get; } = "piece_chest_wood".GetStableHashCode();
        public static int StandingIronTorch { get; } = "piece_groundtorch".GetStableHashCode();
        public static int StandingIronTorchGreen { get; } = "piece_groundtorch_green".GetStableHashCode();
        public static int StandingIronTorchBlue { get; } = "piece_groundtorch_blue".GetStableHashCode();
        //public static IReadOnlyList<int> Banners { get; } = [.. Enumerable.Range(1, 10).Select(static x => $"piece_banner{x:D2}".GetStableHashCode())];
    }

    protected static void ShowMessage(IEnumerable<Peer> peers, Vector3 pos, string message, MessageTypes type, DamageText.TextType inWorldTextType = DamageText.TextType.Normal)
    {
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
        //    => ShowInWorldText(peers.Where(x => Vector3.Distance(x.m_refPos, pos) <= DamageText.instance.m_maxTextDistance).Select(x => x.m_uid), type, pos, text);

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

        public static void RequestOwn(ExtendedZDO itemDrop)
        {
            itemDrop.AssertIs<ItemDrop>();
            /// <see cref="ItemDrop.RequestOwn"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(itemDrop.GetOwner(), itemDrop.m_uid, "RPC_RequestOwn");
        }

        public static void RequestOwn(ExtendedZDO container, long playerID)
        {
            container.AssertIs<Container>();
            /// <see cref="Container.RPC_RequestOpen"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(container.GetOwner(), container.m_uid, "RequestOpen", [playerID]);
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
