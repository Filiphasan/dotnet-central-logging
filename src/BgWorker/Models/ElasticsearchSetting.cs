namespace BgWorker.Models;

public class ElasticsearchSetting
{
    public const string SectionName = "Elasticsearch";

    public required string Host { get; set; }
    public required string User { get; set; }
    public required string Password { get; set; }
    public required string Id { get; set; }
    public required string Key { get; set; }
}