using Microsoft.Extensions.Logging;
using Shared.Logging.Helpers;

namespace Shared.Logging.Models.ConsoleBeautify;

public class ConsoleBeautifyLoggerConfiguration
{
    public bool IsConfigured { get; set; }
    public bool JsonFormatEnabled { get; private set; }
    public Dictionary<string, string> Enrichers { get; } = new();
    public Dictionary<string, LogLevel> LogLevels { get; } = new();
    public Dictionary<LogLevel, ConsoleColor> LogLevelColors { get; } = LoggerHelper.GetDefaultLogLevelColors();

    public ConsoleBeautifyLoggerConfiguration SetJsonFormatEnabled()
    {
        JsonFormatEnabled = true;
        return this;
    }

    public ConsoleBeautifyLoggerConfiguration AddEnricher(string key, string value)
    {
        Enrichers[key] = value;
        return this;
    }

    public ConsoleBeautifyLoggerConfiguration SetMinimumLogLevel(string key, LogLevel logLevel)
    {
        LogLevels[key] = logLevel;
        return this;
    }

    public ConsoleBeautifyLoggerConfiguration SetLogLevelColor(LogLevel logLevel, ConsoleColor color)
    {
        LogLevelColors[logLevel] = color;
        return this;
    }
}