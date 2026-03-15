using Calendar.Contracts.DTOs;

namespace Calendar.Contracts;

public sealed class UserHistoryResponse
{
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public decimal Balance { get; set; }
    public List<HistoryEventDto> Events { get; set; } = new();
    
    
}
