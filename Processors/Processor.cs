using BepInEx.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

abstract class Processor(ManualLogSource logger, ModConfig cfg)
{
    static IReadOnlyList<Processor>? _defaultProcessors;
    public static IReadOnlyList<Processor> DefaultProcessors => _defaultProcessors ??= typeof(Processor).Assembly.GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(Processor).IsAssignableFrom(x))
            .Select(x => (Processor)Activator.CreateInstance(x, args: [Main.Logger, Main.Config]))
            .ToList();

    protected ManualLogSource Logger { get; } = logger;
    protected ModConfig Config { get; } = cfg;

    public bool DestroyZdo { get; protected set; }
    public bool RecreateZdo { get; protected set; }
    public bool UnregisterZdoProcessor { get; protected set; }

    readonly System.Diagnostics.Stopwatch _watch = new();

    public TimeSpan ProcessingTime => _watch.Elapsed;
    long _totalProcessingTimeTicks;
    public TimeSpan TotalProcessingTime => new(_totalProcessingTimeTicks + _watch.ElapsedTicks);

    public virtual void Initialize() { }
    public virtual void PreProcess()
    {
        _totalProcessingTimeTicks += _watch.ElapsedTicks;
        _watch.Reset();
    }

    public virtual bool ClaimExclusive(ExtendedZDO zdo) => false;

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

    protected static class RPC
    {
        static void ShowMessage(long targetPeerId, MessageHud.MessageType type, string message)
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
    }
}
