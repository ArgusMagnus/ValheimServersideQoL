using HarmonyLib;
using System.Reflection;

namespace ServersideQoL.ZDOExtender;

public delegate void ZDOPrefabChangedEventHandler(ZDO zdo, int oldPrefab, int newPrefab);
public delegate void ZDOEventHandler(ZDO zdo);

public interface IExtendedZDO
{
  ZDO ZDO { get; }

  //event ZDOPrefabChangedEventHandler? PrefabChanged;
  //event ZDOEventHandler? Created;
  event ZDOEventHandler? Destroyed;
  //event ZDOEventHandler? OwnerRevisionChanged;
  //event ZDOEventHandler? DataRevisionChanged;

  public static class Events
  {
    static bool _prefabChangedInitialized;
    static bool _destroyedInitialized;
    static bool _ownerRevisionChangedInitialized;
    static bool _dataRevisionChangedInitialized;
    static bool _createdInitialized;

    static ZDOPrefabChangedEventHandler? _prefabChanged;
    public static event ZDOPrefabChangedEventHandler? PrefabChanged
    {
      add
      {
        if (!_prefabChangedInitialized)
        {
          _prefabChangedInitialized = true;
          ZDOExtenderPlugin.HarmonyInstance.PatchAll(typeof(PrefabChangedPatches));
        }
        _prefabChanged += value;
      }
      remove { _prefabChanged -= value; }
    }

    static event ZDOEventHandler? _created;
    public static event ZDOEventHandler? Created
    {
      add
      {
        if (!_createdInitialized)
        {
          _createdInitialized = true;
          var original = typeof(ZDOMan).GetMethod(nameof(ZDOMan.CreateNewZDO), BindingFlags.Instance | BindingFlags.NonPublic)!;
          var postfix = ((Delegate)OnCreated).Method;
          ZDOExtenderPlugin.HarmonyInstance.Patch(original, postfix: new(postfix));
        }
        _created += value;
      }
      remove => _created -= value;
    }

    static event ZDOEventHandler? _destroyed;
    public static event ZDOEventHandler? Destroyed
    {
      add
      {
        EnsureDestroyedInitialized();
        _destroyed += value;
      }
      remove => _destroyed -= value;
    }

    static ZDOEventHandler? _ownerRevisionChanged;
    public static event ZDOEventHandler? OwnerRevisionChanged
    {
      add
      {
        if (!_ownerRevisionChangedInitialized)
        {
          _ownerRevisionChangedInitialized = true;
          var prop = typeof(ZDO).GetProperty(nameof(ZDO.OwnerRevision), BindingFlags.Instance | BindingFlags.Public)!;
          var setter = prop.SetMethod;
          var postfix = ((Delegate)OnOwnerRevisionChanged).Method;
          ZDOExtenderPlugin.HarmonyInstance.Patch(setter, postfix: new(postfix));
        }
        _ownerRevisionChanged += value;
      }
      remove => _ownerRevisionChanged -= value;
    }

    static ZDOEventHandler? _dataRevisionChanged;
    public static event ZDOEventHandler? DataRevisionChanged
    {
      add
      {
        if (!_dataRevisionChangedInitialized)
        {
          _dataRevisionChangedInitialized = true;
          var prop = typeof(ZDO).GetProperty(nameof(ZDO.DataRevision), BindingFlags.Instance | BindingFlags.Public)!;
          var setter = prop.SetMethod;
          var postfix = ((Delegate)OnDataRevisionChanged).Method;
          ZDOExtenderPlugin.HarmonyInstance.Patch(setter, postfix: new(postfix));
        }
        _dataRevisionChanged += value;
      }
      remove => _dataRevisionChanged -= value;
    }

    internal static void EnsureDestroyedInitialized()
    {
      if (!_destroyedInitialized)
      {
        _destroyedInitialized = true;
        ZDOMan.instance.m_onZDODestroyed += OnDestroyed;
      }
    }

    static void OnDestroyed(ZDO __instance)
    {
      var zdo = (ExtendedZDO)__instance;
      try
      {
        _destroyed?.Invoke(zdo);
        zdo._destroyed?.Invoke(zdo);
      }
      catch (Exception ex)
      {
        ZDOExtenderPlugin.Instance.Logger.LogError(ex);
        throw;
      }
      zdo._destroyed = null;
    }

    static void OnCreated(ZDO __result)
    {
      //var zdo = (ExtendedZDO)__result;
      try { _created?.Invoke(__result); }
      catch (Exception ex)
      {
        ZDOExtenderPlugin.Instance.Logger.LogError(ex);
        throw;
      }
    }

    static void OnOwnerRevisionChanged(ZDO __instance)
    {
      //var zdo = (ExtendedZDO)__instance;
      try { _ownerRevisionChanged?.Invoke(__instance); }
      catch (Exception ex)
      {
        ZDOExtenderPlugin.Instance.Logger.LogError(ex);
        throw;
      }
    }

    static void OnDataRevisionChanged(ZDO __instance)
    {
      //var zdo = (ExtendedZDO)__instance;
      try { _dataRevisionChanged?.Invoke(__instance); }
      catch (Exception ex)
      {
        ZDOExtenderPlugin.Instance.Logger.LogError(ex);
        throw;
      }
    }

    static class PrefabChangedPatches
    {
      [HarmonyTargetMethods]
      public static IEnumerable<MethodInfo> GetTargetMethods()
      {
        var zdo = new ZDO();
        yield return ((Delegate)zdo.SetPrefab).Method;
        yield return ((Delegate)zdo.Deserialize).Method;
        yield return ((Delegate)zdo.Load).Method;
        yield return ((Delegate)zdo.LoadOldFormat).Method;
        yield return ((Delegate)zdo.Reset).Method;
      }

      [HarmonyPostfix]
      public static void OnPrefabChanged(ZDO __instance)
      {
        var zdo = (ExtendedZDO)__instance;
        var oldPrefab = zdo._prevPrefab;
        var newPrefab = zdo.GetPrefab();
        if (oldPrefab == newPrefab)
          return;
        try { _prefabChanged?.Invoke(zdo, oldPrefab, newPrefab); }
        catch (Exception ex)
        {
          ZDOExtenderPlugin.Instance.Logger.LogError(ex);
          throw;
        }
        zdo._prevPrefab = newPrefab;
      }
    }
  }
}
