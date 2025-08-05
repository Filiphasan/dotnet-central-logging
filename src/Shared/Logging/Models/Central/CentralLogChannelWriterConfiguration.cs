namespace Shared.Logging.Models.Central;

public class CentralLogChannelWriterConfiguration
{
    internal bool IsSpecific { get; init; }
    internal string ExchangeName { get; init; } = string.Empty;
    internal int ChannelBound { get; init; } = 20_000;
    internal int MaxParallelizm { get; init; } = 20;
    internal string FailedLogsBaseFolder { get; init; } = "CentralFailedLogs";
}