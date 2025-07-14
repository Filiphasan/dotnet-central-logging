using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Messaging.Services.Interfaces;
using Shared.Models.Options;

namespace Shared.Messaging.Services.Implementations;

public class ChannelPoolService : IChannelPoolService
{
    private readonly Lazy<ConcurrentBag<IChannel>> _channelBagLazy = new(() => []);
    private readonly IConnectionManager _connectionManager;
    private readonly MessagingOptions _messagingOptions;
    private readonly ILogger<ChannelPoolService> _logger;

    public ChannelPoolService(IConnectionManager connectionManager, MessagingOptions messagingOptions, ILogger<ChannelPoolService> logger)
    {
        _connectionManager = connectionManager;
        _messagingOptions = messagingOptions;
        _logger = logger;
        _connectionManager.OptionalConnectionShutdownAsync += (_, _) => PurgeChannelPoolAsync();
    }

    private ConcurrentBag<IChannel> ChannelBag => _channelBagLazy.Value;

    public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        if (ChannelBag.TryTake(out var channel))
        {
            return channel;
        }

        var newChannel = await CreateChannelAsync(cancellationToken);
        return newChannel;
    }

    private async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        var channel = await _connectionManager.GetChannelAsync(cancellationToken);
        return channel;
    }

    public async Task ReturnChannelAsync(IChannel? channel, CancellationToken cancellationToken = default)
    {
        if (channel is null)
        {
            return;
        }

        if (channel.IsClosed)
        {
            await channel.DisposeAsync().ConfigureAwait(false);
            return;
        }

        if (ChannelBag.Count >= _messagingOptions.PoolSize)
        {
            await channel.CloseAsync(cancellationToken);
            await channel.DisposeAsync().ConfigureAwait(false);
            return;
        }

        ChannelBag.Add(channel);
    }

    public async Task PurgeChannelPoolAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PurgeChannelPoolAsync called, Pool size is {PoolSize}", ChannelBag.Count);

        while (ChannelBag.TryTake(out var channel))
        {
            try
            {
                await channel.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing channel on PurgeChannelPoolAsync");
            }
        }
    }
}