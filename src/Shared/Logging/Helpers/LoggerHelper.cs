using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Logging.Constants;
using Shared.Logging.Models;
using Shared.Logging.Models.ConsoleBeautify;

namespace Shared.Logging.Helpers;

public static class LoggerHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public static ExceptionDetailModel? ExtractExceptionDetail(Exception? exception, int depth = 0)
    {
        if (exception is null || depth > 2)
        {
            return null;
        }

        return new ExceptionDetailModel
        {
            Type = exception.GetType().FullName ?? "Unknown",
            Message = exception.Message,
            HResult = exception.HResult,
            Source = exception.Source,
            HelpLink = exception.HelpLink,
            StackTrace = exception.StackTrace,
            InnerException = ExtractExceptionDetail(exception.InnerException, depth + 1),
        };
    }

    public static Dictionary<string, string?> ExtractProperties<TState>(TState state, Dictionary<string, string?>? properties = null, string stateName = "State")
    {
        properties ??= new Dictionary<string, string?>();

        if (state is IEnumerable<KeyValuePair<string, object?>> stateProperties)
        {
            foreach (var property in stateProperties)
            {
                if (property.Key.StartsWith('@'))
                {
                    var key = property.Key[1..];
                    properties[key] = JsonSerializer.Serialize(property.Value, SerializerOptions);
                }
                else
                {
                    properties[property.Key] = property.Value?.ToString();
                }
            }
        }
        else
        {
            properties[stateName] = JsonSerializer.Serialize(state, SerializerOptions);
        }

        return properties;
    }

    public static void LoadConsoleBeautifyOptionsFromConfiguration(IConfiguration configuration, ConsoleBeautifyLoggerConfiguration options)
    {
        LoadConsoleBeautifyLogLevelFromConfiguration(configuration, options);
        LoadConsoleBeautifyLogLevelColorsFromConfiguration(configuration, options);
    }

    private static void LoadConsoleBeautifyLogLevelFromConfiguration(IConfiguration configuration, ConsoleBeautifyLoggerConfiguration options)
    {
        var section = configuration.GetSection("Logging:ConsoleBeautify:LogLevel");
        foreach (var item in section.GetChildren())
        {
            options.LogLevels[item.Key] = GetLogLevel(item.Value);
        }
    }

    private static void LoadConsoleBeautifyLogLevelColorsFromConfiguration(IConfiguration configuration, ConsoleBeautifyLoggerConfiguration options)
    {
        var section = configuration.GetSection("Logging:ConsoleBeautify:Colors");
        foreach (var item in section.GetChildren())
        {
            SetConsoleColor(options.LogLevelColors, item.Value, GetLogLevel(item.Key));
        }
    }

    public static Dictionary<LogLevel, ConsoleColor> GetDefaultLogLevelColors()
    {
        return new Dictionary<LogLevel, ConsoleColor>
        {
            { LogLevel.Trace, ConsoleColor.Gray },
            { LogLevel.Debug, ConsoleColor.Cyan },
            { LogLevel.Information, ConsoleColor.Green },
            { LogLevel.Warning, ConsoleColor.Yellow },
            { LogLevel.Error, ConsoleColor.Red },
            { LogLevel.Critical, ConsoleColor.DarkMagenta },
            { LogLevel.None, ConsoleColor.White },
        };
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

    public static LogLevel GetLogLevel(string? logLevelString)
    {
        if (string.IsNullOrEmpty(logLevelString))
            return LogLevel.Information;

        return logLevelString switch
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

    public static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => LogLevelConstant.Trace,
            LogLevel.Debug => LogLevelConstant.Debug,
            LogLevel.Information => LogLevelConstant.Information,
            LogLevel.Warning => LogLevelConstant.Warning,
            LogLevel.Error => LogLevelConstant.Error,
            LogLevel.Critical => LogLevelConstant.Critical,
            LogLevel.None => LogLevelConstant.None,
            _ => LogLevelConstant.Information
        };
    }

    public static string GetFileLoggerPath(string baseFolder, DateTime date, bool createDirectory = true)
    {
        var folderPath = Path.Combine(AppContext.BaseDirectory, baseFolder, date.Year.ToString(), date.Month.ToString(), date.Day.ToString());
        if (createDirectory)
        {
            Directory.CreateDirectory(folderPath);
        }
        var filePath = Path.Combine(folderPath, $"log-{date:yyyyMMdd-HH}.log");
        return filePath;
    }
}