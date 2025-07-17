using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messaging.Services.Interfaces;

namespace Shared.Messaging.Services.Implementations;

public class RabbitMqConnectionManager : IConnectionManager
{
    private readonly SemaphoreSlim _semaphoreSlim = new(1);
    private readonly AsyncPolicy _retryPolicy;

    private IConnection? _connection;
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqConnectionManager> _logger;

    private AsyncEventHandler<ShutdownEventArgs>? _connectionShutdownAsync;
    private Func<ShutdownEventArgs, Task>? _shutdownHandler;

    public RabbitMqConnectionManager(IConnectionFactory connectionFactory, ILogger<RabbitMqConnectionManager> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, _, retryCount) =>
                {
                    _logger.LogError(exception, "RabbitMQ connection failed, retrying in {TimeOut}ms RetryCount: {RetryCount}", timeSpan.TotalMilliseconds, retryCount);
                });
    }

    public bool IsConnected => _connection is { IsOpen: true };

    public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        await ConnectAsync(cancellationToken);

        ArgumentNullException.ThrowIfNull(_connection);
        var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        return channel;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            return;
        }

        await _semaphoreSlim.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
            {
                return;
            }

            _connectionShutdownAsync = async (_, args) =>
            {
                if (_shutdownHandler is not null)
                {
                    await _shutdownHandler(args);
                }

                await OnConnectionShutdownAsync(args, cancellationToken);
            };
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await DisconnectAsync(cancellationToken);

                _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                _connection.ConnectionShutdownAsync += _connectionShutdownAsync;
            });
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            return;
        }

        if (_connectionShutdownAsync is not null)
        {
            _connection.ConnectionShutdownAsync -= _connectionShutdownAsync;
            _connectionShutdownAsync = null;
        }

        await _connection.CloseAsync(cancellationToken).ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
        _connection = null;
    }

    public void SetShutdownHandler(Func<ShutdownEventArgs, Task> shutdownHandler)
    {
        if (_shutdownHandler is not null)
        {
            throw new InvalidOperationException("Shutdown handler is already set");
        }

        _shutdownHandler = shutdownHandler;
    }

    private async Task OnConnectionShutdownAsync(ShutdownEventArgs e, CancellationToken cancellationToken = default)
    {
        _logger.LogError("RabbitMQ connection shutdown. Reason: {Reason}", e.ToString());
        await ConnectAsync(cancellationToken);
    }
}