using System.Text.Json;
using System.Threading.Channels;
using Shared.Logging.Models;

namespace Shared.Logging.Writer;

public class ConsoleBeautifyChannelWriter
{
    private readonly Channel<ConsoleBeautifyLogRecord> _channel;
    private readonly Task _writerTask;

    public ConsoleBeautifyChannelWriter()
    {
        var options = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        };
        _channel = Channel.CreateBounded<ConsoleBeautifyLogRecord>(options);
        _writerTask = Task.Run(ProcessChannelAsync);
    }

    public void Write(ConsoleBeautifyLogRecord record)
    {
        _channel.Writer.TryWrite(record);
    }

    private async Task ProcessChannelAsync()
    {
        try
        {
            await foreach (var record in _channel.Reader.ReadAllAsync())
            {
                try
                {
                    if (record.IsJson)
                    {
                        WriteColoredJsonMessage(record);
                    }
                    else
                    {
                        WriteColoredMessage(record);
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

    private static void WriteColoredJsonMessage(ConsoleBeautifyLogRecord logRecord)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = logRecord.Color;
        Console.WriteLine(JsonSerializer.Serialize(logRecord.LogEntry, LogEntryHelper.GetIntendOption));
        Console.ForegroundColor = originalColor;
    }

    private static void WriteColoredMessage(ConsoleBeautifyLogRecord logRecord)
    {
        ConsoleColor originalColor = Console.ForegroundColor;

        Console.ForegroundColor = logRecord.Color;
        Console.WriteLine("-------------------------------------------------");
        Console.WriteLine($"[{logRecord.LogEntry.EventId,3}: {logRecord.LogEntry.Level,-12} - {logRecord.LogEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fffffff}]");

        Console.ForegroundColor = originalColor;
        Console.Write("      Enrichers - ");

        Console.ForegroundColor = logRecord.Color;
        Console.Write($"{string.Join(", ", logRecord.LogEntry.Enrichers.Select(x => $"{x.Key}: {x.Value}"))}");

        Console.WriteLine();

        Console.ForegroundColor = originalColor;
        Console.Write($"      {logRecord.LogEntry.Source} - ");

        Console.ForegroundColor = logRecord.Color;
        Console.Write($"{logRecord.LogEntry.Message}");

        Console.WriteLine();

        if (logRecord.LogEntry.Exception is not null)
        {
            Console.WriteLine($"      {logRecord.LogEntry.Exception.GetExceptionDetailedMessage()}");
        }

        Console.ForegroundColor = originalColor;
    }
}

public sealed record ConsoleBeautifyLogRecord(ConsoleColor Color, bool IsJson, LogEntryModel LogEntry);