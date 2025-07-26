using System.Text;
using BgWorker.Messaging.Consumers;
using BgWorker.Messaging.Extensions;
using BgWorker.Models;
using BgWorker.Services.Implementations;
using BgWorker.Services.Interfaces;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Shared;

namespace BgWorker;

public static class DependencyInjection
{
    public static void AddBgWorker(this IServiceCollection services, IConfiguration configuration)
    {
        var elasticOption = configuration.GetSection(ElasticsearchSetting.SectionName).Get<ElasticsearchSetting>();
        ArgumentNullException.ThrowIfNull(elasticOption);

        services.AddShared(configuration)
            .AddConsumers(opt =>
            {
                opt.AddAssembly<GeneralLogEntryConsumer>();
            })
            .AddSingleton<ILogEntryWarehouseService, LogEntryWarehouseService>()
            .AddElasticServices(elasticOption);
    }

    private static IServiceCollection AddElasticServices(this IServiceCollection services, ElasticsearchSetting elasticOption)
    {
        var settings = new ElasticsearchClientSettings(new Uri(elasticOption.Host));
        if (elasticOption.Id != "")
        {
            var key = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{elasticOption.Id}:{elasticOption.Password}"));
            settings = settings.Authentication(new ApiKey(key));
        }
        else
        {
            settings.Authentication(new BasicAuthentication(elasticOption.User, elasticOption.Password));
        }

        var esClient = new ElasticsearchClient(settings);
        services.AddSingleton(esClient);
        services.AddSingleton<IElasticService, ElasticService>();
        return services;
    }
}