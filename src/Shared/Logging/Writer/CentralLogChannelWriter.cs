using System.Threading.Channels;
using RabbitMQ.Client;
using Shared.Logging.Models;
using Shared.Messaging.Models;
using Shared.Messaging.Services.Interfaces;

namespace Shared.Logging.Writer;

public class CentralLogChannelWriter
{
    private readonly Channel<CentralLogRecord> _channel;
    private readonly Task _writerTask;
    private readonly IPublishService _publishService;

    public CentralLogChannelWriter(IPublishService publishService)
    {
        _publishService = publishService;
        var options = new BoundedChannelOptions(20000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = false,
        };
        _channel = Channel.CreateBounded<CentralLogRecord>(options);
        _writerTask = Task.Run(ProcessChannelAsync);
    }

    public void Write(CentralLogRecord record)
    {
        _channel.Writer.TryWrite(record);
    }


    private async Task ProcessChannelAsync()
    {
        try
        {
            var settings = new ParallelOptions { MaxDegreeOfParallelism = 40 };
            await Parallel.ForEachAsync(_channel.Reader.ReadAllAsync(), settings, async (record, token) =>
            {
                try
                {
                    await PublishAsync(record, token);
                }
                catch (Exception)
                {
                    // Nothing
                }
            });
        }
        catch (Exception)
        {
            // Nothing
        }
    }

    private async Task PublishAsync(CentralLogRecord record, CancellationToken cancellationToken = default)
    {
        var rkEnding = record.IsSpecific ? "specific" : "general";
        var publishMessageModel = new PublishMessageModel<LogEntryModel>
        {
            Message = record.LogEntry,
            CompressMessage = true,
            JsonSerializerOptions = LogEntryHelper.GetNonIntendOption,
            Exchange =
            {
                Name = record.ExchanceName,
                Type = ExchangeType.Topic,
            },
            RoutingKey = $"project.{record.LogEntry.LogKey.ToLower()}.{rkEnding}",
            TryCount = 5,
        };
        await _publishService.PublishAsync(publishMessageModel, cancellationToken);
    }
}

public sealed record CentralLogRecord(string ExchanceName, bool IsSpecific, LogEntryModel LogEntry);