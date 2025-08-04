using BepInEx.Configuration;
using BepInEx.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Valheim.ServersideQoL;

static class ExtensionMethods
{
    public static ExtendedZDO? GetExtendedZDO(this ZDOMan instance, ZDOID id) => (ExtendedZDO?)instance.GetZDO(id);

    /// <see cref="ZNetScene.InActiveArea(Vector2i, Vector2i)"/>
    public static int GetActiveArea(this ZoneSystem instance) => instance.m_activeArea - 1;
    public static int GetLoadedArea(this ZoneSystem instance) => instance.m_activeArea;

    public static ConfigEntry<T> BindEx<T>(this ConfigFile config, string section, T defaultValue, string description, [CallerMemberName]string key = default!)
        => config.Bind(section, key, defaultValue, description);

    public static ConfigEntry<T> BindEx<T>(this ConfigFile config, string section, T defaultValue, string description, AcceptableValueBase? acceptableValues, [CallerMemberName] string key = default!)
        => config.Bind(section, key, defaultValue, new ConfigDescription(description, acceptableValues));

    [Conditional("DEBUG")]
    public static void DevLog(this ManualLogSource logger, string text, LogLevel logLevel = LogLevel.Warning)
    {
#if DEBUG
        logger.Log(logLevel, text);
#endif
    }

    [Conditional("DEBUG")]
    public static void AssertIs<T>(this ExtendedZDO zdo) where T : MonoBehaviour
    {
#if DEBUG
        if (zdo.PrefabInfo.Prefab.GetComponentInChildren<T>() is null)
            throw new ArgumentException();
#endif
    }
}