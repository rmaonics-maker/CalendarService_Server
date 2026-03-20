using Calendar.Contracts.Events;
using MassTransit;

namespace NotificationService.Consumers;

public class BookingDeletedConsumer(ILogger<BookingCreatedConsumer> logger) :
    IConsumer<BookingDeleted>
{
    public Task Consume(ConsumeContext<BookingDeleted> context)
    {
        var message = context.Message;

        logger.LogInformation("[БРОНИРОВАНИЕ УДАЛЕНО] ID: {BookingId}, Пользователь: {UserId}",
            message.BookingId,
            message.UserId);

        return Task.CompletedTask;
    }
}