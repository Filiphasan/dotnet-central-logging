using Microsoft.Extensions.Logging;
using Shared.Logging.Helpers;

namespace Shared.Logging.Models.ConsoleBeautify;

public class ConsoleBeautifyChannelWriterConfiguration
{
    internal bool JsonFormatEnabled { get; init; }
    internal Dictionary<LogLevel, ConsoleColor> LogLevelColors { get; init; } = LoggerHelper.GetDefaultLogLevelColors();
}