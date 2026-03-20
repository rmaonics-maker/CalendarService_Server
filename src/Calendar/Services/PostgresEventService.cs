using Calendar_Service.Data;
using Calendar_Service.Models;
using Calendar.Contracts;
using Calendar.Contracts.Events;
using MassTransit;

namespace Calendar_Service.Services;

public class PostgresEventService(
    ApplicationDbContext context,
    ILogger<PostgresEventService> logger) : IEventService
{
    public Task<(Guid Id, string Email)> CreateEventAsync(CalendarEvent evt, IPublishEndpoint publishEndpoint, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public CalendarEvent? GetEvent(Guid id)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<CalendarEvent> GetOccurrences(DateTimeOffset from, DateTimeOffset to)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateEventAsync(Guid id, UpdateEventRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteEventAsync(Guid id, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}