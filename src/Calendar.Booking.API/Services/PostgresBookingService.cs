using AutoMapper;
using AutoMapper.QueryableExtensions;
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
    ILogger<PostgresBookingService> logger,
    IMapper mapper) : IBookingService
{
    public async Task<IEnumerable<BookingDto>> GetBookingsAsync(Guid userId, CancellationToken token = default)
    {
        var bookings = await context.Bookings
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.Period.LowerBound)
            .ProjectTo<BookingDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);

        return mapper.Map<IEnumerable<BookingDto>>(bookings);
    }

    public async Task<(Guid?, BookingDto?)> GetBookingAsync(Guid id, CancellationToken token = default)
    {
        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id, token);

        if (booking is null) return (null, null);
        return (booking.UserId, mapper.Map<BookingDto>(booking));
    }

    public async Task<BookingDto> CreateBookingAsync(Guid userId, BookingDto bookingDto, CancellationToken token = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            var bookingModel = mapper.Map<BookingModel>(bookingDto);
            bookingModel.UserId = userId;
            context.Bookings.Add(bookingModel);
            await context.SaveChangesAsync(token);

            // Публикация события через Outbox
            await publishEndpoint.Publish(new BookingCreatedEvent
            (
                BookingId: bookingModel.Id,
                Title: bookingModel.Title,
                UserId: bookingModel.UserId,
                RoomId: bookingModel.RoomId,
                StartTimeUtc: bookingModel.Period.LowerBound,
                EndTimeUtc: bookingModel.Period.UpperBound,
                AppointmentId: bookingModel.AppointmentId,
                OccurredAt: DateTime.UtcNow
            ), token);

            await transaction.CommitAsync(token);

            logger.LogInformation(
                "Создано бронирование: ID={BookingId}, Title='{Title}', UserId={UserId}, RoomId={RoomId}",
                bookingModel.Id, bookingModel.Title, bookingModel.UserId, bookingModel.RoomId);
            
            // смапить модель в дто
            return mapper.Map<BookingDto>(bookingModel);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(token);
            logger.LogError(ex, "Ошибка при создании бронирования");
            throw;
        }
    }

    public async Task<BookingDto?> UpdateBookingAsync(Guid id, BookingDto bookingDto, CancellationToken token = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            var booking = await context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id, token);

            if (booking is null)
                return null;

            var oldTitle = booking.Title;
            var oldRoomId = booking.RoomId;
            var oldPeriod = booking.Period;

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
                "Обновлено бронирование: ID={BookingId}, Title='{OldTitle}' -> '{NewTitle}', " +
                "RoomId={OldRoomId} -> {NewRoomId}, Period={OldPeriod} -> {NewPeriod}",
                booking.Id, oldTitle, booking.Title, oldRoomId, booking.RoomId, oldPeriod, booking.Period);

            return mapper.Map<BookingDto>(booking);
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
                AppointmentId: booking.AppointmentId,
                RefundAmount: 10m, // TODO: добавить стоимость
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

    private static DateTime EnsureUtcUnspecified(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Unspecified)
            return dateTime;

        return DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
    }
}