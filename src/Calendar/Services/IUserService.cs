using Calendar_Service.Models;
using Calendar.Contracts;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Responses;

namespace Calendar_Service.Services;

public interface IUserService
{
    Task<Guid> CreateUserAsync(UserDto user, CancellationToken token = default);
    Task<IEnumerable<HistoryItemDto>> GetUserHistoryAsync(Guid userId, CancellationToken token = default);
    Task<UserDto?> GetUserAsync(Guid id, CancellationToken token = default);
    Task<bool> UpdateBalanceAsync(Guid userId, decimal newBalance, CancellationToken token = default);
    Task<bool> DeductBalanceAsync(Guid userId, decimal amount, CancellationToken token = default);
    Task<bool> HasSufficientBalanceAsync(Guid userId, decimal amount, CancellationToken token = default);
}