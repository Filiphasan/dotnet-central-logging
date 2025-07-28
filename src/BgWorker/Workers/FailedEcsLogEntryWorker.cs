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
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));
    private StreamWriter? _streamWriter;
    private DateTime _writerDate = DateTime.UtcNow; 

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckStreamWriterAsync();

                var failedList = failedEcsLogEntryWarehouseService.DrainList();
                failedList = failedList.OrderBy(x => x.Timestamp).ToList();
                if (failedList.Count == 0)
                {
                    continue;
                }

                var stringBuilder = new StringBuilder();
                foreach (var ecsLogEntryModel in failedList)
                {
                    stringBuilder.AppendLine(JsonSerializer.Serialize(ecsLogEntryModel));
                }

                await _streamWriter!.WriteAsync(stringBuilder, stoppingToken);
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
        var folderPath = Path.Combine(AppContext.BaseDirectory, "FailedLogs", utcNow.Year.ToString(), utcNow.Month.ToString(), utcNow.Day.ToString());
        var filePath = Path.Combine(folderPath, "failed-ecs-logs.json");
        Directory.CreateDirectory(folderPath);

        _streamWriter = new StreamWriter(filePath, append: true, Encoding.UTF8) { AutoFlush = true };
        _writerDate = utcNow;
    }
}