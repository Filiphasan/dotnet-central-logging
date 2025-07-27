using BgWorker.Helpers;
using BgWorker.Models;
using BgWorker.Services.Interfaces;

namespace BgWorker.Workers;

public class GeneralLogElasticWorker(ILogEntryWarehouseService logEntryWarehouseService, IElasticService elasticService, ILogger<GeneralLogElasticWorker> logger) : BackgroundService
{
    private const string MethodName = nameof(GeneralLogElasticWorker);
    private readonly PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

    private const int TriggerSize = 50;
    private const int TriggerDelay = 2 * 1000;
    private readonly DateTime _lastTriggerDate = DateTime.UtcNow;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
            var cancellationToken = cancellationTokenSource.Token;
            try
            {
                if (logEntryWarehouseService.Count() >= TriggerSize || DateTime.UtcNow.Subtract(_lastTriggerDate).TotalMilliseconds >= TriggerDelay)
                {
                    var list = GetEcsEntries();
                    var options = new ParallelOptions { MaxDegreeOfParallelism = 20, CancellationToken = cancellationToken };
                    await Parallel.ForEachAsync(list, options, async (entries, ct) =>
                    {
                        try
                        {
                            await elasticService.BulkAsync(new ElasticBulkModel<EcsLogEntryModel>
                            {
                                Index = $"{entries.LogKey.ToLower()}-logs-{DateTime.UtcNow:yyyy-MM-dd}",
                                List = entries.LogEntries.ToList()
                            }, ct);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "{MethodName} on entries Exception: {Message}", MethodName, ex.Message);
                        }
                    });
                }
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
}

public sealed record EcsLogEntryByLogKeyRecord(string LogKey, EcsLogEntryModel[] LogEntries);