using System.Text;
using System.Text.Json;
using BgWorker.Messaging.Models;
using BgWorker.Messaging.Services.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messaging.Services.Interfaces;

namespace BgWorker.Messaging.Services.Implementations;

public class MqConsumeService(IChannelPoolService channelPool, IServiceScopeFactory serviceScopeFactory, ILogger<MqConsumeService> logger, ICompressorService compressorService)
    : IMqConsumeService
{
    private const string MethodName = nameof(MqConsumeService);

    public async Task ConsumeAsync<TModel>(IConsumerBase<TModel> consume, CancellationToken cancellationToken = default) where TModel : class
    {
        var consumeInfo = consume.GetConsumeInfo();

        var channel = await channelPool.GetChannelAsync(cancellationToken);
        SetChannelShotdownEvent(channel);
        await SetupChannelAsync(channel, consumeInfo, cancellationToken);

        var consumer = await GetConsumerAsync(channel, cancellationToken);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var mesaageToken = GetMessageToken(consumeInfo, cancellationToken);

            try
            {
                var message = await GetMessageAsync<TModel>(ea, consumeInfo, mesaageToken);
                if (message is null)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false, mesaageToken);
                    return;
                }

                var result = await consume.ConsumeAsync(message, mesaageToken);
                await HandleConsumeResultAsync(ea, result, channel, mesaageToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Message consume operation canceled");
                await channel.BasicRejectAsync(ea.DeliveryTag, false, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error consuming message");
                await channel.BasicRejectAsync(ea.DeliveryTag, false, mesaageToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: consumeInfo.Queue.Name,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken
        );
    }

    public async Task ConsumeAsync<TModel>(ConsumeInfoModel consumeInfo, Func<TModel, CancellationToken, Task<ConsumeResult>> consume, CancellationToken cancellationToken = default)
        where TModel : class
    {
        var channel = await channelPool.GetChannelAsync(cancellationToken);
        SetChannelShotdownEvent(channel);
        await SetupChannelAsync(channel, consumeInfo, cancellationToken);

        var consumer = await GetConsumerAsync(channel, cancellationToken);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var mesaageToken = GetMessageToken(consumeInfo, cancellationToken);

            try
            {
                var message = await GetMessageAsync<TModel>(ea, consumeInfo, mesaageToken);
                if (message is null)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false, mesaageToken);
                    return;
                }

                var result = await consume.Invoke(message, mesaageToken);
                await HandleConsumeResultAsync(ea, result, channel, mesaageToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Message consume operation canceled");
                await channel.BasicRejectAsync(ea.DeliveryTag, false, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error consuming message");
                await channel.BasicRejectAsync(ea.DeliveryTag, false, mesaageToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: consumeInfo.Queue.Name,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken
        );
    }

    public async Task ConsumeAsync<TModel>(ConsumeInfoModel consumeInfo, Func<TModel, IServiceScope, CancellationToken, Task<ConsumeResult>> consume, CancellationToken cancellationToken = default)
        where TModel : class
    {
        var channel = await channelPool.GetChannelAsync(cancellationToken);
        SetChannelShotdownEvent(channel);
        await SetupChannelAsync(channel, consumeInfo, cancellationToken);

        var consumer = await GetConsumerAsync(channel, cancellationToken);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var mesaageToken = GetMessageToken(consumeInfo, cancellationToken);

            try
            {
                var message = await GetMessageAsync<TModel>(ea, consumeInfo, mesaageToken);
                if (message is null)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false, mesaageToken);
                    return;
                }

                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var result = await consume.Invoke(message, scope, mesaageToken);
                await HandleConsumeResultAsync(ea, result, channel, mesaageToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Message consume operation canceled");
                await channel.BasicRejectAsync(ea.DeliveryTag, false, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error consuming message");
                await channel.BasicRejectAsync(ea.DeliveryTag, false, mesaageToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: consumeInfo.Queue.Name,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken
        );
    }

    private void SetChannelShotdownEvent(IChannel channel)
    {
        channel.ChannelShutdownAsync += (_, args) =>
        {
            logger.LogWarning("{MethodName} Channel Shutdown: {Reason}", MethodName, args.ToString());
            return Task.CompletedTask;
        };
    }

    private static async Task SetupChannelAsync(IChannel channel, ConsumeInfoModel consumeInfo, CancellationToken cancellationToken = default)
    {
        await channel.QueueDeclareAsync
        (
            queue: consumeInfo.Queue.Name,
            durable: consumeInfo.Queue.Durable,
            exclusive: consumeInfo.Queue.Exclusive,
            autoDelete: consumeInfo.Queue.AutoDelete,
            arguments: consumeInfo.Queue.Arguments,
            cancellationToken: cancellationToken
        );

        if (!string.IsNullOrEmpty(consumeInfo.Exchange.Name))
        {
            await channel.ExchangeDeclareAsync
            (
                exchange: consumeInfo.Exchange.Name,
                type: consumeInfo.Exchange.ExchangeType,
                durable: consumeInfo.Exchange.Durable,
                autoDelete: consumeInfo.Exchange.AutoDelete,
                arguments: consumeInfo.Exchange.Arguments,
                cancellationToken: cancellationToken
            );
            await channel.QueueBindAsync
            (
                queue: consumeInfo.Queue.Name,
                exchange: consumeInfo.Exchange.Name,
                routingKey: consumeInfo.Exchange.RoutingKey,
                cancellationToken: cancellationToken
            );
        }

        await channel.BasicQosAsync(0, 1, false, cancellationToken);
    }

    private Task<AsyncEventingBasicConsumer> GetConsumerAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ShutdownAsync += (_, args) =>
        {
            logger.LogWarning("{MethodName} consumer shutdown, Reason: {Reason}", MethodName, args.ToString());
            return Task.CompletedTask;
        };

        return Task.FromResult(consumer);
    }

    private static CancellationToken GetMessageToken(ConsumeInfoModel consumeInfo, CancellationToken cancellationToken)
    {
        var messageTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        messageTokenSource.CancelAfter(consumeInfo.Message.Timeout);

        var messageToken = messageTokenSource.Token;
        return messageToken;
    }

    private async Task<TModel?> GetMessageAsync<TModel>(BasicDeliverEventArgs ea, ConsumeInfoModel consumeInfo, CancellationToken cancellationToken = default) where TModel : class
    {
        var body = ea.Body.ToArray();
        if (consumeInfo.Message.Decompress)
        {
            body = await compressorService.DecompressAsync(body, cancellationToken);
        }

        var json = Encoding.UTF8.GetString(body);
        var message = JsonSerializer.Deserialize<TModel>(json, consumeInfo.Message.SerializerOptions);
        return message;
    }

    private static async Task HandleConsumeResultAsync(BasicDeliverEventArgs ea, ConsumeResult result, IChannel channel, CancellationToken cancellationToken = default)
    {
        switch (result.Result)
        {
            case ConsumeResultType.Retry:
                // Retry işlemleri tasarlanabilir
                await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                break;
            case ConsumeResultType.Delayed:
                // Delayed işlemleri tasarlanabilir
                await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                break;
            case ConsumeResultType.DeadLetter:
                // DeadLetter işlemleri tasarlanabilir
                await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                break;
            case ConsumeResultType.Done:
                await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                break;
        }
    }
}