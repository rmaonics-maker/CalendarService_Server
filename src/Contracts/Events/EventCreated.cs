namespace Calendar.Contracts;

public record EventCreated
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}
