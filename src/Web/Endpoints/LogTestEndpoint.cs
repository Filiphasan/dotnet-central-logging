using Carter;

namespace Web.Endpoints;

public class LogTestEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/log-tests")
            .WithTags("Log Test Endpoints");

        group.MapPost("multi-thread", MultiThreadTestAsync);
        group.MapPost("debug", DebugTestAsync);
        group.MapPost("trace", TraceTestAsync);
        group.MapPost("info", InfoTestAsync);
        group.MapPost("warning", WarningTestAsync);
        group.MapPost("error", ErrorTestAsync);
        group.MapPost("critical", CriticalTestAsync);
    }

    private static async Task<IResult> MultiThreadTestAsync(ILogger<LogTestEndpoint> logger)
    {
        await Parallel.ForAsync(0, 1000, (i, _) =>
        {
            var mod = i % 6;

            if (mod == 0)
            {
                logger.LogDebug("Test Debug Log Index: {Index}", i);
            }
            else if (mod == 1)
            {
                logger.LogTrace("Test Trace Index: {Index}", i);
            }
            else if (mod == 2)
            {
                logger.LogInformation("Test Information Index: {Index}", i);
            }
            else if (mod == 3)
            {
                logger.LogWarning("Test Warning Index: {Index}", i);
            }
            else if (mod == 4)
            {
                logger.LogError(new Exception("Test Exception"), "Test Error Index: {Index}", i);
            }
            else if (mod == 5)
            {
                logger.LogCritical("Test Critical Index: {Index}", i);
            }

            return ValueTask.CompletedTask;
        });
        
        return Results.Ok();
    }

    private static async Task<IResult> DebugTestAsync(string message, ILogger<LogTestEndpoint> logger)
    {
        logger.LogDebug("Test Debug Message: {Message}", message);
        return Results.Ok();
    }

    private static async Task<IResult> TraceTestAsync(string message, ILogger<LogTestEndpoint> logger)
    {
        logger.LogTrace("Test Trace Message: {Message}", message);
        return Results.Ok();
    }
    
    private static async Task<IResult> InfoTestAsync(string message, ILogger<LogTestEndpoint> logger)
    {
        logger.LogInformation("Test Information Message: {Message}", message);
        return Results.Ok();
    }

    private static async Task<IResult> WarningTestAsync(string message, ILogger<LogTestEndpoint> logger)
    {
        logger.LogWarning("Test Warning Message: {Message}", message);
        return Results.Ok();
    }

    private static async Task<IResult> ErrorTestAsync(string message, ILogger<LogTestEndpoint> logger)
    {
        var exception = new Exception("Test Exception");
        logger.LogError(exception, "Test Error Message: {Message}", message);
        return Results.Ok();
    }

    private static async Task<IResult> CriticalTestAsync(string message, ILogger<LogTestEndpoint> logger)
    {
        logger.LogCritical("Test Critical Message: {Message}", message);
        return Results.Ok();
    }
}