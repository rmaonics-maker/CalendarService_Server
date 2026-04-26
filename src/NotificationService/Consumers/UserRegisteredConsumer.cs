using MassTransit;
using Calendar.Contracts.Events;
using NotificationService.Data;
using Microsoft.EntityFrameworkCore;
using NotificationService.Services;
using NotificationService.Entities;

namespace NotificationService.Consumers;
public class UserRegisteredConsumer(
    NotificationDbContext dbContext,
    IEmailService emailService,
    ILogger<UserRegisteredConsumer> logger) : IConsumer<UserRegisteredEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Получено событие регистрации для пользователя: {UserId}", msg.Id);
        var alreadySent = await dbContext.EmailLogs
        .AnyAsync(l => l.UserId == msg.Id && l.EmailType == "Welcome");

        if(alreadySent) return;

        await emailService.SendWelcomeEmailAsync(msg.Email);

        dbContext.EmailLogs.Add(new EmailLog
        {
            UserId = msg.Id, 
            EmailType = "Welcome", 
            SentAt = DateTimeOffset.UtcNow 
        });
        await dbContext.SaveChangesAsync();
    }
}