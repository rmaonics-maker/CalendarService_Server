using Calendar_Service.Models;
using Calendar.Contracts;
using Calendar.Contracts.Events;
using MassTransit;

namespace Calendar_Service.Services;

public interface IEventService
{
    Task<(Guid Id, string Email)> CreateEventAsync(
        CalendarEvent evt,
        IPublishEndpoint publishEndpoint,
        CancellationToken token = default);
    
    CalendarEvent? GetEvent(Guid id);
    
    IEnumerable<CalendarEvent> GetOccurrences(
        DateTimeOffset from,
        DateTimeOffset to);
    
    Task<bool> UpdateEventAsync(
        Guid id,
        UpdateEventRequest request,
        CancellationToken token = default);
    
    Task<bool> DeleteEventAsync(
        Guid id,
        CancellationToken token = default);
}