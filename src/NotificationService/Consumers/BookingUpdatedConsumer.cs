using Calendar.Contracts.Events;
using MassTransit;

namespace NotificationService.Consumers;

public class BookingUpdatedConsumer(ILogger<BookingUpdatedConsumer> logger) :
    IConsumer<BookingUpdatedEvent>
{
    public Task Consume(ConsumeContext<BookingUpdatedEvent> context)
    {
        var message = context.Message;
        
        logger.LogInformation("[БРОНИРОВАНИЕ ОБНОВЛЕНО] ID: {BookingId}, Название: '{Title}', Начало: {UserId}, Комната: {RoomId}",
            message.BookingId,
            message.NewTitle,
            message.NewStartTimeUtc,
            message.NewRoomId);

        return Task.CompletedTask;
    }
}