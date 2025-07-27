using Microsoft.Extensions.Logging;
using Shared.Logging.Helpers;
using Shared.Logging.Models;
using Shared.Logging.Models.Central;
using Shared.Logging.Writer;

namespace Shared.Logging.Loggers;

public sealed class CentralLogger : ILogger
{
    private readonly string _categoryName;
    private readonly CentralLogChannelWriter _centralLogChannelWriter;

    private readonly string _logKey;
    private readonly bool _isSpecific;
    private readonly string _exchangeName;
    private readonly Dictionary<string, string> _enrichers;
    private readonly Dictionary<string, LogLevel> _categoryLogLevels;
    private readonly LogLevel _defaultLogLevel;

    public CentralLogger(string categoryName, CentralLogChannelWriter centralLogChannelWriter, CentralLoggerConfiguration config)
    {
        _categoryName = categoryName;
        _centralLogChannelWriter = centralLogChannelWriter;

        _logKey = config.LogKey;
        _isSpecific = config.IsSpecific;
        _exchangeName = config.ExchangeName;
        _enrichers = config.Enrichers;
        _categoryLogLevels = config.LogLevels;
        _defaultLogLevel = config.LogLevels.GetValueOrDefault("Default", LogLevel.Information);
    }

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

        if (_categoryLogLevels.TryGetValue(_categoryName, out var categoryLevel))
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
            Level = logLevel.ToString(),
            Source = _categoryName,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = message,
            Enrichers = _enrichers,
            Exception = LoggerHelper.ExtractExceptionDetail(exception),
            Properties = LoggerHelper.ExtractProperties(state),
        };
        _centralLogChannelWriter.Write(new CentralLogRecord(_exchangeName, _isSpecific, logEntry));
    }
}