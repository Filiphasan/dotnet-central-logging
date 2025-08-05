using Microsoft.Extensions.Logging;
using Shared.Logging.Helpers;

namespace Shared.Logging.Models.ConsoleBeautify;

public class ConsoleBeautifyLoggerConfiguration
{
    internal bool JsonFormatEnabled { get; private set; }
    internal LogLevel DefaultLogLevel { get; private set; } = LogLevel.Information;
    internal Dictionary<string, string> Enrichers { get; } = new();
    internal Dictionary<string, LogLevel> LogLevels { get; } = new();
    internal Dictionary<LogLevel, ConsoleColor> LogLevelColors { get; } = LoggerHelper.GetDefaultLogLevelColors();
    internal int ChannelBound { get; private set; } = 10_000;

    public ConsoleBeautifyLoggerConfiguration SetJsonFormatEnabled()
    {
        JsonFormatEnabled = true;
        return this;
    }

    public ConsoleBeautifyLoggerConfiguration SetDefaultLogLevel(LogLevel defaultLogLevel)
    {
        DefaultLogLevel = defaultLogLevel;
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

    public ConsoleBeautifyLoggerConfiguration SetChannelBound(int channelBound)
    {
        ChannelBound = channelBound;
        return this;
    }
}