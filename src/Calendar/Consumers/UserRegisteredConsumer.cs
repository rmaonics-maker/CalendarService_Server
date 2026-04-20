using MassTransit;
using Calendar.Contracts.Events;
using Calendar_Service.Data;
using Calendar_Service.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar_Service.Consumers;

public class UserRegisteredConsumer(ApplicationDbContext context, ILogger<UserRegisteredConsumer> logger) 
    : IConsumer<UserRegisteredEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredEvent> contextParams)
    {
        var message = contextParams.Message;
        
        var exists = await context.Users.AnyAsync(u => u.Id == message.Id, contextParams.CancellationToken);
        if (exists)
        {
            logger.LogInformation("Пользователь {UserId} уже существует в Calendar_Service", message.Id);
            return;
        }

        var newUser = new User
        {
            Id = message.Id,
            Email = message.Email,
            Name = message.Name,
            Balance = 0 
        };

        context.Users.Add(newUser);
        await context.SaveChangesAsync(contextParams.CancellationToken);
        
        logger.LogInformation("Пользователь {UserId} успешно синхронизирован в Calendar_Service", message.Id);
    }
}