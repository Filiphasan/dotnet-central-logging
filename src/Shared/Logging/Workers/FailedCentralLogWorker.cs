using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Logging.Helpers;
using Shared.Logging.Models;
using Shared.Logging.Models.Central;
using Shared.Messaging.Models;
using Shared.Messaging.Services.Interfaces;

namespace Shared.Logging.Workers;

public class FailedCentralLogWorker(ILogger<FailedCentralLogWorker> logger, CentralLogChannelWriterConfiguration options, IPublishService publishService) : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(15));

    private readonly FileStreamOptions _fileReadOptions = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        Options = FileOptions.SequentialScan,
    };
    private const int MaxFileSize = 100 * 1024 * 1024; // 100MB

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var filePaths = GetFilePathList();

                foreach (var filePath in filePaths)
                {
                    try
                    {
                        await ProcessLogFileAsync(filePath, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Exception processing file {FilePath}, Message: {Message}", filePath, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception on FailedCentralLogWorker, Message: {Message}", ex.Message);
            }
        }
    }

    private async Task ProcessLogFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MaxFileSize)
        {
            logger.LogWarning("File {FilePath} is too large ({Size}MB), skipping", filePath, fileInfo.Length / 1024 / 1024);
            return;
        }

        var logLines = await ReadLogLinesAsync(filePath, cancellationToken);
        if (logLines.Count == 0)
        {
            return;
        }

        logger.LogInformation("Processing {LineCount} log entries from file: {FilePath}", logLines.Count, filePath);

        var processedResults = await ProcessLogLinesAsync(logLines, cancellationToken);

        await UpdateLogFileAsync(filePath, processedResults, cancellationToken);

        var successCount = processedResults.Count(x => x.Success);
        var failCount = processedResults.Count(x => !x.Success);

        logger.LogInformation("Processed file {FilePath}: {SuccessCount} successful, {FailCount} failed",
            filePath, successCount, failCount);
    }

    private async Task<List<LogFileLineModel>> ReadLogLinesAsync(string filePath, CancellationToken cancellationToken)
    {
        var lineLogList = new List<LogFileLineModel>();

        try
        {
            using var reader = new StreamReader(filePath, Encoding.UTF8, true, _fileReadOptions);
            var lineNumber = 0;

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lineLogList.Add(new LogFileLineModel
                    {
                        LogEntry = line,
                        Success = false,
                        LineNumber = ++lineNumber
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading log file {FilePath}: {Message}", filePath, ex.Message);
        }

        return lineLogList;
    }

    private async Task<List<LogFileLineModel>> ProcessLogLinesAsync(List<LogFileLineModel> logLines, CancellationToken cancellationToken)
    {
        var maxParallelism = Math.Min(logLines.Count, options.MaxParallelizm) / 3;
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = maxParallelism
        };

        await Parallel.ForEachAsync(logLines, parallelOptions, async (logLine, ct) =>
        {
            try
            {
                var logEntry = JsonSerializer.Deserialize<LogEntryModel>(logLine.LogEntry, LogEntryHelper.GetNonIntendOption);
                if (logEntry is not null)
                {
                    var success = await PublishAsync(logEntry, ct);
                    logLine.Success = success;
                }
                else
                {
                    logger.LogWarning("Failed to deserialize log entry at line {LineNumber}", logLine.LineNumber);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception processing log line {LineNumber}, Message: {Message}",
                    logLine.LineNumber, ex.Message);
                logLine.Success = false;
            }
        });

        return logLines;
    }

    private async Task UpdateLogFileAsync(string filePath, List<LogFileLineModel> processedResults, CancellationToken cancellationToken)
    {
        try
        {
            var failedLines = processedResults
                .Where(x => !x.Success)
                .Select(x => x.LogEntry)
                .ToList();

            if (failedLines.Count == 0)
            {
                // Tüm satırlar başarılı, dosyayı sil
                File.Delete(filePath);
                logger.LogInformation("All log entries processed successfully, deleted file: {FilePath}", filePath);
                return;
            }

            if (failedLines.Count < processedResults.Count)
            {
                // Sadece başarısız olan satırları dosyaya yaz
                var tempFilePath = filePath + ".tmp";

                await using var writer = new StreamWriter(tempFilePath, false, Encoding.UTF8);
                foreach (var failedLine in failedLines)
                {
                    await writer.WriteLineAsync(failedLine.AsMemory(), cancellationToken);
                }

                File.Move(tempFilePath, filePath, overwrite: true);

                logger.LogInformation("Updated file {FilePath} with {FailedCount} remaining failed entries", filePath, failedLines.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating log file {FilePath}: {Message}", filePath, ex.Message);
        }
    }

    private List<string> GetFilePathList()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-3);
        var hourCount = (int)DateTime.UtcNow.Subtract(startDate).TotalHours;

        var filePaths = new List<string>();
        for (var i = 0; i < hourCount; i++)
        {
            var date = startDate.AddHours(i);
            var filePath = LoggerHelper.GetFileLoggerPath(options.FailedLogsBaseFolder, date);
            filePaths.Add(filePath);
        }

        return filePaths;
    }

    private async Task<bool> PublishAsync(LogEntryModel logEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            var rkSuffix = options.IsSpecific ? "specific" : "general";
            var publishMessageModel = new PublishMessageModel<LogEntryModel>
            {
                Message = logEntry,
                CompressMessage = true,
                JsonSerializerOptions = LogEntryHelper.GetNonIntendOption,
                Exchange =
                {
                    Name = options.ExchangeName,
                    Type = ExchangeType.Topic,
                },
                RoutingKey = $"project.{logEntry.LogKey.ToLower()}.{rkSuffix}",
                TryCount = 5,
            };
            await publishService.PublishAsync(publishMessageModel, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish log entry for LogKey: {LogKey}, Message: {Message}",
                logEntry.LogKey, ex.Message);
            return false;
        }
    }
}

public class LogFileLineModel
{
    public string LogEntry { get; set; } = null!;
    public bool Success { get; set; }
    public int LineNumber { get; set; }
}