using Calendar.Contracts;
using Calendar_Service.Models;
using Calendar.Contracts.Responses;
using MassTransit;
using CalendarEvent = Calendar_Service.Models.CalendarEvent;

namespace Calendar_Service.Services;

public interface ICalendarService
{
    Task<UserHistoryResponse?> GetUserHistoryAsync(Guid userId, CancellationToken token);
    Task<Guid> CreateUserAsync(User user, CancellationToken token);
}