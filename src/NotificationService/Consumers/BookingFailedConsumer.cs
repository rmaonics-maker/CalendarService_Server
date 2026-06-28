using Calendar.Contracts.Events;
using MassTransit;

namespace NotificationService.Consumers;

/// <summary>
/// Логирует неудачные попытки бронирования (комната занята / нет блокировки).
/// Событие публикуется Booking API, когда BookRoomCommand не удалось выполнить.
/// </summary>
public class BookingFailedConsumer(ILogger<BookingFailedConsumer> logger) :
    IConsumer<BookingFailedEvent>
{
    public Task Consume(ConsumeContext<BookingFailedEvent> context)
    {
        var message = context.Message;

        logger.LogWarning(
            "[БРОНИРОВАНИЕ НЕ УДАЛОСЬ] AppointmentId: {AppointmentId}, Комната: {RoomId}, " +
            "Пользователь: {UserId}, Слот: {Start:o}. Причина: {Reason}",
            message.AppointmentId,
            message.RoomId,
            message.UserId,
            message.OccurrenceStartTimeUtc,
            message.Reason);

        return Task.CompletedTask;
    }
}
