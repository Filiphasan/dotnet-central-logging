namespace Shared.Models.Options;

public class MessagingOptions
{
    public const string SectionName = "Messaging";

    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string User { get; set; }
    public required string Password { get; set; }
    public required string ConnectionName { get; set; }
    public int PoolSize { get; set; } = 20;
}