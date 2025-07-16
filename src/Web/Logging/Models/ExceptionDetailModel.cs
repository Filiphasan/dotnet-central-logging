namespace Web.Logging.Models;

public sealed record ExceptionDetailModel
{
    public string? Type { get; set; }
    public string? Message { get; set; }
    public string? StackTrace { get; set; }
    public int? HResult { get; set; }
    public string? Source { get; set; }
    public string? HelpLink { get; set; }
    public Dictionary<string, string>? Data { get; set; }
    public ExceptionDetailModel? InnerException { get; set; }

    public string GetExceptionDetailedMessage()
    {
        return $"{Type}: {Message}: {StackTrace}: {HResult}: {Source}: {HelpLink}: {InnerException?.GetExceptionDetailedMessage()}";
    }
}