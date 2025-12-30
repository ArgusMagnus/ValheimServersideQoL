using BepInEx.Logging;
using System.Diagnostics;
using UnityEngine;

namespace Valheim.ServersideQoL;

public sealed class Logger(string sourceName) : ILogSource, IDisposable
{
    readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource(sourceName);

    public string SourceName => _logger.SourceName;

    public event EventHandler<LogEventArgs> LogEvent
    {
        add => _logger.LogEvent += value;
        remove => _logger.LogEvent -= value;
    }

    public void Dispose() => _logger.Dispose();

    public void Log(LogLevel level, object data)
    {
        const double OneDay = 60 * 60 * 24;
        var seconds = Time.realtimeSinceStartupAsDouble;
        var ts = TimeSpan.FromSeconds(seconds);
        if (seconds < OneDay)
            _logger.Log(level, $@"[{ts:hh\:mm\:ss\.fff}] {data}");
        else
            _logger.Log(level, $@"[{ts:d\.hh\:mm\:ss\.fff}] {data}");
    }

    public void LogFatal(object data) => Log(LogLevel.Fatal, data);
    public void LogError(object data) => Log(LogLevel.Error, data);
    public void LogWarning(object data) => Log(LogLevel.Warning, data);
    public void LogMessage(object data) => Log(LogLevel.Message, data);
    public void LogInfo(object data) => Log(LogLevel.Info, data);
    public void LogDebug(object data) => Log(LogLevel.Debug, data);

    [Conditional("DEBUG")]
    public void DevLog(string text, LogLevel logLevel = LogLevel.Warning) => Log(logLevel, text);
}
