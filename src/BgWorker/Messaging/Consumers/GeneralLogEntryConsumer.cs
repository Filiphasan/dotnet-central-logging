using BgWorker.Messaging.Models;
using BgWorker.Messaging.Services.Interfaces;
using BgWorker.Services.Interfaces;
using RabbitMQ.Client;
using Shared.Logging.Models;

namespace BgWorker.Messaging.Consumers;

public class GeneralLogEntryConsumer(ILogEntryWarehouseService logEntryWarehouse) : IConsumerBase<LogEntryModel>
{
    public int ConsumerCount { get; } = 5;

    public ConsumeInfoModel GetConsumeInfo()
    {
        return new ConsumeInfoModel
        {
            Queue =
            {
                Name = "general-log-entry",
            },
            Exchange =
            {
                Name = "central-logs-exchange",
                ExchangeType = ExchangeType.Topic,
                RoutingKey = "project.*.general",
            },
            Message =
            {
                Decompress = true,
                Timeout = TimeSpan.FromSeconds(5),
                SerializerOptions = LogEntryHelper.GetNonIntendOption,
            }
        };
    }

    public Task<ConsumeResult> ConsumeAsync(LogEntryModel model, CancellationToken cancellationToken = default)
    {
        logEntryWarehouse.AddLogEntry(model);
        return Task.FromResult(ConsumeResult.Done);
    }
}