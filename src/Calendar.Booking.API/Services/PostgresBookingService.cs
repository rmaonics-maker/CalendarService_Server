using Calendar.Booking.API.Data;
using Calendar.Booking.API.Models;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Calendar.Booking.API.Services;

public class PostgresBookingService(
    BookingDbContext context,
    IPublishEndpoint publishEndpoint,
    ILogger<PostgresBookingService> logger) : IBookingService
{
    public async Task<IEnumerable<BookingDto>> GetBookingsAsync(Guid userId, CancellationToken token = default)
    {
        var bookings = await context.Bookings
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.Period.LowerBound)
            .ToListAsync(token);

        return bookings.Select(MapToDto);
    }

    public async Task<BookingDto?> GetBookingAsync(Guid id, CancellationToken token = default)
    {
        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id, token);

        return booking is not null ? MapToDto(booking) : null;
    }

    public async Task<BookingDto> CreateBookingAsync(BookingDto bookingDto, CancellationToken token = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            var booking = new BookingModel
            {
                Id = Guid.NewGuid(),
                Title = bookingDto.Title,
                UserId = bookingDto.UserId,
                RoomId = bookingDto.RoomId,
                Period = new NpgsqlRange<DateTime>(
                    EnsureUtcUnspecified(bookingDto.StartTimeUtc),
                    EnsureUtcUnspecified(bookingDto.EndTimeUtc)
                ),
                CreatedAtUtc = DateTime.UtcNow,
                Version = 0
            };

            context.Bookings.Add(booking);
            await context.SaveChangesAsync(token);

            // Публикация события через Outbox
            await publishEndpoint.Publish(new BookingCreatedEvent
            (
                BookingId: booking.Id,
                Title: booking.Title,
                UserId: booking.UserId,
                RoomId: booking.RoomId,
                StartTimeUtc: booking.Period.LowerBound,
                EndTimeUtc: booking.Period.UpperBound,
                AppointmentId: Guid.Empty, // TODO добавить потом
                OccurredAt: DateTime.UtcNow
            ), token);

            await transaction.CommitAsync(token);

            logger.LogInformation(
                "Создано бронирование: ID={BookingId}, Title='{Title}', UserId={UserId}, RoomId={RoomId}",
                booking.Id, booking.Title, booking.UserId, booking.RoomId);

            return MapToDto(booking);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(token);
            logger.LogError(ex, "Ошибка при создании бронирования");
            throw;
        }
    }

    public async Task<bool> UpdateBookingAsync(Guid id, BookingDto bookingDto, CancellationToken token = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            var booking = await context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id, token);

            if (booking is null)
                return false;

            var oldTitle = booking.Title;
            var oldRoomId = booking.RoomId;
            //var oldPeriod = booking.Period;

            booking.Title = bookingDto.Title;
            booking.RoomId = bookingDto.RoomId;
            booking.Period = new NpgsqlRange<DateTime>(
                EnsureUtcUnspecified(bookingDto.StartTimeUtc),
                EnsureUtcUnspecified(bookingDto.EndTimeUtc)
            );

            await context.SaveChangesAsync(token);

            // Публикация события через Outbox
            await publishEndpoint.Publish(new BookingUpdatedEvent
            (
                BookingId: booking.Id,
                NewTitle: booking.Title,
                NewRoomId: booking.RoomId,
                NewStartTimeUtc: booking.Period.LowerBound,
                NewEndTimeUtc: booking.Period.UpperBound,
                OccurredAt: DateTime.UtcNow
            ), token);

            await transaction.CommitAsync(token);

            logger.LogInformation(
                "Обновлено бронирование: ID={BookingId}, Title='{OldTitle}' -> '{NewTitle}', RoomId={OldRoomId} -> {NewRoomId}",
                booking.Id, oldTitle, booking.Title, oldRoomId, booking.RoomId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(token);
            logger.LogError(ex, "Ошибка при обновлении бронирования {BookingId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteBookingAsync(Guid id, CancellationToken token = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            var booking = await context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id, token);

            if (booking is null)
                return false;

            var userId = booking.UserId;
            context.Bookings.Remove(booking);
            await context.SaveChangesAsync(token);

            // Публикация события через Outbox
            await publishEndpoint.Publish(new BookingDeletedEvent
            (
                BookingId: id,
                UserId: userId,
                AppointmentId: Guid.Empty,  // TODO добавить потом
                OccurredAt: DateTime.UtcNow
            ), token);

            await transaction.CommitAsync(token);

            logger.LogInformation("Удалено бронирование: ID={BookingId}, UserId={UserId}", id, userId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(token);
            logger.LogError(ex, "Ошибка при удалении бронирования {BookingId}", id);
            throw;
        }
    }

    private static BookingDto MapToDto(BookingModel booking)
    {
        return new BookingDto
        (
            Id: booking.Id,
            Title: booking.Title,
            UserId: booking.UserId,
            RoomId: booking.RoomId,
            StartTimeUtc: booking.Period.LowerBound,
            EndTimeUtc: booking.Period.UpperBound,
            CreatedAtUtc: booking.CreatedAtUtc,
            AppointmentId: Guid.Empty   // TODO добавить потом
        );
    }

    private static DateTime EnsureUtcUnspecified(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Unspecified)
            return dateTime;

        return DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
    }
}