using System.Reflection;
using BgWorker.Messaging.Services.Implementations;
using BgWorker.Messaging.Services.Interfaces;

namespace BgWorker.Messaging.Extensions;

public static class ConsumerExtension
{
    public static IServiceCollection AddConsumers(this IServiceCollection services, Action<ConsumerConfig>? configure = null)
    {
        var config = new ConsumerConfig();
        configure?.Invoke(config);

        services.AddSingleton(config);
        services.AddSingleton<IMqConsumeService, MqConsumeService>();
        services.AddSingleton<IConsumerManager, ConsumerManager>();

        var assemblies = config.Assemblies.Count != 0
            ? config.Assemblies
            : [Assembly.GetCallingAssembly()];

        foreach (var assembly in assemblies)
        {
            RegisterConsumersFromAssembly(services, assembly, ServiceLifetime.Singleton);
        }

        services.AddHostedService<ConsumerWorker>();
        return services;
    }

    private static void RegisterConsumersFromAssembly(IServiceCollection services, Assembly assembly, ServiceLifetime lifetime)
    {
        var consumerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IConsumerBase<>)))
            .ToList();

        foreach (var consumerType in consumerTypes)
        {
            services.Add(new ServiceDescriptor(consumerType, consumerType, lifetime));
        }
    }
}

public class ConsumerConfig
{
    public List<Assembly> Assemblies { get; } = [];

    public ConsumerConfig AddAssembly<T>()
    {
        Assemblies.Add(typeof(T).Assembly);
        return this;
    }

    public ConsumerConfig AddAssembly(Assembly assembly)
    {
        Assemblies.Add(assembly);
        return this;
    }

    public ConsumerConfig AddAssemblies(params Assembly[] assemblies)
    {
        Assemblies.AddRange(assemblies);
        return this;
    }
}