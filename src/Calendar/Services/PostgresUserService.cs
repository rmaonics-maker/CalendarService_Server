using AutoMapper;
using Calendar_Service.Data;
using Calendar_Service.Models;
using Calendar.Contracts;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Responses;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Calendar_Service.Services;

public class PostgresUserService(
    ApplicationDbContext context, 
    ILogger<PostgresUserService> logger,
    IMapper mapper,
    IPublishEndpoint publishEndpoint) :
    IUserService
{
    public async Task<Guid> CreateUserAsync(UserDto user, CancellationToken token = default)
    {
        var userEntity = mapper.Map<User>(user);
        userEntity.Balance = 0;
        context.Users.Add(userEntity);
        await context.SaveChangesAsync(token);

        logger.LogInformation("Создан пользователь: ID={UserId}, Email={Email}", user.Id, user.Email);

        return user.Id;
    }

    public async Task<IEnumerable<HistoryItemDto>> GetUserHistoryAsync(Guid userId, CancellationToken token = default)
    {
        // TODO: Получать бронирования из Calendar.Booking.API через MassTransit
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, token);

        if (user == null) return [];

        return []; // TODO: Загрузить из Booking.API и вернуть объединённый список
    }

    public async Task<UserDto?> GetUserAsync(Guid id, CancellationToken token = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, token);
        
        return mapper.Map<UserDto>(user);
    }

    public async Task<bool> UpdateBalanceAsync(Guid userId, decimal newBalance, CancellationToken token = default)
    {
        var user = await context.Users.FindAsync([userId], token);
        if (user == null) return false;

        user.Balance = newBalance;
        await context.SaveChangesAsync(token);

        logger.LogInformation("Баланс обновлён: UserID={UserId}, NewBalance={Balance}", userId, newBalance);

        return true;
    }

    public async Task<bool> DeductBalanceAsync(Guid userId, decimal amount, CancellationToken token = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            var user = await context.Users.FindAsync([userId], token);
            if (user == null) return false;

            if (user.Balance < amount)
            {
                logger.LogWarning(
                    "Недостаточно средств: UserID={UserId}, Balance={Balance}, Required={Required}",
                    userId, user.Balance, amount);
                return false;
            }

            user.Balance -= amount;
            await context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);

            logger.LogInformation(
                "Списаны средства: UserID={UserId}, Amount={Amount}, NewBalance={Balance}",
                userId, amount, user.Balance);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(token);
            logger.LogError(ex, "Ошибка при списании средств: UserID={UserId}", userId);
            throw;
        }
    }

    public async Task<bool> HasSufficientBalanceAsync(Guid userId, decimal amount, CancellationToken token = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, token);

        return user != null && user.Balance >= amount;
    }
}