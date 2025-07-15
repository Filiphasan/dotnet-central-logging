using System.Text.Json;
using Web.Logging.Constants;
using Web.Logging.Helpers;
using Web.Logging.Models;
using Web.Logging.Models.ConsoleBeautify;

namespace Web.Logging.Loggers;

public sealed class ConsoleBeautifyLogger : ILogger
{
    private readonly string _categoryName;
    private readonly Dictionary<string, LogLevel> _categoryLogLevels;
    private readonly Dictionary<LogLevel, ConsoleColor> _logLevelColors;
    private readonly LogLevel _defaultLogLevel;

    public ConsoleBeautifyLogger(string categoryName, IConfiguration configuration)
    {
        _categoryName = categoryName;

        _defaultLogLevel = GetLogLevel(configuration["Logging:LogLevel:Default"]);

        _categoryLogLevels = new Dictionary<string, LogLevel>();
        LoadCategoryLogLevelsFromConfiguration(configuration);

        // Renk konfigürasyonu
        _logLevelColors = new Dictionary<LogLevel, ConsoleColor>
        {
            { LogLevel.Trace, GetConsoleColor(configuration["Logging:ConsoleBeautify:Colors:Trace"], ConsoleColor.Gray) },
            { LogLevel.Debug, GetConsoleColor(configuration["Logging:ConsoleBeautify:Colors:Debug"], ConsoleColor.Cyan) },
            { LogLevel.Information, GetConsoleColor(configuration["Logging:ConsoleBeautify:Colors:Information"], ConsoleColor.Green) },
            { LogLevel.Warning, GetConsoleColor(configuration["Logging:ConsoleBeautify:Colors:Warning"], ConsoleColor.Yellow) },
            { LogLevel.Error, GetConsoleColor(configuration["Logging:ConsoleBeautify:Colors:Error"], ConsoleColor.Red) },
            { LogLevel.Critical, GetConsoleColor(configuration["Logging:ConsoleBeautify:Colors:Critical"], ConsoleColor.DarkMagenta) }
        };
    }

    public ConsoleBeautifyLogger(string categoryName, Action<ConsoleBeautifyLoggerConfiguration> configure)
    {
        _categoryName = categoryName;

        var configuration = new ConsoleBeautifyLoggerConfiguration();
        configure.Invoke(new ConsoleBeautifyLoggerConfiguration());

        _defaultLogLevel = configuration.LogLevels.TryGetValue(_categoryName, out var categoryLevel) ? categoryLevel : _defaultLogLevel;

        _categoryLogLevels = configuration.LogLevels;
        _logLevelColors = configuration.LogLevelColors;
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
            Properties = LoggerHelper.ExtractProperties(state),
        };

        var jsonMessage = JsonSerializer.Serialize(logEntry, LogEntryModelJsonContext.Default.LogEntryModel);

        WriteColoredMessage(logLevel, jsonMessage);
    }

    private void LoadCategoryLogLevelsFromConfiguration(IConfiguration configuration)
    {
        var logLevelSection = configuration.GetSection("Logging:LogLevel");
        foreach (var item in logLevelSection.GetChildren())
        {
            if (item.Key != "Default")
            {
                _categoryLogLevels[item.Key] = GetLogLevel(item.Value);
            }
        }

        var customLevelsSection = configuration.GetSection("Logging:ConsoleBeautify:LogLevel");
        foreach (var item in customLevelsSection.GetChildren())
        {
            _categoryLogLevels[item.Key] = GetLogLevel(item.Value);
        }
    }

    private static LogLevel GetLogLevel(string? logLevelString)
    {
        if (string.IsNullOrEmpty(logLevelString))
            return LogLevel.Information;

        return logLevelString.ToUpper() switch
        {
            LogLevelConstant.Trace => LogLevel.Trace,
            LogLevelConstant.Debug => LogLevel.Debug,
            LogLevelConstant.Information => LogLevel.Information,
            LogLevelConstant.Warning => LogLevel.Warning,
            LogLevelConstant.Error => LogLevel.Error,
            LogLevelConstant.Critical => LogLevel.Critical,
            LogLevelConstant.None => LogLevel.None,
            _ => LogLevel.Information
        };
    }

    private static ConsoleColor GetConsoleColor(string? colorString, ConsoleColor defaultColor)
    {
        if (string.IsNullOrEmpty(colorString))
        {
            return defaultColor;
        }

        return colorString.ToUpper() switch
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
            _ => defaultColor
        };
    }

    private void WriteColoredMessage(LogLevel logLevel, string message)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = _logLevelColors[logLevel];
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }
}