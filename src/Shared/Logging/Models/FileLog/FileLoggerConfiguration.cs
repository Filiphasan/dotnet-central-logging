using Microsoft.Extensions.Logging;

namespace Shared.Logging.Models.FileLog;

public class FileLoggerConfiguration
{
    internal int MaxParallelism { get; private set; } = 20;
    internal int WriteSize { get; private set; } = 100;
    internal int WriteInterval { get; private set; } = 2_000;
    internal string BaseFolder { get; private set; } = "Logs";
    internal LogLevel DefaultLogLevel { get; private set; } = LogLevel.Information;
    internal Dictionary<string, string> Enrichers { get; } = new();
    internal Dictionary<string, LogLevel> LogLevels { get; } = new();

    public FileLoggerConfiguration SetMaxParallelism(int maxParallelism)
    {
        MaxParallelism = maxParallelism;
        return this;
    }

    public FileLoggerConfiguration SetWriteSize(int batchSize)
    {
        WriteSize = batchSize;
        return this;
    }

    public FileLoggerConfiguration SetWriteInterval(int interval)
    {
        WriteInterval = interval;
        return this;
    }

    public FileLoggerConfiguration SetBaseFolder(string baseFolder)
    {
        BaseFolder = baseFolder;
        return this;
    }

    public FileLoggerConfiguration SetDefaultLogLevel(LogLevel defaultLogLevel)
    {
        DefaultLogLevel = defaultLogLevel;
        return this;
    }

    public FileLoggerConfiguration AddEnricher(string key, string value)
    {
        Enrichers[key] = value;
        return this;
    }

    public FileLoggerConfiguration SetMinimumLogLevel(string key, LogLevel logLevel)
    {
        LogLevels[key] = logLevel;
        return this;
    }
}