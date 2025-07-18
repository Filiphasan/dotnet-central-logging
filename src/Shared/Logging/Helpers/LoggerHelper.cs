using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Logging.Constants;
using Shared.Logging.Models;

namespace Shared.Logging.Helpers;

public static class LoggerHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

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

    public static Dictionary<string, object?> ExtractProperties<TState>(TState state)
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
            properties["State"] = JsonSerializer.Serialize(state, SerializerOptions);
        }

        return properties;
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

    public static void LoadEnrichersFromConfiguration(string section, IConfiguration configuration, Dictionary<string, string> enrichers)
    {
        var enrichersSection = configuration.GetSection(section);
        foreach (var item in enrichersSection.GetChildren())
        {
            enrichers[item.Key] = item.Value ?? string.Empty;
        }
    }

    public static void LoadCategoryLogLevelsFromConfiguration(string section, IConfiguration configuration, Dictionary<string, LogLevel> categoryLogLevels)
    {
        var logLevelSection = configuration.GetSection("Logging:LogLevel");
        foreach (var item in logLevelSection.GetChildren())
        {
            if (item.Key != "Default")
            {
                categoryLogLevels[item.Key] = GetLogLevel(item.Value);
            }
        }

        var customLevelsSection = configuration.GetSection("Logging:ConsoleBeautify:LogLevel");
        foreach (var item in customLevelsSection.GetChildren())
        {
            categoryLogLevels[item.Key] = GetLogLevel(item.Value);
        }
    }

    public static LogLevel GetLogLevel(string? logLevelString)
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
}