using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using RabbitMQ.Client;
using Shared.Messaging.Services.Implementations;
using Shared.Messaging.Services.Interfaces;
using Shared.Models.Options;

namespace Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddShared(this IServiceCollection services, IConfiguration configuration)
    {
        var messagingOptions = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>();
        ArgumentNullException.ThrowIfNull(messagingOptions);

        services.AddSingleton(messagingOptions);
        services.AddRabbitMq(messagingOptions);
        services.AddSingleton<IConnectionManager, RabbitMqConnectionManager>();
        services.AddSingleton<IChannelPoolService, ChannelPoolService>();
        services.AddSingleton<IPublishService, PublishService>();
        services.AddSingleton<RecyclableMemoryStreamManager>();
        services.AddSingleton<ICompressorService, BrotliCompressorService>();
        return services;
    }

    private static void AddRabbitMq(this IServiceCollection services, MessagingOptions messagingOptions)
    {
        services.AddSingleton<IConnectionFactory>(new ConnectionFactory
        {
            HostName = messagingOptions.Host,
            Port = messagingOptions.Port,
            UserName = messagingOptions.User,
            Password = messagingOptions.Password,
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = messagingOptions.ConnectionName,
        });
    }
}