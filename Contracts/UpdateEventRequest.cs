namespace Calendar_Service.Contracts
{
    public sealed class UpdateEventRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        // Разрешим менять и длительность
        public TimeSpan Duration { get; set; }
    }
}
