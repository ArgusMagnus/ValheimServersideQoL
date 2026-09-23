using ServersideQoL.Utilities;
using UnityEngine;
using static ServersideQoL.RPC;

namespace ServersideQoL;

partial class ServersideQoLZDO
{
  public ZDORpc RPC => new(this);

  public readonly ref struct ZDORpc(ServersideQoLZDO zdo)
  {
    readonly ServersideQoLZDO _zdo = zdo;

    public PlayerRpc Player => new(_zdo);
    public readonly ref struct PlayerRpc(ServersideQoLZDO zdo)
    {
      readonly ZDO _zdo = AssertAndGetZDO<Player>(zdo);

      /// <see cref="Player.UseStamina"/>
      public void UseStamina(float value)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.Player.UseStamina, parameters: [value]);

      /// <see cref="Player.TeleportTo(Vector3, Quaternion, bool)"/>
      public void TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.Player.TeleportTo, parameters: [pos, rot, distantTeleport]);

      public void AddMapPin(string pinName, Minimap.PinType pinType, Vector3 pos, bool showMap)
        => DiscoverLocationResponse(_zdo.GetOwner(), pinName, pinType, pos, showMap);
    }

    public CharacterRpc Character => new(_zdo);
    public readonly ref struct CharacterRpc(ServersideQoLZDO zdo)
    {
      readonly ZDO _zdo = AssertAndGetZDO<Character>(zdo);

      /// <see cref="SEMan.AddStatusEffect"/>
      public void AddStatusEffect(int nameHash, bool resetTime = false, int itemLevel = 0, float skillLevel = 0f, int variant = -1)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.Character.AddStatusEffect, parameters: [nameHash, resetTime, itemLevel, skillLevel, variant]);

      /// <see cref="Character.SetTamed(bool)"/>
      public void SetTamed(bool tamed)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.Character.SetTamed, parameters: [tamed]);

      /// <see cref="Character.Damage(HitData)"/>
      public void Damage(HitData hitData)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.Character.Damage, parameters: [hitData]);
    }

    public ContainerRpc Container => new(_zdo);
    public readonly ref struct ContainerRpc(ServersideQoLZDO zdo)
    {
      readonly ZDO _zdo = AssertAndGetZDO<Container>(zdo);

      /// <see cref="Container.RPC_RequestStack"/>
      public void RequestStack(ServersideQoLZDO player, PlayerID playerID = default)
      {
        player.AssertIs<Player>();

        if (playerID.Value is 0)
          playerID = player.Vars.GetPlayerID();
        InvokeRoutedRPCAsSender(player.ZDO.GetOwner(), _zdo.GetOwner(), _zdo.m_uid, RpcName.Container.RequestStack, parameters: [playerID.Value]);
      }

      /// <see cref="Container.RPC_StackResponse"/>
      public void StackResponse(bool granted)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.Container.StackResponse, parameters: [granted]);

      /// <see cref="Container.RPC_TakeAllRespons"/>
      public void TakeAllResponse(bool granted)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.Container.TakeAllResponse, parameters: [granted]);

      /// <see cref="Container.RPC_RequestOpen"/>
      public void RequestOpen(PlayerID playerID)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.Container.RequestOpen, parameters: [playerID.Value]);

      /// <see cref="Container.RPC_RequestOpen"/>
      public void RequestOpenFor(ServersideQoLZDO player)
      {
        player.AssertIs<Player>();
        InvokeRoutedRPCAsSender(player.ZDO.GetOwner(), _zdo.GetOwner(), _zdo.m_uid, RpcName.Container.RequestOpen, parameters: [player.Vars.GetPlayerID().Value]);
      }

      internal void RequestOwnershipRelease(PlayerID playerID = default)
      {
        var sender = PlayerID.GetModPlayerID().Value; // guaranteed to not be a valid peerID
        InvokeRoutedRPCAsSender(sender, _zdo.GetOwner(), _zdo.m_uid, RpcName.Container.RequestOpen, parameters: [playerID.Value]);
      }

      /// <see cref="Container.RPC_OpenRespons"/>
      public void OpenResponse(bool granted)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.Container.OpenResponse, parameters: [granted]);
    }

    public TeleportWorldRpc TeleportWorld => new(_zdo);
    public readonly ref struct TeleportWorldRpc(ServersideQoLZDO zdo)
    {
      readonly ZDO _zdo = AssertAndGetZDO<TeleportWorld>(zdo);

      /// <see cref="TeleportWorld.SetText"/>
      public void SetTag(string tag, string authorId)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.TeleportWorld.SetTag, parameters: [tag, authorId]);
    }

    public PieceRpc Piece => new(_zdo);
    public readonly ref struct PieceRpc(ServersideQoLZDO zdo)
    {
      readonly ZDO _zdo = AssertAndGetZDO<Piece>(zdo);

      /// <see cref="WearNTear.RPC_Remove"/>
      public void Remove(bool blockDrop = false)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.Piece.Remove, parameters: [false]);
    }

    public ItemDropRpc ItemDrop => new(_zdo);
    public readonly ref struct ItemDropRpc(ServersideQoLZDO zdo)
    {
      readonly ZDO _zdo = AssertAndGetZDO<ItemDrop>(zdo);

      /// <see cref="ItemDrop.RequestOwn"/>
      public void RequestOwn()
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.ItemDrop.RequestOwn);
    }

    public TrapRpc Trap => new(_zdo);
    public readonly ref struct TrapRpc(ServersideQoLZDO zdo)
    {
      readonly ZDO _zdo = AssertAndGetZDO<Trap>(zdo);

      /// <see cref="Trap.RPC_RequestStateChange"/>
      /// <param name="state"><see cref="Trap.TrapState"/></param>
      public void RequestStateChange(int state)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.Trap.RequestStateChange, parameters: [state]);
    }

    public MineRock5Rpc MineRock5 => new(_zdo);
    public readonly ref struct MineRock5Rpc(ServersideQoLZDO zdo)
    {
      readonly ZDO _zdo = AssertAndGetZDO<MineRock5>(zdo);

      /// <see cref="MineRock5.RPC_Damage"/>
      public void Damage(HitData hit, int hitAreaIndex)
        => InvokeRoutedRPC(_zdo.GetOwner(), _zdo.m_uid, RpcName.MineRock5.Damage, parameters: [hit, hitAreaIndex]);
    }

    static ZDO AssertAndGetZDO<T>(ServersideQoLZDO zdo)
      where T : MonoBehaviour
    {
      zdo.AssertIs<T>();
      return zdo.ZDO;
    }
  }
}
