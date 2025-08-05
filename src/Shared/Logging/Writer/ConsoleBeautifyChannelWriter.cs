using System.Text.Json;
using System.Threading.Channels;
using Shared.Logging.Helpers;
using Shared.Logging.Models;
using Shared.Logging.Models.ConsoleBeautify;

namespace Shared.Logging.Writer;

public class ConsoleBeautifyChannelWriter
{
    private readonly ConsoleBeautifyChannelWriterConfiguration _options;
    private readonly Channel<LogEntryModel> _channel;

    public ConsoleBeautifyChannelWriter(ConsoleBeautifyChannelWriterConfiguration options)
    {
        _options = options;
        var channelOptions = new BoundedChannelOptions(_options.ChannelBound)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        };
        _channel = Channel.CreateBounded<LogEntryModel>(channelOptions);
        Task.Run(ProcessChannelAsync);
    }

    public void Write(LogEntryModel logEntry)
    {
        _channel.Writer.TryWrite(logEntry);
    }

    private async Task ProcessChannelAsync()
    {
        try
        {
            await foreach (var logEntry in _channel.Reader.ReadAllAsync())
            {
                try
                {
                    var logColor = _options.LogLevelColors[LoggerHelper.GetLogLevel(logEntry.Level)];
                    if (_options.JsonFormatEnabled)
                    {
                        WriteColoredJsonMessage(logEntry, logColor);
                    }
                    else
                    {
                        WriteColoredMessage(logEntry, logColor);
                    }
                }
                catch (Exception)
                {
                    // Nothing
                }
            }
        }
        catch (Exception)
        {
            // Nothing
        }
    }

    private static void WriteColoredJsonMessage(LogEntryModel logEntry, ConsoleColor color)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(JsonSerializer.Serialize(logEntry, LogEntryHelper.GetIntendOption));
        Console.ForegroundColor = originalColor;
    }

    private static void WriteColoredMessage(LogEntryModel logEntry, ConsoleColor color)
    {
        ConsoleColor originalColor = Console.ForegroundColor;

        Console.ForegroundColor = color;
        Console.WriteLine("-------------------------------------------------");
        Console.WriteLine($"[{logEntry.EventId,3}: {logEntry.Level,-12} - {logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fffffff}]");

        if (logEntry.Enrichers.Count > 0)
        {
            Console.ForegroundColor = originalColor;
            Console.Write("      Enrichers - ");

            Console.ForegroundColor = color;
            Console.Write($"{string.Join(", ", logEntry.Enrichers.Select(x => $"{x.Key}: {x.Value}"))}");

            Console.WriteLine();
        }

        Console.ForegroundColor = originalColor;
        Console.Write($"      {logEntry.Source} - ");

        Console.ForegroundColor = color;
        Console.Write($"{logEntry.Message}");

        Console.WriteLine();

        if (logEntry.Exception is not null)
        {
            Console.WriteLine($"      {logEntry.Exception.GetExceptionDetailedMessage()}");
        }

        Console.ForegroundColor = originalColor;
    }
}