using Microsoft.Extensions.Logging;

namespace Shared.Logging.Models.Central;

public class CentralLoggerConfiguration
{
    public string LogKey { get; set; } = string.Empty;
    public bool IsSpecific { get; set; }
    public string ExchangeName { get; set; } = string.Empty;
    public Dictionary<string, string> Enrichers { get; } = new();
    public Dictionary<string, LogLevel> LogLevels { get; set; } = new();
}