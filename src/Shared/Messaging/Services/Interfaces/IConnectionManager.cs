using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shared.Messaging.Services.Interfaces;

public interface IConnectionManager
{
    bool IsConnected { get; }
    event AsyncEventHandler<ShutdownEventArgs>? OptionalConnectionShutdownAsync;
    Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default);
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}