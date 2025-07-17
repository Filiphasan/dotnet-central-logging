using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messaging.Services.Interfaces;
using Shared.Models.Options;

namespace Shared.Messaging.Services.Implementations;

public class ChannelPoolService(IConnectionManager connectionManager, MessagingOptions messagingOptions, ILogger<ChannelPoolService> logger)
    : IChannelPoolService
{
    private readonly Lazy<ConcurrentBag<IChannel>> _channelBagLazy = new(() => []);
    private bool _isShutdownHandlerSet;

    private ConcurrentBag<IChannel> ChannelBag => _channelBagLazy.Value;

    public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        await SetShutdownHandlerAsync(cancellationToken);

        if (ChannelBag.TryTake(out var channel))
        {
            return channel;
        }

        var newChannel = await CreateChannelAsync(cancellationToken);
        return newChannel;
    }

    private Task SetShutdownHandlerAsync(CancellationToken cancellationToken = default)
    {
        if (_isShutdownHandlerSet)
        {
            return Task.CompletedTask;
        }

        try
        {
            connectionManager.SetShutdownHandler(async _ => await PurgeChannelPoolAsync(cancellationToken));
            _isShutdownHandlerSet = true;
        }
        catch (InvalidOperationException)
        {
            // pass
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting shutdown handler");
        }

        return Task.CompletedTask;
    }

    private async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        var channel = await connectionManager.GetChannelAsync(cancellationToken);
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

        if (ChannelBag.Count >= messagingOptions.PoolSize)
        {
            await channel.CloseAsync(cancellationToken);
            await channel.DisposeAsync().ConfigureAwait(false);
            return;
        }

        ChannelBag.Add(channel);
    }

    public async Task PurgeChannelPoolAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("PurgeChannelPoolAsync called, Pool size is {PoolSize}", ChannelBag.Count);

        while (ChannelBag.TryTake(out var channel))
        {
            try
            {
                await channel.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disposing channel on PurgeChannelPoolAsync");
            }
        }
    }
}