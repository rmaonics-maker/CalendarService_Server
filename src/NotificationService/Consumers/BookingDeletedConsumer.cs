using Calendar.Contracts.Events;
using MassTransit;

namespace NotificationService.Consumers;

public class BookingDeletedConsumer(ILogger<BookingDeletedConsumer> logger) :
    IConsumer<BookingDeletedEvent>
{
    public Task Consume(ConsumeContext<BookingDeletedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation("[БРОНИРОВАНИЕ УДАЛЕНО] ID: {BookingId}, Пользователь: {UserId}",
            message.BookingId,
            message.UserId);

        return Task.CompletedTask;
    }
}