using Calendar_Service.Models;

namespace Calendar_Service.Contracts
{
    public sealed class UserHistoryResponse
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public List<CalendarEvent> Events { get; set; } = new();
    }
}
