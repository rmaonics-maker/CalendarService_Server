namespace Calendar.Contracts.Dtos;

public sealed record AppointmentDto
{
    public string? Id { get; init; }
    public Guid? MasterId { get; init; }
    public bool IsException { get; init; }
    public string? Title { get; init; } 
    public string? Description { get; init; } 
    public DateTimeOffset StartTime { get; init; }
    public TimeSpan Duration { get; init; }
    public string? Location { get; init; } 
    public RecurrencePatternDto? RecurrentRule { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public int? RoomId { get; init; }
}