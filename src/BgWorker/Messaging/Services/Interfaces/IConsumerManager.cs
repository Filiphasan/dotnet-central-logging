namespace BgWorker.Messaging.Services.Interfaces;

public interface IConsumerManager
{
    Task StartAllConsumersAsync(CancellationToken cancellationToken = default);
    string[] GetConsumerNamesAsync();
}