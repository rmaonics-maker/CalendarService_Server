namespace Calendar_Service.Contracts;

public sealed class EventResponse
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public required string Location { get; init; }
}