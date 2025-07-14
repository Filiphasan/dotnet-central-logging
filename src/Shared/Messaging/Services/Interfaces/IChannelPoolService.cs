using RabbitMQ.Client;

namespace Shared.Messaging.Services.Interfaces;

public interface IChannelPoolService
{
    Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default);
    Task ReturnChannelAsync(IChannel? channel, CancellationToken cancellationToken = default);
    Task PurgeChannelPoolAsync(CancellationToken cancellationToken = default);
}