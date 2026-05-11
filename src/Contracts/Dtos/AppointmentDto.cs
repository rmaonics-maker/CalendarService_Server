namespace Calendar.Contracts.Dtos;

public sealed record AppointmentDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public DateTimeOffset StartTime { get; init; }
    public TimeSpan Duration { get; init; }
    public string Location { get; init; } = default!;
    public string? RecurrentRule { get; init; }
    public Guid UserId { get; init; }
    public DateTime? CreatedAt { get; init; }
}