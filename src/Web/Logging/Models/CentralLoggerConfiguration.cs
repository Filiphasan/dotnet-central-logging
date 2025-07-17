namespace Web.Logging.Models;

public class CentralLoggerConfiguration
{
    public string IxdexPrefix { get; set; } = string.Empty;
    public string ExchangeName { get; set; } = string.Empty;
    public Dictionary<string, string> Enrichers { get; } = new();
    public Dictionary<string, LogLevel> LogLevels { get; set; } = new();
}