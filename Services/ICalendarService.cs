using Calendar_Service.Contracts;
using CalendarEvent = Calendar_Service.Models.CalendarEvent;

namespace Calendar_Service.Services;

public interface ICalendarService
{
    Task<Guid> CreateEventAsync(CalendarEvent evt, CancellationToken token);
    CalendarEvent? GetEvent(Guid id);
    Task<UserHistoryResponse?> GetUserHistoryAsync(Guid userId, CancellationToken token);
    IEnumerable<CalendarEvent> GetOccurrences(DateTimeOffset from, DateTimeOffset to);
    Task<bool> DeleteEventAsync(Guid id, CancellationToken token);
    Task<bool> UpdateEventAsync(Guid id, UpdateEventRequest request, CancellationToken token);
}