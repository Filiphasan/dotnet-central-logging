using BgWorker.Messaging.Extensions;
using BgWorker.Messaging.Services.Interfaces;

namespace BgWorker.Messaging.Services.Implementations;

public class ConsumerManager(IMqConsumeService consumeService, IServiceProvider serviceProvider, ConsumerConfig consumerConfig) : IConsumerManager
{
    private readonly Dictionary<string, Type> _consumerTypes = new();

    public async Task StartAllConsumersAsync(CancellationToken cancellationToken = default)
    {
        AddConsumersFromAssemblies();

        List<Task> tasks = [];
        foreach (var consumerType in _consumerTypes)
        {
            var service = serviceProvider.GetRequiredService(consumerType.Value);
            var modelType = consumerType.Value.GetInterfaces()
                .First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IConsumerBase<>))
                .GetGenericArguments()[0];

            var countProperty = consumerType.Value.GetInterfaces()
                .First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IConsumerBase<>))
                .GetProperty(nameof(IConsumerBase<object>.ConsumerCount));
            int consumerCount = (int)(countProperty?.GetValue(service) ?? 1);

            var method = typeof(IMqConsumeService)
                .GetMethods()
                .First(x => x.Name == nameof(IMqConsumeService.ConsumeAsync)
                    && x.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IConsumerBase<>)
                    && x.GetParameters()[1].ParameterType == typeof(CancellationToken)
                    && x.GetParameters().Length == 2)
                .MakeGenericMethod(modelType);

            for (int i = 0; i < consumerCount; i++)
            {
                var task = (Task)method.Invoke(consumeService, [service, cancellationToken])!;
                tasks.Add(task);
            }
        }

        await Task.WhenAll(tasks);
    }

    private void AddConsumersFromAssemblies()
    {
        foreach (var assembly in consumerConfig.Assemblies)
        {
            var consumerTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IConsumerBase<>)))
                .ToList();

            foreach (var consumerType in consumerTypes)
            {
                _consumerTypes[consumerType.Name] = consumerType;
            }
        }
    }

    public string[] GetConsumerNamesAsync()
    {
        return _consumerTypes.Keys.ToArray();
    }
}