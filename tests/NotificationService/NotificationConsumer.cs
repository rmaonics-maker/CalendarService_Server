using Calendar.Contracts;
using MassTransit;

namespace NotificationService;

public class NotificationConsumer(ILogger<NotificationConsumer> logger) : IConsumer<EventCreated>
{
    private readonly ILogger<NotificationConsumer> _logger = logger;

    public Task Consume(ConsumeContext<EventCreated> context)
    {
        var message = context.Message;

        // Симуляция отправки письма
        _logger.LogInformation(
            "📧 [EMAIL SENT] To: {Email}. Subject: '{Title}'. Event ID: {Id}",
            message.UserEmail,
            message.Title,
            message.EventId
        );

        return Task.CompletedTask;
    }
}
