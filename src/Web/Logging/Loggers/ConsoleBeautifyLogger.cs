using System.Text.Json;
using Web.Logging.Constants;
using Web.Logging.Helpers;
using Web.Logging.Models;
using Web.Logging.Models.ConsoleBeautify;

namespace Web.Logging.Loggers;

public sealed class ConsoleBeautifyLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogLevel _defaultLogLevel;
    private readonly bool _isJsonFormatEnabled;
    private readonly Dictionary<string, string> _enrichers;
    private readonly Dictionary<string, LogLevel> _categoryLogLevels;
    private readonly Dictionary<LogLevel, ConsoleColor> _logLevelColors;

    public ConsoleBeautifyLogger(string categoryName, IConfiguration configuration)
    {
        _categoryName = categoryName;

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

    public ConsoleBeautifyLogger(string categoryName, Action<ConsoleBeautifyLoggerConfiguration> configure)
    {
        _categoryName = categoryName;

        var configuration = new ConsoleBeautifyLoggerConfiguration();
        configure(configuration);

        _defaultLogLevel = configuration.LogLevels.TryGetValue("Default", out var defaultLevel) ? defaultLevel : LogLevel.Information;
        _isJsonFormatEnabled = configuration.JsonFormatEnabled;

        _enrichers = configuration.Enrichers;
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
            Enrichers = _enrichers,
            Properties = _isJsonFormatEnabled ? LoggerHelper.ExtractProperties(state) : null,
        };

        if (_isJsonFormatEnabled)
        {
            WriteColoredJsonMessage(logLevel, logEntry);
        }
        else
        {
            WriteColoredMessage(logLevel, logEntry);
        }
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

    private void WriteColoredJsonMessage(LogLevel logLevel, LogEntryModel logEntry)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = _logLevelColors[logLevel];
        Console.WriteLine(JsonSerializer.Serialize(logEntry, LogEntryModelJsonContext.Default.LogEntryModel));
        Console.ForegroundColor = originalColor;
    }

    private void WriteColoredMessage(LogLevel logLevel, LogEntryModel logEntry)
    {
        ConsoleColor originalColor = Console.ForegroundColor;

        Console.ForegroundColor = _logLevelColors[logLevel];
        Console.WriteLine("-------------------------------------------------");
        Console.WriteLine($"[{logEntry.EventId,3}: {logLevel,-12} - {logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fffffff}]");
        
        Console.ForegroundColor = originalColor;
        Console.Write("      Enrichers - ");

        Console.ForegroundColor = _logLevelColors[logLevel];
        Console.Write($"{string.Join(", ", logEntry.Enrichers.Select(x => $"{x.Key}: {x.Value}"))}");

        Console.WriteLine();

        Console.ForegroundColor = originalColor;
        Console.Write($"      {logEntry.Source} - ");

        Console.ForegroundColor = _logLevelColors[logLevel];
        Console.Write($"{logEntry.Message}");

        Console.WriteLine();

        if (logEntry.Exception is not null)
        {
            Console.WriteLine($"      {logEntry.Exception.GetExceptionDetailedMessage()}");
        }

        Console.ForegroundColor = originalColor;
    }
}