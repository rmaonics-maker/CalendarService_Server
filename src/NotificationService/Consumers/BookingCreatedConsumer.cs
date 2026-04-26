using Calendar.Contracts.Events;
using MassTransit;

namespace NotificationService.Consumers;

public class BookingCreatedConsumer(ILogger<BookingCreatedConsumer> logger) :
    IConsumer<BookingCreatedEvent>
{
    public Task Consume(ConsumeContext<BookingCreatedEvent> context)
    {
        var message = context.Message;
        
        logger.LogInformation("[БРОНИРОВАНИЕ СОЗДАНО] ID: {BookingId}, Название: '{Title}', Пользователь: {UserId}, Комната: {RoomId}",
            message.BookingId,
            message.Title,
            message.UserId,
            message.RoomId);

        return Task.CompletedTask;
    }
}