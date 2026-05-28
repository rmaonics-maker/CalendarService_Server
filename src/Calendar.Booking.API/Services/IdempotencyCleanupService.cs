using Calendar.Booking.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Booking.API.Services;

public class IdempotencyCleanupService(
    IServiceProvider serviceProvider,
    TimeProvider timeProvider,
    ILogger<IdempotencyCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            
            await using var scope = serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var now = timeProvider.GetUtcNow();

            var deleted = await dbContext.IdempotencyKeys
                .Where(k => k.ExpiresAt < now)
                .ExecuteDeleteAsync(stoppingToken);
            
            if(deleted > 0)
                logger.LogInformation("Удалено {Count} истёкших ключей идемпотентности", deleted);
        }
    }
}