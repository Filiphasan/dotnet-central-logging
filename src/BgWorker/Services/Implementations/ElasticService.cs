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
        if (response.IsValidResponse)
        {
            return [];
        }
        
        if (response.ElasticsearchServerError is not null)
        {
            logger.LogWarning("{MethodName} BulkAsync failed, Status: {Status} Error: {Error}",
                MethodName, response.ElasticsearchServerError.Status, response.ElasticsearchServerError.Error.ToString());
        }

        var failedList = new List<T>();
        foreach (var item in response.ItemsWithErrors)
        {
            var data = model.List.First(x => x.Id == item.Id);
            failedList.Add(data);

            logger.LogWarning("Bulk operation failed for document ID: {DocumentId} in index: {Index}. Reason: {ErrorReason}",
                item.Id, item.Index, item.Error?.Reason);
        }

        return failedList;
    }
}