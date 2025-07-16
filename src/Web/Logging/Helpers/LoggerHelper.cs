using System.Text.Json;
using Web.Logging.Models;

namespace Web.Logging.Helpers;

public static class LoggerHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static ExceptionDetailModel? ExtractExceptionDetail(Exception? exception, int depth = 0)
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
}