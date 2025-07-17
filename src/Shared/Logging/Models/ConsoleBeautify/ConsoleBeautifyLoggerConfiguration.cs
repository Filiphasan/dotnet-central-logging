using Microsoft.Extensions.Logging;
using Shared.Logging.Helpers;

namespace Shared.Logging.Models.ConsoleBeautify;

public class ConsoleBeautifyLoggerConfiguration
{
    public bool JsonFormatEnabled { get; set; }
    public Dictionary<string, string> Enrichers { get; } = new();
    public Dictionary<string, LogLevel> LogLevels { get; } = new();
    public Dictionary<LogLevel, ConsoleColor> LogLevelColors { get; } = LoggerHelper.GetDefaultLogLevelColors();
}