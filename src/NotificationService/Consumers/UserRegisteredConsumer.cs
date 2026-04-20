using MassTransit;
using Calendar.Contracts.Events;
using NotificationService.Data;
using Microsoft.EntityFrameworkCore;
using NotificationService.Services;
using NotificationService.Entities;

namespace NotificationService.Consumers;
public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserRegisteredConsumer> _logger;
    
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Получено событие регистрации для пользователя: {UserId}", msg.Id);
        var alreadySent = await _dbContext.EmailLogs
        .AnyAsync(l => l.UserId == msg.Id && l.EmailType == "Welcome");

        if(alreadySent) return;

        await _emailService.SendWelcomeEmailAsync(msg.UserEmail);

        _dbContext.EmailLogs.Add(new EmailLog
        {
            UserId = msg.Id, 
            EmailType = "Welcome", 
            SentAt = DateTimeOffset.UtcNow 
        });
        await _dbContext.SaveChangesAsync();
    }
}