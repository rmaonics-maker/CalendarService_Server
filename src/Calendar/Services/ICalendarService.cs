using Calendar.Contracts;
using Calendar_Service.Models;
using MassTransit;
using CalendarEvent = Calendar_Service.Models.CalendarEvent;

namespace Calendar_Service.Services;

public interface ICalendarService
{
    Task<(Guid Id, string Email)> CreateEventAsync(CalendarEvent evt, IPublishEndpoint publishEndpoint,
        CancellationToken token);
    CalendarEvent? GetEvent(Guid id);
    Task<UserHistoryResponse?> GetUserHistoryAsync(Guid userId, CancellationToken token);
    Task<Guid> CreateUserAsync(User user, CancellationToken token);
    IEnumerable<CalendarEvent> GetOccurrences(DateTimeOffset from, DateTimeOffset to);
    Task<bool> DeleteEventAsync(Guid id, CancellationToken token);
    Task<bool> UpdateEventAsync(Guid id, UpdateEventRequest request, CancellationToken token);
}