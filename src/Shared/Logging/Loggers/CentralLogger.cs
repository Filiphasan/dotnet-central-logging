using Microsoft.Extensions.Logging;
using Shared.Logging.Helpers;
using Shared.Logging.Models;
using Shared.Logging.Models.Central;
using Shared.Logging.Writer;

namespace Shared.Logging.Loggers;

public sealed class CentralLogger(string categoryName, CentralLogChannelWriter centralLogChannelWriter, CentralLoggerConfiguration config)
    : ILogger
{
    private readonly string _logKey = config.LogKey;
    private readonly bool _isSpecific = config.IsSpecific;
    private readonly string _exchangeName = config.ExchangeName;
    private readonly Dictionary<string, string> _enrichers = config.Enrichers;
    private readonly Dictionary<string, LogLevel> _categoryLogLevels = config.LogLevels;
    private readonly LogLevel _defaultLogLevel = config.LogLevels.GetValueOrDefault("Default", LogLevel.Information);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel is LogLevel.None)
        {
            return false;
        }

        if (_categoryLogLevels.TryGetValue(categoryName, out var categoryLevel))
        {
            return logLevel >= categoryLevel;
        }

        return logLevel >= _defaultLogLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);

        var logEntry = new LogEntryModel
        {
            LogKey = _logKey,
            Timestamp = DateTime.UtcNow,
            Level = LoggerHelper.GetLogLevelString(logLevel),
            Source = categoryName,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = message,
            Enrichers = _enrichers,
            Exception = LoggerHelper.ExtractExceptionDetail(exception),
            Properties = LoggerHelper.ExtractProperties(state),
        };
        centralLogChannelWriter.Write(logEntry);
    }
}