using System.Text.Json;
using Calendar.Booking.API.Data;
using Calendar.Booking.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Calendar.Booking.API.Attributes;

public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var value))
        {
            context.Result = new BadRequestObjectResult("Idempotency-Key header is required");
            return;
        }

        var key = value.ToString();

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<BookingDbContext>();
        
        var existingKey = await dbContext.IdempotencyKeys.FindAsync(key);
        if (existingKey is not null && existingKey.ExpiresAt > DateTimeOffset.UtcNow)   // TODO заменить на TimeProvider
        {
            context.Result = new ObjectResult(JsonSerializer.Deserialize<object>(existingKey.ResponseBody))
                {StatusCode = existingKey.StatusCode};
            return;
        }
        
        var executedContent = await next();

        if (executedContent.Result is ObjectResult { StatusCode: >= 200 and < 300 } objectResult)
        {
            dbContext.IdempotencyKeys.Add(new IdempotencyKeyModel
            {
                Key = key,
                StatusCode = objectResult.StatusCode ?? 200,
                ResponseBody = JsonSerializer.Serialize(objectResult.Value),
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
            });
            
            await dbContext.SaveChangesAsync(context.HttpContext.RequestAborted);
        }
    }
}