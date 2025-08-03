namespace Shared.Logging.Models.Central;

public class CentralLogChannelWriterConfiguration
{
    internal bool IsSpecific { get; init; }
    internal string ExchangeName { get; init; } = string.Empty;
    internal int MaxParallelizm { get; init; } = 20;
}