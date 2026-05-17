namespace Calendar.Contracts.Dtos;

public sealed record AppointmentDto
{
    public Guid Id { get; init; }
    public string? Title { get; init; } 
    public string? Description { get; init; } 
    public DateTimeOffset StartTime { get; init; }
    public TimeSpan Duration { get; init; }
    public string? Location { get; init; } 
    public string? RecurrentRule { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
}