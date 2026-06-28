using Calendar.Booking.API.Data;
using Calendar.Contracts.Commands;
using Calendar.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Booking.API.Consumers;

/// <summary>
/// Каскадно удаляет все бронирования, связанные с событием (AppointmentId).
/// Срабатывает при удалении или обновлении события в Calendar API.
/// После удаления публикует <see cref="BookingsDeletedEvent"/>.
/// Идемпотентен: повторная команда для уже пустого набора — no-op.
/// </summary>
public class DeleteBookingsByAppointmentIdConsumer(
    BookingDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ILogger<DeleteBookingsByAppointmentIdConsumer> logger) : IConsumer<DeleteBookingsByEventIdCommand>
{
    public async Task Consume(ConsumeContext<DeleteBookingsByEventIdCommand> context)
    {
        var appointmentId = context.Message.AppointmentId;

        var linked = await dbContext.Bookings
            .Where(b => b.AppointmentId == appointmentId)
            .ToListAsync(context.CancellationToken);

        if (linked.Count == 0)
        {
            logger.LogDebug("Нет бронирований для удаления: AppointmentId={AppointmentId}", appointmentId);
            return;
        }

        dbContext.Bookings.RemoveRange(linked);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        await publishEndpoint.Publish(new BookingsDeletedEvent(
            AppointmentId: appointmentId,
            Count: linked.Count), context.CancellationToken);

        logger.LogInformation(
            "Удалено бронирований: {Count} для события {AppointmentId}",
            linked.Count, appointmentId);
    }
}
