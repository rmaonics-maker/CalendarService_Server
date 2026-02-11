namespace Calendar_Service.Models;

public class CalendarEvent
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset StartTime { get; init; } 
    public TimeSpan Duration { get; init; }
    public string Location { get; init; } = string.Empty;
    public string? RecurrentRule { get; init; }
    public Guid UserId { get; init; }
}