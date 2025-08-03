using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Logging.Helpers;
using Shared.Logging.Models;
using Shared.Logging.Models.Central;
using Shared.Messaging.Models;
using Shared.Messaging.Services.Interfaces;

namespace Shared.Logging.Writer;

public sealed class CentralLogChannelWriter
{
    private readonly Channel<LogEntryModel> _channel;
    private readonly IPublishService _publishService = null!;
    private readonly ConsoleBeautifyChannelWriter _consoleBeautifyChannelWriter;
    private readonly FileLogChannelWriter _fileLogChannelWriter;
    private readonly CentralLogChannelWriterConfiguration _options;
    private static bool _isPublishServiceSet;

    public CentralLogChannelWriter(IServiceProvider serviceProvider, ConsoleBeautifyChannelWriter consoleBeautifyChannelWriter, CentralLogChannelWriterConfiguration options, FileLogChannelWriter fileLogChannelWriter)
    {
        _consoleBeautifyChannelWriter = consoleBeautifyChannelWriter;
        _options = options;
        _fileLogChannelWriter = fileLogChannelWriter;
        if (!_isPublishServiceSet)
        {
            _isPublishServiceSet = true;
            _publishService = serviceProvider.GetRequiredService<IPublishService>();
        }
        var channelOptions = new BoundedChannelOptions(20_000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
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
            var settings = new ParallelOptions { MaxDegreeOfParallelism = _options.MaxParallelizm };
            await Parallel.ForEachAsync(_channel.Reader.ReadAllAsync(), settings, async (logEntry, token) =>
            {
                try
                {
                    var success = await PublishAsync(logEntry, token);
                    if (!success)
                    {
                        _fileLogChannelWriter.Write(logEntry);
                    }
                }
                catch (Exception ex)
                {
                    HandleProcessChannelException(ex, logEntry);
                }
            });
        }
        catch (Exception ex)
        {
            HandleProcessChannelException(ex);
        }
    }

    private void HandleProcessChannelException(Exception exception, LogEntryModel? logEntry = null)
    {
        if (logEntry is not null)
        {
            _fileLogChannelWriter.Write(logEntry);
        }

        _consoleBeautifyChannelWriter.Write(new LogEntryModel
        {
            Timestamp = DateTime.UtcNow,
            Level = nameof(LogLevel.Error),
            Source = "Shared.Logging.Writer.CentralLogChannelWriter",
            Message = "CentralLogChannelWriter Critical Error",
            Exception = LoggerHelper.ExtractExceptionDetail(exception),
            Properties = null,
        });
    }

    private async Task<bool> PublishAsync(LogEntryModel logEntry, CancellationToken cancellationToken = default)
    {
        // Add Circuit Breaker
        var rkSuffix = _options.IsSpecific ? "specific" : "general";
        var publishMessageModel = new PublishMessageModel<LogEntryModel>
        {
            Message = logEntry,
            CompressMessage = true,
            JsonSerializerOptions = LogEntryHelper.GetNonIntendOption,
            Exchange =
            {
                Name = _options.ExchangeName,
                Type = ExchangeType.Topic,
            },
            RoutingKey = $"project.{logEntry.LogKey.ToLower()}.{rkSuffix}",
            TryCount = 5,
        };
        await _publishService.PublishAsync(publishMessageModel, cancellationToken);
        return true;
    }
}