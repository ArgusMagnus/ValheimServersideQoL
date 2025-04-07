using BepInEx.Logging;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

abstract class Processor(ManualLogSource logger, ModConfig cfg)
{
    static IReadOnlyList<Processor>? _defaultProcessors;
    public static IReadOnlyList<Processor> DefaultProcessors => _defaultProcessors ??= [.. typeof(Processor).Assembly.GetTypes()
        .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(Processor).IsAssignableFrom(x))
        .Select(x => (Processor)Activator.CreateInstance(x, args: [Main.Instance.Logger, Main.Instance.Config]))];

    static class InstanceCache<T> where T : Processor
    {
        public static T Instance { get; } = DefaultProcessors.OfType<T>().First();
    }
    public static T Instance<T>() where T : Processor => InstanceCache<T>.Instance;

    protected ManualLogSource Logger { get; } = logger;
    protected ModConfig Config { get; } = cfg;

    public bool DestroyZdo { get; protected set; }
    public bool RecreateZdo { get; protected set; }
    public bool UnregisterZdoProcessor { get; protected set; }

    readonly System.Diagnostics.Stopwatch _watch = new();
    protected HashSet<ExtendedZDO> PlacedPieces { get; } = new();
    static bool __initialized;

    public TimeSpan ProcessingTime => _watch.Elapsed;
    long _totalProcessingTimeTicks;
    public TimeSpan TotalProcessingTime => new(_totalProcessingTimeTicks + _watch.ElapsedTicks);

    public virtual void Initialize()
    {
        if (__initialized)
            return;
        __initialized = true;

        foreach (var zdo in ZDOMan.instance.GetObjectsByID().Values.Cast<ExtendedZDO>().Where(x => x.Vars.GetCreator() == Main.PluginGuidHash))
            zdo.Destroy();
    }

    protected void RegisterZdoDestroyed()
    {
        void OnDestroyed(ZDO zdo) => OnZdoDestroyed((ExtendedZDO)zdo);
        ZDOMan.instance.m_onZDODestroyed -= OnDestroyed;
        ZDOMan.instance.m_onZDODestroyed += OnDestroyed;
    }

    protected virtual void OnZdoDestroyed(ExtendedZDO zdo) { }

    public virtual void PreProcess()
    {
        _totalProcessingTimeTicks += _watch.ElapsedTicks;
        _watch.Reset();
    }

    public virtual bool ClaimExclusive(ExtendedZDO zdo) => PlacedPieces.Contains(zdo);

    protected abstract bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers);
    public void Process(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
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

    protected bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo)
        => CheckMinDistance(peers, zdo, Config.General.MinPlayerDistance.Value);

    protected static bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo, float minDistance)
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

    protected static class Prefabs
    {
        public static int GraustenFloor4x4 { get; } = "Piece_grausten_floor_4x4".GetStableHashCode();
        public static int GraustenWall4x2 { get; } = "Piece_grausten_wall_4x2".GetStableHashCode();
        public static int PortalWood { get; } = "portal_wood".GetStableHashCode();
        public static int Sconce { get; } = "piece_walltorch".GetStableHashCode();
        public static int DvergerGuardstone { get; } = "dverger_guardstone".GetStableHashCode();
        public static int Sign { get; } = "sign".GetStableHashCode();
        public static int Candle { get; } = "Candle_resin".GetStableHashCode();
    }

    protected static class RPC
    {
        public static void ShowMessage(long targetPeerId, MessageHud.MessageType type, string message)
        {
            /// Invoke <see cref="MessageHud.RPC_ShowMessage"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerId, "ShowMessage", (int)type, message);
        }

        public static void ShowMessage(MessageHud.MessageType type, string message)
            => ShowMessage(ZRoutedRpc.Everybody, type, message);

        public static void ShowMessage(ZNetPeer peer, MessageHud.MessageType type, string message)
            => ShowMessage(peer.m_uid, type, message);

        public static void ShowMessage(IEnumerable<ZNetPeer> peers, MessageHud.MessageType type, string message)
        {
            foreach (var peer in peers)
                ShowMessage(peer, type, message);
        }

        public static void UseStamina(ExtendedZDO playerZdo, float value)
        {
            /// <see cref="Player.UseStamina(float)"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(playerZdo.GetOwner(), playerZdo.m_uid, "UseStamina", value);
        }

        public static void SendGlobalKeys(ZNetPeer peer, List<string> keys)
        {
            /// <see cref="ZoneSystem.SendGlobalKeys"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "GlobalKeys", keys);
        }

        static void ShowInWorldText(IEnumerable<long> targetPeerIds, DamageText.TextType type, Vector3 pos, string text)
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

        public static void ShowInWorldText(IEnumerable<ZNetPeer> peers, DamageText.TextType type, Vector3 pos, string text)
            => ShowInWorldText(peers.Select(x => x.m_uid), type, pos, text);

        public static void ShowInWorldText(DamageText.TextType type, Vector3 pos, string text)
            => ShowInWorldText([ZRoutedRpc.Everybody], type, pos, text);

        public static void ShowInWorldText(ZNetPeer peer, DamageText.TextType type, Vector3 pos, string text)
            => ShowInWorldText([peer.m_uid], type, pos, text);

        static void TeleportPlayer(long targetPeerID, Vector3 pos, Quaternion rot, bool distantTeleport)
        {
            /// <see cref="Chat.TeleportPlayer(long, Vector3, Quaternion, bool)"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerID, "RPC_TeleportPlayer", pos, rot, distantTeleport);
        }

        public static void TeleportPlayer(ZNetPeer peer, Vector3 pos, Quaternion rot, bool distantTeleport)
            => TeleportPlayer(peer.m_uid, pos, rot, distantTeleport);

        public static void TeleportPlayer(ExtendedZDO player, Vector3 pos, Quaternion rot, bool distantTeleport)
        {
            /// <see cref="Player.TeleportTo(Vector3, Quaternion, bool)"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(player.GetOwner(), player.m_uid, "RPC_TeleportTo", pos, rot, distantTeleport);
        }

        public static void Remove(ExtendedZDO piece, bool blockDrop = false)
        {
            /// <see cref="WearNTear.RPC_Remove"/>
            ZRoutedRpc.instance.InvokeRoutedRPC(piece.GetOwner(), piece.m_uid, "RPC_Remove", false);
        }
    }
}
