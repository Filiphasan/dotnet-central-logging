using Microsoft.Extensions.Logging;

namespace Shared.Logging.Models.Central;

public class CentralLoggerConfiguration
{
    public string LogKey { get; private set; } = string.Empty;
    public bool IsSpecific { get; private set; }
    public string ExchangeName { get; set; } = string.Empty;
    public Dictionary<string, string> Enrichers { get; } = new();
    public Dictionary<string, LogLevel> LogLevels { get; set; } = new();

    public CentralLoggerConfiguration SetLogKey(string logKey)
    {
        LogKey = logKey;
        return this;
    }

    public CentralLoggerConfiguration SetIsSpecific()
    {
        IsSpecific = true;
        return this;
    }

    public CentralLoggerConfiguration SetExchangeName(string exchangeName)
    {
        ExchangeName = exchangeName;
        return this;
    }

    public CentralLoggerConfiguration AddEnricher(string key, string value)
    {
        Enrichers[key] = value;
        return this;
    }

    public CentralLoggerConfiguration SetMinimumLogLevel(string key, LogLevel logLevel)
    {
        LogLevels[key] = logLevel;
        return this;
    }
}