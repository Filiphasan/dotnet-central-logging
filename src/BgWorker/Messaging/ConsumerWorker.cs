using BgWorker.Messaging.Services.Interfaces;

namespace BgWorker.Messaging;

public class ConsumerWorker(IConsumerManager consumerManager, ILogger<ConsumerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker running at: {Time}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss:fffffff"));
        
        await consumerManager.StartAllConsumersAsync(stoppingToken);

        logger.LogInformation("Worker stopped at: {Time}", DateTime.UtcNow);
    }
}