using BgWorker.Helpers;
using BgWorker.Models;
using BgWorker.Services.Interfaces;

namespace BgWorker.Workers;

public class GeneralLogElasticWorker(
    ILogEntryWarehouseService logEntryWarehouseService,
    IFailedEcsLogEntryWarehouseService failedEcsLogEntryWarehouseService,
    IEcsLogEntryMemEstimateService memEstimateService,
    IElasticService elasticService,
    ILogger<GeneralLogElasticWorker> logger) : BackgroundService
{
    private const string MethodName = nameof(GeneralLogElasticWorker);
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(100));

    private const int TriggerSize = 10_000;
    private const int TriggerDelay = 2 * 1000;
    private DateTime _lastTriggerDate = DateTime.UtcNow;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                if (logEntryWarehouseService.Count() < TriggerSize && !(DateTime.UtcNow.Subtract(_lastTriggerDate).TotalMilliseconds >= TriggerDelay))
                {
                    continue;
                }

                var list = GetEcsEntries();
                foreach (var entries in list)
                {
                    try
                    {
                        await WriteToElasticAsync(entries, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        failedEcsLogEntryWarehouseService.AddLogEntries(entries.LogEntries);
                        logger.LogError(ex, "{MethodName} on entries Exception: {Message}", MethodName, ex.Message);
                    }
                }

                _lastTriggerDate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{MethodName} on Exception: {Message}", MethodName, ex.Message);
            }
        }
    }

    private EcsLogEntryByLogKeyRecord[] GetEcsEntries()
    {
        var list = logEntryWarehouseService.DrainList();
        return list.AsParallel()
            .GroupBy(x => x.LogKey)
            .Select(x => new EcsLogEntryByLogKeyRecord(x.Key, x.Select(EcsLogEntryHelper.LogEntryToEcs).ToArray()))
            .ToArray();
    }

    private async Task WriteToElasticAsync(EcsLogEntryByLogKeyRecord record, CancellationToken cancellationToken)
    {
        var logKey = record.LogKey.ToLower();
        memEstimateService.CalculateBytes(logKey, record.LogEntries.First());

        var chunkSize = memEstimateService.GetAvgBulkApiRequestItemSize(logKey);
        var indexName = $"{logKey}-logs-{DateTime.UtcNow:yyyy-MM-dd}";
        var chunkedList = record.LogEntries.Chunk(chunkSize);
        foreach (var chunk in chunkedList)
        {
            try
            {
                var failed = await elasticService.BulkAsync(new ElasticBulkModel<EcsLogEntryModel>
                {
                    Index = indexName,
                    List = record.LogEntries.ToList()
                }, cancellationToken);

                failedEcsLogEntryWarehouseService.AddLogEntries(failed);
            }
            catch (Exception ex)
            {
                failedEcsLogEntryWarehouseService.AddLogEntries(chunk);
                logger.LogError(ex, "{MethodName} on entries Exception: {Message}", MethodName, ex.Message);
            }
        }

    }
}

public sealed record EcsLogEntryByLogKeyRecord(string LogKey, EcsLogEntryModel[] LogEntries);