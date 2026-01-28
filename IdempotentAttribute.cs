using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace Calendar_Service;

public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var value))
        {
            context.Result = new BadRequestObjectResult("Idempotency key is missing");
            return;
        }
        
        var key = value.ToString();
        var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

        if (cache.TryGetValue(key, out IdempotencyRecord? idempotencyRecord))
        {
            context.Result = new ObjectResult(idempotencyRecord?.Body)
            {
                StatusCode = idempotencyRecord?.StatusCode
            };
            return;
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult objectResult)
        {
            var record = new IdempotencyRecord(objectResult.StatusCode??200, objectResult.Value);
            cache.Set(key, record, TimeSpan.FromHours(24));
        }
    }
}

public record IdempotencyRecord(int StatusCode, object? Body);