namespace Calendar.Contracts;

public sealed class UserHistoryResponse
{
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public decimal Balance { get; set; }
    public List<EventResponse> Events { get; set; } = [];
}
