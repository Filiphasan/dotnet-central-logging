using System.Text;
using System.Text.Json;
using BgWorker.Services.Interfaces;

namespace BgWorker.Workers;

public class FailedEcsLogEntryWorker(
    ILogger<FailedEcsLogEntryWorker> logger,
    IFailedEcsLogEntryWarehouseService failedEcsLogEntryWarehouseService
) : BackgroundService
{
    private const string MethodName = nameof(GeneralLogElasticWorker);
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(100));
    private StreamWriter? _streamWriter;
    private DateTime _writerDate = DateTime.UtcNow; 

    private const int TriggerSize = 100;
    private const int TriggerDelay = 60 * 1000;
    private const int ChunkSize = 3_000;
    private DateTime _lastTriggerDate = DateTime.UtcNow;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var count = failedEcsLogEntryWarehouseService.Count();
                if (count < TriggerSize && DateTime.UtcNow.Subtract(_lastTriggerDate).TotalMilliseconds < TriggerDelay)
                {
                    continue;
                }

                _lastTriggerDate = DateTime.UtcNow;

                await CheckStreamWriterAsync();
                if (_streamWriter is null)
                {
                    logger.LogCritical("{MethodName} StreamWriter is null", MethodName);
                    continue;
                }

                var failedList = failedEcsLogEntryWarehouseService.DrainList();
                failedList = failedList.OrderBy(x => x.Timestamp).ToList();
                if (failedList.Count == 0)
                {
                    continue;
                }

                var chunkList = failedList.Chunk(ChunkSize);
                foreach (var chunk in chunkList)
                {
                    var stringBuilder = new StringBuilder();
                    foreach (var ecsLogEntryModel in chunk)
                    {
                        stringBuilder.AppendLine(JsonSerializer.Serialize(ecsLogEntryModel));
                    }

                    await _streamWriter.WriteAsync(stringBuilder, stoppingToken);
                    stringBuilder.Clear();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{MethodName} on Exception: {Message}", MethodName, ex.Message);
            }
        }
    }

    private async Task CheckStreamWriterAsync()
    {
        if (_streamWriter is not null && _writerDate.Date == DateTime.UtcNow.Date)
        {
            return;
        }

        if (_streamWriter is not null)
        {
            await _streamWriter.DisposeAsync().ConfigureAwait(false);
        }

        var utcNow = DateTime.UtcNow;
        var folderPath = Path.Combine(AppContext.BaseDirectory, "FailedEcsLogs");
        var filePath = Path.Combine(folderPath, $"failed-ecs-logs-{utcNow:yyyyMMdd}.log");
        Directory.CreateDirectory(folderPath);

        _streamWriter = new StreamWriter(filePath, append: true, Encoding.UTF8, 64 * 1024) { AutoFlush = true };
        _writerDate = utcNow;
    }
}