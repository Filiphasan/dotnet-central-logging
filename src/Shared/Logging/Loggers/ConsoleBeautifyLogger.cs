using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Logging.Constants;
using Shared.Logging.Helpers;
using Shared.Logging.Models;
using Shared.Logging.Models.ConsoleBeautify;
using Shared.Logging.Writer;

namespace Shared.Logging.Loggers;

public sealed class ConsoleBeautifyLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ConsoleBeautifyChannelWriter _consoleBeautifyChannelWriter;
    private readonly LogLevel _defaultLogLevel;
    private readonly bool _isJsonFormatEnabled;
    private readonly Dictionary<string, string> _enrichers;
    private readonly Dictionary<string, LogLevel> _categoryLogLevels;
    private readonly Dictionary<LogLevel, ConsoleColor> _logLevelColors;

    public ConsoleBeautifyLogger(string categoryName, IConfiguration configuration, ConsoleBeautifyChannelWriter consoleBeautifyChannelWriter)
    {
        _categoryName = categoryName;
        _consoleBeautifyChannelWriter = consoleBeautifyChannelWriter;

        _defaultLogLevel = LoggerHelper.GetLogLevel(configuration["Logging:LogLevel:Default"]);
        _isJsonFormatEnabled = configuration.GetValue<bool>("Logging:ConsoleBeautify:JsonFormatEnabled");

        _enrichers = new Dictionary<string, string>();
        LoggerHelper.LoadEnrichersFromConfiguration("Logging:ConsoleBeautify:Enrichers", configuration, _enrichers);

        _categoryLogLevels = new Dictionary<string, LogLevel>();
        LoggerHelper.LoadCategoryLogLevelsFromConfiguration("Logging:ConsoleBeautify:LogLevel" ,configuration, _categoryLogLevels);

        _logLevelColors = LoggerHelper.GetDefaultLogLevelColors();
        SetConsoleColor(_logLevelColors, configuration["Logging:ConsoleBeautify:Colors:Trace"], LogLevel.Trace);
        SetConsoleColor(_logLevelColors, configuration["Logging:ConsoleBeautify:Colors:Debug"], LogLevel.Debug);
        SetConsoleColor(_logLevelColors, configuration["Logging:ConsoleBeautify:Colors:Information"], LogLevel.Information);
        SetConsoleColor(_logLevelColors, configuration["Logging:ConsoleBeautify:Colors:Warning"], LogLevel.Warning);
        SetConsoleColor(_logLevelColors, configuration["Logging:ConsoleBeautify:Colors:Error"], LogLevel.Error);
        SetConsoleColor(_logLevelColors, configuration["Logging:ConsoleBeautify:Colors:Critical"], LogLevel.Critical);
        SetConsoleColor(_logLevelColors, configuration["Logging:ConsoleBeautify:Colors:None"], LogLevel.None);
    }

    public ConsoleBeautifyLogger(string categoryName, ConsoleBeautifyLoggerConfiguration config, ConsoleBeautifyChannelWriter consoleBeautifyChannelWriter)
    {
        _categoryName = categoryName;
        _consoleBeautifyChannelWriter = consoleBeautifyChannelWriter;

        _defaultLogLevel = config.LogLevels.GetValueOrDefault("Default", LogLevel.Information);
        _isJsonFormatEnabled = config.JsonFormatEnabled;

        _enrichers = config.Enrichers;
        _categoryLogLevels = config.LogLevels;
        _logLevelColors = config.LogLevelColors;
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
            return;

        var message = formatter(state, exception);

        var logEntry = new LogEntryModel
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel.ToString(),
            Source = _categoryName,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = message,
            Exception = LoggerHelper.ExtractExceptionDetail(exception),
            Enrichers = _enrichers,
            Properties = _isJsonFormatEnabled ? LoggerHelper.ExtractProperties(state) : null,
        };

        _consoleBeautifyChannelWriter.Write(new ConsoleBeautifyLogRecord(_logLevelColors[logLevel], _isJsonFormatEnabled, logEntry));
    }

    private static void SetConsoleColor(Dictionary<LogLevel, ConsoleColor> logLevelColors, string? colorString, LogLevel logLevel)
    {
        if (string.IsNullOrEmpty(colorString))
        {
            return;
        }

        ConsoleColor? foundedColor = colorString.ToUpper() switch
        {
            ConsoleColorConstant.Black => ConsoleColor.Black,
            ConsoleColorConstant.DarkBlue => ConsoleColor.DarkBlue,
            ConsoleColorConstant.DarkGreen => ConsoleColor.DarkGreen,
            ConsoleColorConstant.DarkCyan => ConsoleColor.DarkCyan,
            ConsoleColorConstant.DarkRed => ConsoleColor.DarkRed,
            ConsoleColorConstant.DarkMagenta => ConsoleColor.DarkMagenta,
            ConsoleColorConstant.DarkYellow => ConsoleColor.DarkYellow,
            ConsoleColorConstant.Gray => ConsoleColor.Gray,
            ConsoleColorConstant.DarkGray => ConsoleColor.DarkGray,
            ConsoleColorConstant.Blue => ConsoleColor.Blue,
            ConsoleColorConstant.Green => ConsoleColor.Green,
            ConsoleColorConstant.Cyan => ConsoleColor.Cyan,
            ConsoleColorConstant.Red => ConsoleColor.Red,
            ConsoleColorConstant.Magenta => ConsoleColor.Magenta,
            ConsoleColorConstant.Yellow => ConsoleColor.Yellow,
            ConsoleColorConstant.White => ConsoleColor.White,
            _ => null
        };

        if (foundedColor is not null)
        {
            logLevelColors[logLevel] = foundedColor.Value;
        }
    }
}