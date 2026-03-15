namespace Calendar.Contracts.DTOs;

public sealed record HistoryEventDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Location { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
}