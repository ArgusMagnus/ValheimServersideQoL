namespace ServersideQoL.AutoMapTables.Tests;

[TestClass]
public sealed class WardAccessTests
{
  [TestMethod]
  public void NoWards_DoesNotRestrictAccess()
  {
    Assert.IsFalse(WardAccess.HasActiveWard<TestWard>(null, static ward => ward.Enabled));
    Assert.IsFalse(WardAccess.HasActiveWard([], static ward => ward.Enabled));
  }

  [TestMethod]
  public void InactiveWard_DoesNotRestrictAccess()
  {
    var wards = new[] { new TestWard(false) };

    Assert.IsFalse(WardAccess.HasActiveWard(wards, static ward => ward.Enabled));
  }

  [TestMethod]
  public void MultipleInactiveWards_DoNotRestrictAccess()
  {
    var wards = new[] { new TestWard(false), new TestWard(false) };

    Assert.IsFalse(WardAccess.HasActiveWard(wards, static ward => ward.Enabled));
  }

  [TestMethod]
  public void ActiveWard_RestrictsAccess()
  {
    var wards = new[] { new TestWard(true) };

    Assert.IsTrue(WardAccess.HasActiveWard(wards, static ward => ward.Enabled));
  }

  [TestMethod]
  public void MixedActiveAndInactiveWards_RestrictAccess()
  {
    var wards = new[] { new TestWard(false), new TestWard(true), new TestWard(false) };

    Assert.IsTrue(WardAccess.HasActiveWard(wards, static ward => ward.Enabled));
  }

  readonly record struct TestWard(bool Enabled);
}
