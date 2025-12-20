using System;
using Microsoft.Extensions.Logging;
using BepInEx.Logging;

namespace BingBox.Logging
{
    public class BepInExLoggerFactory : ILoggerFactory
    {
        private readonly ManualLogSource _logger;

        public BepInExLoggerFactory(ManualLogSource logger)
        {
            _logger = logger;
        }

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName)
        {
            return new BepInExLogger(_logger, categoryName);
        }

        public void Dispose() { }
    }

    public class BepInExLogger : ILogger
    {
        private readonly ManualLogSource _logger;
        private readonly string _categoryName;

        public BepInExLogger(ManualLogSource logger, string categoryName)
        {
            _logger = logger;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var bepInLevel = logLevel switch
            {
                Microsoft.Extensions.Logging.LogLevel.Trace => BepInEx.Logging.LogLevel.Info,
                Microsoft.Extensions.Logging.LogLevel.Debug => BepInEx.Logging.LogLevel.Info,
                Microsoft.Extensions.Logging.LogLevel.Information => BepInEx.Logging.LogLevel.Info,
                Microsoft.Extensions.Logging.LogLevel.Warning => BepInEx.Logging.LogLevel.Warning,
                Microsoft.Extensions.Logging.LogLevel.Error => BepInEx.Logging.LogLevel.Error,
                Microsoft.Extensions.Logging.LogLevel.Critical => BepInEx.Logging.LogLevel.Fatal,
                Microsoft.Extensions.Logging.LogLevel.None => BepInEx.Logging.LogLevel.None,
                _ => BepInEx.Logging.LogLevel.Info
            };

            string msg = formatter(state, exception);
            _logger.Log(bepInLevel, $"[{_categoryName}] {msg}");
        }
    }
}
