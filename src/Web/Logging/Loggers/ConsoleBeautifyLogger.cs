using System.Text.Json;
using Web.Logging.Constants;
using Web.Logging.Models;
using Web.Logging.Models.ConsoleBeautify;

namespace Web.Logging.Loggers;

public sealed class ConsoleBeautifyLogger : ILogger
{
    private readonly string _categoryName;
    private readonly Dictionary<string, LogLevel> _categoryLogLevels;
    private readonly Dictionary<LogLevel, ConsoleColor> _logLevelColors;
    private readonly LogLevel _defaultLogLevel;
    private readonly JsonSerializerOptions _serializerOptions;

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

        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    public ConsoleBeautifyLogger(string categoryName, Action<ConsoleBeautifyLoggerConfiguration> configure)
    {
        _categoryName = categoryName;

        var configuration = new ConsoleBeautifyLoggerConfiguration();
        configure.Invoke(new ConsoleBeautifyLoggerConfiguration());

        _defaultLogLevel = configuration.LogLevels.TryGetValue(_categoryName, out var categoryLevel) ? categoryLevel : _defaultLogLevel;

        _categoryLogLevels = configuration.LogLevels;
        _logLevelColors = configuration.LogLevelColors;

        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };
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

        // JSON formatında log objesi oluştur
        var logEntry = new LogEntryModel
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel.ToString(),
            Source = _categoryName,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = message,
            Exception = ExtractExceptionDetail(exception),
            Properties = ExtractProperties(state),
        };

        var jsonMessage = JsonSerializer.Serialize(logEntry, LogEntryModelJsonContext.Default.LogEntryModel);

        WriteColoredMessage(logLevel, jsonMessage);
    }

    private static ExceptionDetailModel? ExtractExceptionDetail(Exception? exception, int depth = 0)
    {
        if (exception is null || depth > 2)
        {
            return null;
        }

        Dictionary<string, string>? data = null;
        if (exception.Data.Count > 0)
        {
            data = new Dictionary<string, string>();
            foreach (System.Collections.DictionaryEntry entry in exception.Data)
            {
                var key = entry.Key.ToString() ?? string.Empty;
                var value = entry.Value?.ToString() ?? string.Empty;
                data[key] = value;
            }
        }

        return new ExceptionDetailModel
        {
            Type = exception.GetType().FullName ?? "Unknown",
            Message = exception.Message,
            HResult = exception.HResult,
            Source = exception.Source,
            HelpLink = exception.HelpLink,
            StackTrace = exception.StackTrace,
            Data = data,
            InnerException = ExtractExceptionDetail(exception.InnerException, depth + 1),
        };
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

    private Dictionary<string, object?> ExtractProperties<TState>(TState state)
    {
        var properties = new Dictionary<string, object?>();

        if (state is IEnumerable<KeyValuePair<string, object>> stateProperties)
        {
            foreach (var property in stateProperties)
            {
                properties[property.Key] = property.Value;
            }
        }
        else
        {
            properties["State"] = JsonSerializer.Serialize(state, _serializerOptions);
        }

        return properties;
    }

    private void WriteColoredMessage(LogLevel logLevel, string message)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = _logLevelColors[logLevel];
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }
}