using Microsoft.Extensions.Logging;
using Shared.Logging.Helpers;
using Shared.Logging.Managers;
using Shared.Logging.Models;
using Shared.Logging.Models.Central;
using Shared.Logging.Writer;

namespace Shared.Logging.Loggers;

public sealed class CentralLogger(string categoryName, CentralLogChannelWriter centralLogChannelWriter, CentralLoggerConfiguration config, ILogScopeManager logScopeManager)
    : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return logScopeManager.PushState(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel is LogLevel.None)
        {
            return false;
        }

        if (config.LogLevels.TryGetValue(categoryName, out var categoryLevel))
        {
            return logLevel >= categoryLevel;
        }

        return logLevel >= config.DefaultLogLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var properties = new Dictionary<string, string?>();
        var message = formatter(state, exception);
        properties = LoggerHelper.ExtractProperties(state, properties);
        var messageTemplate = properties.GetValueOrDefault("{OriginalFormat}", null);
        properties.Remove("{OriginalFormat}");
        properties = logScopeManager.GetScopeProperties(properties);

        var logEntry = new LogEntryModel
        {
            LogKey = config.LogKey,
            Timestamp = DateTime.UtcNow,
            Level = LoggerHelper.GetLogLevelString(logLevel),
            Source = categoryName,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = message,
            MessageTemplate = messageTemplate,
            Enrichers = config.Enrichers,
            Exception = LoggerHelper.ExtractExceptionDetail(exception),
            Properties = properties,
        };
        centralLogChannelWriter.Write(logEntry);
    }
}