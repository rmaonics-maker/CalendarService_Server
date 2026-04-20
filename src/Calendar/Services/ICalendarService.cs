using Calendar.Contracts;
using Calendar_Service.Models;
using Calendar.Contracts.Responses;
using MassTransit;

namespace Calendar_Service.Services;

public interface ICalendarService
{
    Task<UserHistoryResponse?> GetUserHistoryAsync(Guid userId, CancellationToken token);
    Task<Guid> CreateUserAsync(User user, CancellationToken token);
}