using Microsoft.Extensions.Logging;
using Shared.Logging.Helpers;
using Shared.Logging.Models;
using Shared.Logging.Models.FileLog;
using Shared.Logging.Writer;

namespace Shared.Logging.Loggers;

public class FileLogger(string categoryName, FileLoggerConfiguration options, FileLogChannelWriter writer) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);

        var logEntry = new LogEntryModel
        {
            Timestamp = DateTime.UtcNow,
            Level = LoggerHelper.GetLogLevelString(logLevel),
            Source = categoryName,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = message,
            Exception = LoggerHelper.ExtractExceptionDetail(exception),
            Enrichers = options.Enrichers,
            Properties = LoggerHelper.ExtractProperties(state),
        };

        writer.Write(logEntry);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        if (options.LogLevels.TryGetValue(categoryName, out var categoryLevel))
        {
            return logLevel >= categoryLevel;
        }

        return logLevel >= options.DefaultLogLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}