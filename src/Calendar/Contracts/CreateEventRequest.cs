namespace Calendar_Service.Contracts;

public sealed record CreateEventRequest(
    string Title,
    string Description,
    DateTimeOffset StartTime,
    TimeSpan Duration,
    string Location,
    string? RecurrentRule,
    Guid UserId);
