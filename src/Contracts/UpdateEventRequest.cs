namespace Calendar.Contracts
{
    public sealed class UpdateEventRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
    }
}
