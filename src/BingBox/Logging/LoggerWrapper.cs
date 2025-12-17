using System;
using BepInEx.Logging;

namespace BingBox.Logging;

internal class LoggerWrapper
{
    private readonly ManualLogSource _logger;

    public LoggerWrapper(ManualLogSource logger)
    {
        _logger = logger;
    }

    public ManualLogSource Source => _logger;

    private string PrependTimestamp(object data)
    {
        return $"[{DateTime.Now:HH:mm:ss.fff}] {data}";
    }

    public void LogError(object data) => _logger.LogError(PrependTimestamp(data));
    public void LogFatal(object data) => _logger.LogFatal(PrependTimestamp(data));
    public void LogDebug(object data) => _logger.LogDebug(PrependTimestamp(data));
    public void LogMessage(object data) => _logger.LogMessage(PrependTimestamp(data));

    public void LogInfo(string data) => _logger.LogInfo(PrependTimestamp(data));
    public void LogWarning(string data) => _logger.LogWarning(PrependTimestamp(data));
    public void LogError(string data) => _logger.LogError(PrependTimestamp(data));
    public void LogFatal(string data) => _logger.LogFatal(PrependTimestamp(data));
    public void LogDebug(string data) => _logger.LogDebug(PrependTimestamp(data));
    public void LogMessage(string data) => _logger.LogMessage(PrependTimestamp(data));
}
