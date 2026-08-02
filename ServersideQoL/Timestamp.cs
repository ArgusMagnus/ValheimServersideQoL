using UnityEngine;

namespace ServersideQoL;

public readonly struct Timestamp : IEquatable<Timestamp>, IComparable<Timestamp>
{
  readonly float _realtimeSinceStartup;

  Timestamp(float realtimeSinceStartup) => _realtimeSinceStartup = realtimeSinceStartup;
  public static Timestamp Now => new(Time.realtimeSinceStartup);

  public Timestamp AddSeconds(float seconds) => new(_realtimeSinceStartup + seconds);

  public bool Equals(Timestamp other) => _realtimeSinceStartup == other._realtimeSinceStartup;
  public int CompareTo(Timestamp other) => _realtimeSinceStartup.CompareTo(other._realtimeSinceStartup);
  public override bool Equals(object obj) => obj is Timestamp other && Equals(other);
  public override int GetHashCode() => _realtimeSinceStartup.GetHashCode();

  public static bool operator ==(Timestamp left, Timestamp right) => left._realtimeSinceStartup == right._realtimeSinceStartup;
  public static bool operator !=(Timestamp left, Timestamp right) => left._realtimeSinceStartup != right._realtimeSinceStartup;
  public static bool operator <(Timestamp left, Timestamp right) => left._realtimeSinceStartup < right._realtimeSinceStartup;
  public static bool operator <=(Timestamp left, Timestamp right) => left._realtimeSinceStartup <= right._realtimeSinceStartup;
  public static bool operator >(Timestamp left, Timestamp right) => left._realtimeSinceStartup > right._realtimeSinceStartup;
  public static bool operator >=(Timestamp left, Timestamp right) => left._realtimeSinceStartup >= right._realtimeSinceStartup;
}
