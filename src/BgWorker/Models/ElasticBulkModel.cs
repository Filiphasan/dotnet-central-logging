namespace BgWorker.Models;

public class ElasticBulkModel<T> where T : IElasticBulkModel
{
    public string? Index { get; set; }
    public List<T> List { get; set; } = [];
}

public interface IElasticBulkModel
{
    public string Id { get; set; }
}