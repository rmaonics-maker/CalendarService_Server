namespace Calendar_Service.Models;

public class CalendarEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; } 
    public TimeSpan Duration { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? RecurrentRule { get; set; }
    public Guid UserId { get; set; }
}