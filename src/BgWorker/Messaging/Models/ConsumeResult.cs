namespace BgWorker.Messaging.Models;

public class ConsumeResult
{
    public ConsumeResultType Result { get; set; } = ConsumeResultType.Done;
    public string? Reason { get; set; } = null;
    public int? Delay { get; set; } = null;

    private ConsumeResult()
    {
    }

    public static ConsumeResult Done => new();

    public static ConsumeResult Retry(string? reason = null)
    {
        return new ConsumeResult
        {
            Result = ConsumeResultType.Retry,
            Reason = reason
        };
    }

    public static ConsumeResult Delayed(int? delay = null, string? reason = null)
    {
        return new ConsumeResult
        {
            Result = ConsumeResultType.Delayed,
            Delay = delay,
            Reason = reason
        };
    }

    public static ConsumeResult DeadLetter(string? reason = null)
    {
        return new ConsumeResult
        {
            Result = ConsumeResultType.DeadLetter,
            Reason = reason
        };
    }
}

public enum ConsumeResultType
{
    Done = 1,
    Retry = 2,
    Delayed = 3,
    DeadLetter = 4,
}