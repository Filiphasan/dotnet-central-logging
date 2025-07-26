using BgWorker.Models;
using BgWorker.Services.Interfaces;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;

namespace BgWorker.Services.Implementations;

public class ElasticService(ElasticsearchClient client, ILogger<ElasticService> logger)
    : IElasticService
{
    private const string MethodName = nameof(ElasticService);

    public async Task IndexAsync<T>(ElasticIndexModel<T> model, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(model.Index))
        {
            return;
        }

        var response = await client.IndexAsync(model.Data, x => x.Index(model.Index), cancellationToken);
        if (!response.IsValidResponse)
        {
            logger.LogWarning("{MethodName} IndexAsync failed, Status: {Status} Error: {Error}",
                MethodName, response.ElasticsearchServerError?.Status, response.ElasticsearchServerError?.Error?.Reason);
        }
    }

    public async Task<List<T>> BulkAsync<T>(ElasticBulkModel<T> model, CancellationToken cancellationToken = default) where T : IElasticBulkModel
    {
        if (string.IsNullOrWhiteSpace(model.Index))
        {
            return [];
        }

        var request = new BulkRequest(index: model.Index)
        {
            Operations = new BulkOperationsCollection(model.List.Select(x => new BulkIndexOperation<T>(x)
            {
                Id = x.Id
            }))
        };
        var response = await client.BulkAsync(request, cancellationToken);
        if (!response.IsValidResponse)
        {
            logger.LogWarning("{MethodName} BulkAsync failed, Status: {Status} Error: {Error}",
                MethodName, response.ElasticsearchServerError?.Status, response.ElasticsearchServerError?.Error?.Reason);
        }

        return response.ItemsWithErrors
            .Where(x => !x.IsValid && int.TryParse(x.Index, out _))
            .Where(x => int.Parse(x.Index) >= 0 && int.Parse(x.Index) < model.List.Count)
            .Select(x => model.List[int.Parse(x.Index)])
            .ToList();
    }
}