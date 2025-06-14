using BepInEx.Logging;
using System.Diagnostics;
using UnityEngine;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

static class ExtensionMethods
{
    public static ExtendedZDO? GetExtendedZDO(this ZDOMan instance, ZDOID id) => (ExtendedZDO?)instance.GetZDO(id);

    /// <see cref="ZNetScene.InActiveArea(Vector2i, Vector2i)"/>
    public static int GetActiveArea(this ZoneSystem instance) => instance.m_activeArea - 1;
    public static int GetLoadedArea(this ZoneSystem instance) => instance.m_activeArea;

    [Conditional("DEBUG")]
    public static void DevLog(this ManualLogSource logger, string text, LogLevel logLevel = LogLevel.Info)
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