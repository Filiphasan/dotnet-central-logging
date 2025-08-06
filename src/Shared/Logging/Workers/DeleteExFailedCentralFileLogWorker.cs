using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Logging.Helpers;
using Shared.Logging.Models.Central;

namespace Shared.Logging.Workers;

public class DeleteExFailedCentralFileLogWorker(ILogger<DeleteExFailedCentralFileLogWorker> logger, CentralLogChannelWriterConfiguration options) : BackgroundService
{
    private readonly PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromHours(1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var filePaths = GetFilePathList();
                foreach (var filePath in filePaths)
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception on DeleteExFailedCentralFileLogWorker, Message: {Message}", ex.Message);
            }
        }
    }

    private List<string> GetFilePathList()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-7);
        var hourCount = (int)DateTime.UtcNow.Date.AddDays(-3).Subtract(startDate).TotalHours;

        var filePaths = new List<string>();
        for (var i = 0; i < hourCount; i++)
        {
            var date = startDate.AddHours(i);
            var filePath = LoggerHelper.GetFileLoggerPath(options.FailedLogsBaseFolder, date);
            filePaths.Add(filePath);
        }

        return filePaths;
    }
}