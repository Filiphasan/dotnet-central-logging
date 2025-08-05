using Microsoft.Extensions.Logging;

namespace Shared.Logging.Models.Central;

public class CentralLoggerConfiguration
{
    internal string LogKey { get; private set; } = string.Empty;
    internal bool IsSpecific { get; private set; }
    internal string ExchangeName { get; private set; } = string.Empty;
    internal int ChannelBound { get; private set; } = 20_000;
    internal int MaxParallelizm { get; private set; } = 20;
    internal Dictionary<string, string> Enrichers { get; } = new();
    internal Dictionary<string, LogLevel> LogLevels { get; } = new();

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

    public CentralLoggerConfiguration SetChannelBound(int channelBound)
    {
        ChannelBound = channelBound;
        return this;
    }

    public CentralLoggerConfiguration SetMaxParallelizm(int channelPoolSize)
    {
        MaxParallelizm = channelPoolSize;
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