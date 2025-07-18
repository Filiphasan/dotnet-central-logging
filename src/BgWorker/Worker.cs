using Shared.Messaging.Services.Interfaces;

namespace BgWorker;

public class Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory ) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BgWorker service running at: {Time}", DateTime.UtcNow);

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var channelPoolService = scope.ServiceProvider.GetRequiredService<IChannelPoolService>();
        
        var channel = await channelPoolService.GetChannelAsync(stoppingToken);

        // Queue & Exchange declare

        // Consume Operations

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await channelPoolService.ReturnChannelAsync(channel, stoppingToken);
        logger.LogInformation("BgWorker service is stopping at: {Time}", DateTime.UtcNow);
    }
}