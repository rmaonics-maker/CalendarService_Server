namespace Calendar.Contracts;

public sealed class UserHistoryResponse
{
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public decimal Balance { get; set; }
    public List<HistoryEventDto> Events { get; set; } = new();
    
    public sealed record HistoryEventDto
    {
        public Guid Id { get; init; }
        public required string Title { get; init; }
        public required string Location { get; init; }
        public DateTimeOffset StartTime { get; init; }
        public DateTimeOffset EndTime { get; init; }
    }
}
