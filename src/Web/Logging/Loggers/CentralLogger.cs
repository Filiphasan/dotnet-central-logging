using RabbitMQ.Client;
using Web.Common.Models.Messaging;
using Web.Logging.Helpers;
using Web.Logging.Models;
using Web.Services.Interfaces;

namespace Web.Logging.Loggers;

public sealed class CentralLogger : ILogger
{
    private readonly string _categoryName;
    private readonly IPublishService _publishService;

    private readonly string _logKey;
    private readonly bool _isSpecific;
    private readonly string _exchangeName;
    private readonly Dictionary<string, string> _enrichers;
    private readonly Dictionary<string, LogLevel> _categoryLogLevels;
    private readonly LogLevel _defaultLogLevel;

    public CentralLogger(string categoryName, IPublishService publishService, Action<CentralLoggerConfiguration> configure)
    {
        _categoryName = categoryName;
        _publishService = publishService;

        var configuration = new CentralLoggerConfiguration();
        configure(configuration);

        _logKey = configuration.LogKey;
        _isSpecific = configuration.IsSpecific;
        _exchangeName = configuration.ExchangeName;
        _enrichers = configuration.Enrichers;
        _categoryLogLevels = configuration.LogLevels;
        _defaultLogLevel = configuration.LogLevels.TryGetValue("Default", out var defaultLevel) ? defaultLevel : LogLevel.Information;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
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

        try
        {
            var ctSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var cToken = ctSource.Token;
            Task.Run(async () =>
            {
                var rkEnding = _isSpecific ? "specific" : "general";
                var publishMessageModel = new PublishMessageModel<LogEntryModel>
                {
                    Message = logEntry,
                    JsonTypeInfo = LogEntryModelJsonContext.Default.LogEntryModel,
                    Exchange =
                    {
                        Name = _exchangeName,
                        Type = ExchangeType.Topic,
                    },
                    RoutingKey = $"project-{logEntry.LogKey.ToLower()}-{rkEnding}",
                    TryCount = 5,
                };
                await _publishService.PublishAsync(publishMessageModel, cToken);
            }, cToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}