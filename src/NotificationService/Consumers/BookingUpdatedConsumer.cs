using Calendar.Contracts.Events;
using MassTransit;

namespace NotificationService.Consumers;

public class BookingUpdatedConsumer(ILogger<BookingCreatedConsumer> logger) :
    IConsumer<BookingUpdated>
{
    public Task Consume(ConsumeContext<BookingUpdated> context)
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