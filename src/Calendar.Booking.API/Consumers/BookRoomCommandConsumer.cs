using Calendar.Booking.API.Services;
using Calendar.Contracts.Commands;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Events;
using MassTransit;

namespace Calendar.Booking.API.Consumers;

/// <summary>
/// Обрабатывает команду бронирования комнаты от Calendar API.
/// Создаёт бронирование через существующий <see cref="IBookingService.CreateBookingAsync"/>
/// (с распределительной блокировкой Redis + GiST-проверкой пересечений + outbox-публикацией).
/// При конфликте (комната занята / нет блокировки) публикует <see cref="BookingFailedEvent"/>.
/// </summary>
public class BookRoomCommandConsumer(
    IBookingService bookingService,
    IPublishEndpoint publishEndpoint,
    ILogger<BookRoomCommandConsumer> logger,
    TimeProvider timeProvider) : IConsumer<BookRoomCommand>
{
    public async Task Consume(ConsumeContext<BookRoomCommand> context)
    {
        var msg = context.Message;

        logger.LogInformation(
            "Получена команда бронирования: AppointmentId={AppointmentId}, RoomId={RoomId}, " +
            "Слот={Start:o} - {End:o}, OccurrenceKey={Key}",
            msg.AppointmentId, msg.RoomId, msg.StartTimeUtc, msg.EndTimeUtc, msg.OccurrenceKey);

        var dto = new BookingDto(
            Id: Guid.NewGuid(),
            Title: msg.Title,
            RoomId: msg.RoomId,
            StartTimeUtc: new DateTimeOffset(msg.StartTimeUtc, TimeSpan.Zero),
            EndTimeUtc: new DateTimeOffset(msg.EndTimeUtc, TimeSpan.Zero),
            CreatedAtUtc: timeProvider.GetUtcNow(),
            AppointmentId: msg.AppointmentId); // TODO смапить

        try
        {
            await bookingService.CreateBookingAsync(msg.UserId, dto, context.CancellationToken);

            logger.LogInformation(
                "Бронирование создано: AppointmentId={AppointmentId}, RoomId={RoomId}, Слот={Start:o}",
                msg.AppointmentId, msg.RoomId, msg.StartTimeUtc);
        }
        catch (InvalidOperationException ex)
        {
            // Конфликт (комната занята / нет блокировки) — не ретраим, публикуем событие сбоя.
            logger.LogWarning(ex,
                "Конфликт бронирования: AppointmentId={AppointmentId}, RoomId={RoomId}, Слот={Start:o}. Причина: {Reason}",
                msg.AppointmentId, msg.RoomId, msg.StartTimeUtc, ex.Message);

            await publishEndpoint.Publish(new BookingFailedEvent(
                AppointmentId: msg.AppointmentId,
                RoomId: msg.RoomId,
                UserId: msg.UserId,
                Reason: ex.Message,
                OccurrenceStartTimeUtc: msg.StartTimeUtc,
                OccurredAt: timeProvider.GetUtcNow()), context.CancellationToken); // TODO смапить
        }
    }
}
