using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static ZRoutedRpc;

namespace ServersideQoL;

public static class RPC
{
  public static void ShowMessage(long targetPeerId, MessageHud.MessageType type, string message)
  {
    /// Invoke <see cref="MessageHud.RPC_ShowMessage"/>
    InvokeRoutedRPC(targetPeerId, "ShowMessage", parameters: [(int)type, message]);
  }

  //public static void ShowMessage(MessageHud.MessageType type, string message)
  //    => ShowMessage(ZRoutedRpc.Everybody, type, message);

  public static void ShowMessage(Peer peer, MessageHud.MessageType type, string message)
      => ShowMessage(peer.ZNetPeer.m_uid, type, message);

  public static void ShowMessage(IEnumerable<Peer> peers, MessageHud.MessageType type, string message)
  {
    foreach (var peer in peers)
      ShowMessage(peer, type, message);
  }

  public static void UseStamina(ServersideQoLZDO playerZdo, float value)
  {
    playerZdo.AssertIs<Player>();
    /// <see cref="Player.UseStamina(float)"/>
    InvokeRoutedRPC(playerZdo.ZDO.GetOwner(), playerZdo.ZDO.m_uid, "UseStamina", parameters: [value]);
  }

  public static void SendGlobalKeys(Peer peer, List<string> keys)
  {
    /// <see cref="ZoneSystem.SendGlobalKeys"/>
    InvokeRoutedRPC(peer.ZNetPeer.m_uid, "GlobalKeys", parameters: [keys]);
  }

  public static void ShowInWorldText(IEnumerable<long> targetPeerIds, DamageText.TextType type, Vector3 pos, string text)
  {
    /// <see cref="DamageText.ShowText(DamageText.TextType, Vector3, string, bool)"/>
    ZPackage zPackage = new();
    zPackage.Write((int)type);
    zPackage.Write(pos);
    zPackage.Write(text);
    zPackage.Write(false);
    foreach (var peer in targetPeerIds)
      InvokeRoutedRPC(peer, "RPC_DamageText", parameters: [zPackage]);
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
    InvokeRoutedRPC(targetPeerID, "RPC_TeleportPlayer", parameters: [pos, rot, distantTeleport]);
  }

  public static void TeleportPlayer(Peer peer, Vector3 pos, Quaternion rot, bool distantTeleport)
      => TeleportPlayer(peer.ZNetPeer.m_uid, pos, rot, distantTeleport);

  public static void TeleportPlayer(ServersideQoLZDO player, Vector3 pos, Quaternion rot, bool distantTeleport)
  {
    player.AssertIs<Player>();
    /// <see cref="Player.TeleportTo(Vector3, Quaternion, bool)"/>
    InvokeRoutedRPC(player.ZDO.GetOwner(), player.ZDO.m_uid, "RPC_TeleportTo", parameters: [pos, rot, distantTeleport]);
  }

  public static void Remove(ServersideQoLZDO piece, bool blockDrop = false)
  {
    piece.AssertIs<Piece>();
    /// <see cref="WearNTear.RPC_Remove"/>
    InvokeRoutedRPC(piece.ZDO.GetOwner(), piece.ZDO.m_uid, "RPC_Remove", parameters: [false]);
  }

  public static void AddStatusEffect(ServersideQoLZDO character, int nameHash, bool resetTime = false, int itemLevel = 0, float skillLevel = 0f)
  {
    character.AssertIs<Character>();
    /// <see cref="SEMan.AddStatusEffect"/>
    InvokeRoutedRPC(character.ZDO.GetOwner(), character.ZDO.m_uid, "RPC_AddStatusEffect", parameters: [nameHash, resetTime, itemLevel, skillLevel]);
  }

  public static void RequestStack(ServersideQoLZDO container, ServersideQoLZDO player, PlayerID playerID = default)
  {
    container.AssertIs<Container>();
    player.AssertIs<Player>();

    /// <see cref="Container.RPC_RequestStack"/>
    if (playerID.Value is 0)
      playerID = player.Vars.GetPlayerID();
    InvokeRoutedRPCAsSender(player.ZDO.GetOwner(), container.ZDO.GetOwner(), container.ZDO.m_uid, "RPC_RequestStack", parameters: [playerID.Value]);
  }

  public static void StackResponse(ServersideQoLZDO container, bool granted)
  {
    container.AssertIs<Container>();

    /// <see cref="Container.RPC_StackResponse"/>
    InvokeRoutedRPC(container.ZDO.GetOwner(), container.ZDO.m_uid, "RPC_StackResponse", parameters: [granted]);
  }

  public static void TakeAllResponse(ServersideQoLZDO container, bool granted)
  {
    container.AssertIs<Container>();
    /// <see cref="Container.RPC_TakeAllRespons"/>
    InvokeRoutedRPC(container.ZDO.GetOwner(), container.ZDO.m_uid, "TakeAllRespons", parameters: [granted]);
  }

  public static void RequestStateChange(ServersideQoLZDO trap, int state)
  {
    trap.AssertIs<Trap>();

    /// <see cref="Trap.RPC_RequestStateChange"/>"/>
    InvokeRoutedRPC(trap.ZDO.GetOwner(), trap.ZDO.m_uid, "RPC_RequestStateChange", parameters: [state]);
  }

  public static void SetTamed(ServersideQoLZDO character, bool tamed)
  {
    character.AssertIs<Character>();

    /// <see cref="Character.SetTamed(bool)"/>
    InvokeRoutedRPC(character.ZDO.GetOwner(), character.ZDO.m_uid, "RPC_SetTamed", parameters: [tamed]);
  }

  public static void Damage(ServersideQoLZDO character, HitData hitData)
  {
    character.AssertIs<Character>();

    /// <see cref="Character.Damage(HitData)"/>
    InvokeRoutedRPC(character.ZDO.GetOwner(), character.ZDO.m_uid, "RPC_Damage", parameters: [hitData]);
  }

  public static void RequestOwn(ServersideQoLZDO itemDrop, [CallerFilePath] string callerFile = default!, [CallerLineNumber] int callerLineNo = default)
  {
    itemDrop.AssertIs<ItemDrop>();
    //DevShowMessage(itemDrop, "Ownership requested", DamageText.TextType.Normal, callerFile, callerLineNo);
    /// <see cref="ItemDrop.RequestOwn"/>
    InvokeRoutedRPC(itemDrop.ZDO.GetOwner(), itemDrop.ZDO.m_uid, "RPC_RequestOwn");
  }

  public static void RequestOpen(ServersideQoLZDO container, PlayerID playerID)
  {
    container.AssertIs<Container>();
    /// <see cref="Container.RPC_RequestOpen"/>
    InvokeRoutedRPC(container.ZDO.GetOwner(), container.ZDO.m_uid, "RequestOpen", parameters: [playerID.Value]);
  }

  public static void RequestOpenFor(ServersideQoLZDO player, ServersideQoLZDO container)
  {
    player.AssertIs<Player>();
    container.AssertIs<Container>();
    /// <see cref="Container.RPC_RequestOpen"/>
    InvokeRoutedRPCAsSender(player.ZDO.GetOwner(), container.ZDO.GetOwner(), container.ZDO.m_uid, "RequestOpen", parameters: [player.Vars.GetPlayerID()]);
  }

  public static void OpenResponse(ServersideQoLZDO container, bool granted)
  {
    container.AssertIs<Container>();
    /// <see cref="Container.RPC_OpenRespons"/>
    InvokeRoutedRPC(container.ZDO.GetOwner(), container.ZDO.m_uid, "OpenRespons", parameters: [granted]);
  }

  public static void DamageMineRock5(ServersideQoLZDO minerock5, HitData hit, int hitAreaIndex)
  {
    minerock5.AssertIs<MineRock5>();
    /// <see cref="MineRock5.RPC_Damage"/>
    InvokeRoutedRPC(minerock5.ZDO.GetOwner(), minerock5.ZDO.m_uid, "RPC_Damage", parameters: [hit, hitAreaIndex]);
  }

  static readonly Dictionary<string, int> __invokeCounters = [];
  static readonly Dictionary<string, int> __invokeAsSenderCounters = [];
  static int __invokeTotalCounter;

  static void InvokeRoutedRPC(long targetPeerId, string methodName, object[]? parameters = null)
  {
    if (Config.Instance.DiagnosticLogs.Value)
    {
      __invokeCounters.TryGetValue(methodName, out var count);
      __invokeCounters[methodName] = ++count;
      __invokeTotalCounter++;
      if (count % 10 is 0)
        ServersideQoLPlugin.Logger.LogInfo($"{nameof(InvokeRoutedRPC)}: {methodName}: {count} of {__invokeTotalCounter} ({(float)count / __invokeTotalCounter:P0})");
    }
    ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerId, methodName, parameters ?? []);
  }

  static void InvokeRoutedRPC(long targetPeerId, ZDOID targetZDO, string methodName, object[]? parameters = null)
  {
    if (Config.Instance.DiagnosticLogs.Value)
    {
      __invokeCounters.TryGetValue(methodName, out var count);
      __invokeCounters[methodName] = ++count;
      __invokeTotalCounter++;
      if (count % 10 is 0)
        ServersideQoLPlugin.Logger.LogInfo($"{nameof(InvokeRoutedRPC)}: {methodName}: {count} of {__invokeTotalCounter} ({(float)count / __invokeTotalCounter:P0})");
    }
    ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerId, targetZDO, methodName, parameters ?? []);
  }

  static Action<ZRoutedRpc, long, ZDOID, string, object[], long>? __invokeRouteRPCAsSender;

  static void InvokeRoutedRPCAsSender(long senderPeerId, long targetPeerID, ZDOID targetZDO, string methodName, object[] parameters)
  {
    if (Config.Instance.DiagnosticLogs.Value)
    {
      __invokeAsSenderCounters.TryGetValue(methodName, out var count);
      __invokeAsSenderCounters[methodName] = ++count;
      __invokeTotalCounter++;
      if (count % 10 is 0)
        ServersideQoLPlugin.Logger.LogInfo($"{nameof(InvokeRoutedRPCAsSender)}: {methodName}: {count} of {__invokeTotalCounter} ({(float)count / __invokeTotalCounter:P0})");
    }

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

  public static class Intercept
  {
    static List<object?> __args = [];
    static readonly Dictionary<int, object?[]> __argArrays = [];
    static int __loopCounter;

    static bool HandleRoutedRPCPrefix(RoutedRPCData data)
    {
      if (__methods.TryGetValue(data.m_methodHash, out var rpcMethod))
      {
        ServersideQoLZDO? zdo = null;
        for (int i = 0; i < rpcMethod.Delegates.Count; i++)
        {
          var del = rpcMethod.Delegates[i];
          try
          {
            __args.Clear();
            ZRpc.Deserialize(del.Parameters, data.m_parameters, ref __args);
            data.m_parameters.SetPos(0);
            if (del.DataParameterIndex < del.ZdoParameterIndex)
            {
              if (del.DataParameterIndex > -1)
                __args.Insert(del.DataParameterIndex, data);
              if (del.ZdoParameterIndex > -1)
                __args.Insert(del.ZdoParameterIndex, zdo ??= ZDOMan.instance.GetZDO(data.m_targetZDO).ServersideQoLZDO);
            }
            else
            {
              if (del.ZdoParameterIndex > -1)
                __args.Insert(del.ZdoParameterIndex, zdo ??= ZDOMan.instance.GetZDO(data.m_targetZDO).ServersideQoLZDO);
              if (del.DataParameterIndex > -1)
                __args.Insert(del.DataParameterIndex, data);
            }

            if (!__argArrays.TryGetValue(__args.Count, out var args))
              __argArrays.Add(__args.Count, args = [.. __args]);
            else
              __args.CopyTo(args);

            __loopCounter++;
            if (__loopCounter > 1)
              ServersideQoLPlugin.Logger.DevLog($"{rpcMethod.Name}: Loop Counter: {__loopCounter}");
            var result = del.Delegate.DynamicInvoke(args);
            __loopCounter--;

            if (result is bool success && !success)
              return false;
          }
          catch (Exception ex)
          {
            ServersideQoLPlugin.Logger.LogError($"{rpcMethod.Name}: {del.Delegate.Method.DeclaringType.Name}.{del.Delegate.Method.Name}: {ex}");
            ServersideQoLPlugin.Logger.LogError($"Arguments: {string.Join(", ", __args.Select(static (x, i) => $"{i}: {x?.GetType().Name}"))}");
            rpcMethod.Delegates.RemoveAt(i--);
            if (rpcMethod.Delegates.Count is 0 && __methods.Remove(data.m_methodHash) && __methods.Count is 0)
              ServersideQoLPlugin.HarmonyInstance.Unpatch(__handleRoutedRPCMethod, __handleRoutedRPCPrefix);
          }
        }
      }
      else if (__methods.Count is 0)
      {
        ServersideQoLPlugin.HarmonyInstance.Unpatch(__handleRoutedRPCMethod, __handleRoutedRPCPrefix);
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
        ZdoParameterIndex = pars.IndexOf(typeof(ServersideQoLZDO));
      }
    }

    sealed record RpcMethod(string Name, List<RpcDelegate> Delegates);
    static readonly Dictionary<int, RpcMethod> __methods = [];
    static readonly MethodInfo __handleRoutedRPCMethod = typeof(ZRoutedRpc).GetMethod("HandleRoutedRPC", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly MethodInfo __handleRoutedRPCPrefix = new Func<RoutedRPCData, bool>(HandleRoutedRPCPrefix).Method;

    public static void UpdateInterception(string methodName, Delegate interceptor, bool enable)
    {
      var patched = __methods.Count is not 0;
      var methodHash = methodName.GetStableHashCode();
      if (!__methods.TryGetValue(methodHash, out var rpcMethod) && enable)
        __methods.Add(methodHash, rpcMethod = new(methodName, []));
      if (enable)
      {
        if (!rpcMethod.Delegates.Any(x => x.Delegate == interceptor))
          rpcMethod.Delegates.Add(new(interceptor));
      }
      else if (rpcMethod is not null)
      {
        var idx = rpcMethod.Delegates.FindIndex(x => x.Delegate == interceptor);
        if (idx > -1)
        {
          rpcMethod.Delegates.RemoveAt(idx);
          if (rpcMethod.Delegates.Count is 0)
            __methods.Remove(methodHash);
        }
      }

      if (!patched && __methods.Count > 0)
        ServersideQoLPlugin.HarmonyInstance.Patch(__handleRoutedRPCMethod, prefix: new(__handleRoutedRPCPrefix));
    }
  }
}
