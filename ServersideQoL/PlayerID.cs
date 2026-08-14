namespace ServersideQoL;

public readonly record struct PlayerID(long Value)
{
#if DEBUG
  static PlayerID()
  {
    for (var i = 0; i < 1000; i++)
      NormalizeVanillaUID(Utils.GenerateUID());
  }
#endif

  /// <summary>
  /// The player ID used to identify ZDOs created/owned by this mod
  /// </summary>
  /// <remarks>
  /// Valheim generates player IDs with <see cref="Utils.GenerateUID"/> which never sets the upper 31 bits of the returned value for positive values
  /// or always sets those bits for negative values (two's complement).
  /// Thus using a mod-"player" ID that only uses the upper bits gives us the following advantages:
  /// - The mod player ID can never conflict with vanilla player IDs
  /// - The bit-wise combination of the mod-player ID with a vanilla player ID is also guaranteed to be unique.
  ///   This value can later be split again into mod-player ID and player ID without loss of information (useful e.g. for map table pins).
  /// </remarks>
  static readonly long __modPlayerID = CreateModPlayerID();
  const long ModPlayerIDMask = unchecked(-1L << 33);
  const long SignBit33 = 1L << 32;

  public bool IsModPlayerID(out PlayerID vanillaPlayerId)
  {
    if ((Value & ModPlayerIDMask) == __modPlayerID)
    {
      vanillaPlayerId = new(Value & ~ModPlayerIDMask);
      if ((vanillaPlayerId.Value & SignBit33) is not 0) // negative value
        vanillaPlayerId = new(vanillaPlayerId.Value | ModPlayerIDMask);
      return true;
    }
    vanillaPlayerId = default;
    return false;
  }

  public bool IsModPlayerID(out uint lowerBits)
  {
    if ((Value & ModPlayerIDMask) == __modPlayerID)
    {
      lowerBits = (uint)Value;
      return true;
    }
    lowerBits = default;
    return false;
  }

  public static PlayerID GetModPlayerID() => new(__modPlayerID);
  public static PlayerID GetModPlayerID(PlayerID vanillaPlayerId) => GetModPlayerID(vanillaPlayerId.Value);
  public static PlayerID GetModPlayerID(uint lowerBits) => GetModPlayerID((long)lowerBits);
  
  static PlayerID GetModPlayerID(long lowerBits)
  {
    lowerBits = NormalizeVanillaUID(lowerBits);
    return new(__modPlayerID | lowerBits);
  }

  static long CreateModPlayerID()
  {
    var value = unchecked((long)((ulong)ServersideQoLPlugin.PluginGuid.GetStableHashCode() << 33));
    System.Diagnostics.Debug.Assert(value is not 0 && value is not ModPlayerIDMask && (value & ~ModPlayerIDMask) is 0);
    return value;
  }

  static long NormalizeVanillaUID(long value)
  {
    if (value < 0)
    {
      if ((value & ModPlayerIDMask) is not ModPlayerIDMask || (value & SignBit33) is 0)
        throw new ArgumentOutOfRangeException(nameof(value));
      value &= ~ModPlayerIDMask;
    }
    else if ((value & ModPlayerIDMask) is not 0)
      throw new ArgumentOutOfRangeException(nameof(value));
    return value;
  }
}
