namespace ServersideQoL;

public readonly record struct PlayerID(long Value)
{
  /// <summary>
  /// The player ID used to identify ZDOs created/owned by this mod
  /// </summary>
  /// <remarks>
  /// Valheim generates player IDs with <see cref="Utils.GenerateUID"/> which never sets the upper 31 bits of the returned value.
  /// Thus using a mod-"player" ID that only uses the upper bits gives us the following advantages:
  /// - The mod player ID can never conflict with vanilla player IDs
  /// - The bit-wise combination of the mod-player ID with a vanilla player ID is also guaranteed to be unique.
  ///   This value can later be split again into mod-player ID and player ID without loss of information (useful e.g. for map table pins).
  /// </remarks>
  static readonly long __modPlayerID = unchecked((long)((ulong)ServersideQoLPlugin.PluginGuid.GetStableHashCode() << 33));
  const long ModPlayerIDMask = unchecked((long)(ulong.MaxValue << 33));

  public bool IsModPlayerID(out PlayerID lowerBits)
  {
    if ((Value & ModPlayerIDMask) == __modPlayerID)
    {
      lowerBits = new(Value & ~ModPlayerIDMask);
      return true;
    }
    lowerBits = default;
    return false;
  }

  public static PlayerID GetModPlayerID() => new(__modPlayerID);
  public static PlayerID GetModPlayerID(PlayerID lowerBits) => GetModPlayerID(lowerBits.Value);
  public static PlayerID GetModPlayerID(long lowerBits)
  {
    if ((lowerBits & ModPlayerIDMask) is not 0)
      throw new ArgumentOutOfRangeException(nameof(lowerBits));
    return new(__modPlayerID | lowerBits);
  }
}
