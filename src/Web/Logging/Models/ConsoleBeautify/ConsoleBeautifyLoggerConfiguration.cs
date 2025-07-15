namespace Web.Logging.Models.ConsoleBeautify;

public class ConsoleBeautifyLoggerConfiguration
{
    public Dictionary<string, LogLevel> LogLevels { get; } = new();
    public Dictionary<LogLevel, ConsoleColor> LogLevelColors { get; } = new();
}