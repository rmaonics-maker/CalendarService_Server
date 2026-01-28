using Calendar_Service.Contracts;
using Calendar_Service.Data;
using Calendar_Service.Models;
using Calendar_Service.Specifications;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Calendar_Service.Services;

public class PostgresCalendarService(ApplicationDbContext context) : ICalendarService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Guid> CreateEventAsync(CalendarEvent evt, CancellationToken token)
    {
        const decimal Price = 10.0m; // Заглушка для цены
        
        // 1. начало транзакции
        using var transaction = await _context.Database.BeginTransactionAsync(token);

        try
        {
            // 2. списание денег
            var userId = evt.UserId;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, token) ?? throw new Exception($"User not found. Я искал ID: '{userId}'. (В базе такого нет)");

            if (user.Balance < Price)
            {
                throw new InvalidOperationException("Not enough mineralz (money).");
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

            _context.Bookings.Add(booking);

            // 4. сохранение изменений
            await _context.SaveChangesAsync(token);

            // 5. фиксация
            await transaction.CommitAsync(token);
            // Возвращаем ID созданной сущности
            return booking.Id;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    public IEnumerable<CalendarEvent> GetOccurrences(DateTimeOffset from, DateTimeOffset to)
    {
        var bookings = _context.Bookings
            .AsNoTracking()
            .InPeriod(from, to)
            .OrderBy(b => b.Period.LowerBound)
            .ToList();
        return bookings.Select(MapToDomain);
    }

    // МЕТОД ЧТЕНИЯ (Одиночный)
    public CalendarEvent? GetEvent(Guid id)
    {
        var booking = _context.Bookings
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

    // Метод удаления (если был в интерфейсе)
    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken token)
    {
        var booking = _context.Bookings.Find(id);
        if (booking == null) return false;

        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync(token);
        return true;
    }

    public async Task<bool> UpdateEventAsync(Guid id, UpdateEventRequest request, CancellationToken token)
    {
        // 1. ЗАГРУЗКА (Tracking включен по умолчанию)
        // Мы тянем сущность из базы. ChangeTracker делает "снимок" её состояния.
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id, token);

        if (booking == null)
        {
            return false;
        }

        // 2. МОДИФИКАЦИЯ
        // Мы просто меняем свойства C# объекта.
        booking.Title = request.Title;

        // Если меняем длительность, нужно пересчитать Period (tsrange)
        // Вспоминаем наш хак с UtcUnspecified для базы
        if (request.Duration != TimeSpan.Zero)
        {
            var startUnspecified = booking.Period.LowerBound; // Текущее начало
            var newEndUnspecified = startUnspecified.Add(request.Duration);

            booking.Period = new NpgsqlRange<DateTime>(startUnspecified, newEndUnspecified);
        }

        // 3. СОХРАНЕНИЕ
        // Вызываем SaveChanges. 
        // EF Core сравнивает текущий объект со "снимком". 
        // Он видит, что Title изменился, и генерирует SQL: UPDATE bookings SET Title = ... WHERE Id = ...
        await _context.SaveChangesAsync(token);

        return true;
    }

    // Санитайзер для дат. Превращает "2026-06-15 14:00 UTC" в "2026-06-15 14:00 Unspecified"
    private static DateTime EnsureUtcUnspecified(DateTime dt)
    {
        return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
    }

    private static CalendarEvent MapToDomain(Booking b)
    {
        // При чтении из базы мы знаем, что храним там UTC.
        // Поэтому восстанавливаем Kind = Utc перед созданием DateTimeOffset.
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
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Bookings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == userId, token);

        if (user == null) return null;

        return new UserHistoryResponse
        {
            UserId = user.Id,
            Name = user.Username,
            Balance = user.Balance,
            Events = user.Bookings
                .Select(MapToDomain)
                .ToList()
        };
    }
}

