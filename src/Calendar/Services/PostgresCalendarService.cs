using Calendar.Contracts;
using Calendar_Service.Data;
using Calendar_Service.Models;
using Calendar_Service.Specifications;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Calendar_Service.Services;

public class PostgresCalendarService(ApplicationDbContext context, ILogger<PostgresCalendarService> logger) 
    : ICalendarService
{
    public async Task<(Guid Id , string Email)> CreateEventAsync(CalendarEvent evt, IPublishEndpoint publishEndpoint,
        CancellationToken token)
    {
        const decimal Price = 10.0m; // Заглушка для цены
        
        // 1. начало транзакции
        await using var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            // 2. списание денег
            var userId = evt.UserId;

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, token) ?? 
            throw new Exception($"User not found. Я искал ID: '{userId}'. (В базе такого нет)");

            if (user.Balance < Price)
            {
                throw new InvalidOperationException($"Not enough money. User '{user.Id}' has {user.Balance}, but {Price} is required.");
            }

            user.Balance -= Price;

            // 3. создание брони
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                Title = evt.Title,
                RoomId = 1, 
                UserId = evt.UserId,
                Period = new NpgsqlRange<DateTime>(
                EnsureUtcUnspecified(evt.StartTime.UtcDateTime),
                EnsureUtcUnspecified(evt.Duration == TimeSpan.Zero
                    ? evt.StartTime.UtcDateTime.AddHours(1)
                    : evt.StartTime.UtcDateTime.Add(evt.Duration))
            ),

                Version = 0
            };

            context.Bookings.Add(booking);
            
            await publishEndpoint.Publish(new EventCreated
            {
                EventId = booking.Id,
                Title = evt.Title,
                UserId = evt.UserId,
                UserEmail = user.Email,
                OccurredAt = DateTime.UtcNow
            }, token);

            // 4. сохранение изменений
            await context.SaveChangesAsync(token);
            
            // 5. фиксация
            await transaction.CommitAsync(token);
            // Возвращаем ID созданной сущности
            return (booking.Id, user.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while creating an event.");
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    public IEnumerable<CalendarEvent> GetOccurrences(DateTimeOffset from, DateTimeOffset to)
    {
        var bookings = context.Bookings
            .AsNoTracking()
            .InPeriod(from, to)
            .OrderBy(b => b.Period.LowerBound)
            .ToList();
        return bookings.Select(MapToDomain);
    }

    public CalendarEvent? GetEvent(Guid id)
    {
        var booking = context.Bookings
            .AsNoTracking()
            .FirstOrDefault(b => b.Id == id);

        if (booking == null) return null;

        // Mapping
        return new CalendarEvent
        {
            Id = booking.Id,
            Title = booking.Title,
            StartTime = new DateTimeOffset(booking.Period.LowerBound, TimeSpan.Zero),
            Duration = booking.Period.UpperBound - booking.Period.LowerBound
        };
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken token)
    {
        var booking = context.Bookings.Find(id);
        if (booking == null) return false;

        context.Bookings.Remove(booking);
        await context.SaveChangesAsync(token);
        return true;
    }

    public async Task<bool> UpdateEventAsync(Guid id, UpdateEventRequest request, CancellationToken token)
    {
        // 1. ЗАГРУЗКА (Tracking включен по умолчанию)
        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == id, token);

        if (booking == null)
        {
            return false;
        }

        // 2. МОДИФИКАЦИЯ
        booking.Title = request.Title;

        if (request.Duration != TimeSpan.Zero)
        {
            var startUnspecified = booking.Period.LowerBound; 
            var newEndUnspecified = startUnspecified.Add(request.Duration);

            booking.Period = new NpgsqlRange<DateTime>(startUnspecified, newEndUnspecified);
        }

        // 3. СОХРАНЕНИЕ
        await context.SaveChangesAsync(token);

        return true;
    }

    private static DateTime EnsureUtcUnspecified(DateTime dt)
    {
        return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
    }

    private static CalendarEvent MapToDomain(Booking b)
    {
        var startUnspecified = b.Period.LowerBound;
        var endUnspecified = b.Period.UpperBound;

        var startUtc = DateTime.SpecifyKind(startUnspecified, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(endUnspecified, DateTimeKind.Utc);

        return new CalendarEvent
        {
            Id = b.Id,
            Title = b.Title,
            StartTime = new DateTimeOffset(startUtc),
            Duration = endUtc - startUtc
        };
    }

    public async Task<UserHistoryResponse?> GetUserHistoryAsync(Guid userId, CancellationToken token)
    {
        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Bookings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == userId, token);

        if (user == null) return null;

        return new UserHistoryResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Balance = user.Balance,
            
            Events = [.. user.Bookings.Select(b =>
            {
                // 1. Восстанавливаем UTC Kind (Npgsql возвращает Unspecified)
                var startUtc = DateTime.SpecifyKind(b.Period.LowerBound, DateTimeKind.Utc);
                var endUtc = DateTime.SpecifyKind(b.Period.UpperBound, DateTimeKind.Utc);

                // 2. Создаем DTO
                return new UserHistoryResponse.HistoryEventDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Location = "Room " + b.RoomId, 
                    StartTime = new DateTimeOffset(startUtc),
                    EndTime = new DateTimeOffset(endUtc)
                };
            })]
        };
    }

    public async Task<Guid> CreateUserAsync(User user, CancellationToken token)
    {
        
        context.Users.Add(user);
        await context.SaveChangesAsync(token);
        return user.Id;
    }
}
