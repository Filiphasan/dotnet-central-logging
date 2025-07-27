namespace Web.Middlewares;

public class CorrelationMiddleware : IMiddleware
{
    public const string CorrelationIdKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationIdKey, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[CorrelationIdKey] = correlationId;
        }

        await next(context);
    }
}